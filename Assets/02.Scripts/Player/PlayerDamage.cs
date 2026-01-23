using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;              
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PlayerDamage : NetworkBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Death Effects")]
    [SerializeField] private GameObject deathUICanvas; 
    [SerializeField] private MonoBehaviour playerMovementScript;  
    [SerializeField] private MonoBehaviour playerShootScript; 
    [SerializeField] private Volume globalVolume;  

    private Animator anim;
    private ColorAdjustments colorAdjustments;  // 흑백 효과용

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) Debug.LogWarning("[PlayerDamage] Animator 없음");

        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<PlayerMove>();
            if (playerMovementScript == null) Debug.Log("[PlayerDamage] 움직임 스크립트(PlayerController 등)를 찾을 수 없음");
        }
        if (playerShootScript == null)
        {
            playerShootScript = GetComponent<PlayerShoot>();
            if (playerShootScript == null) Debug.Log("[PlayerDamage] 움직임 스크립트(PlayerShoot 등)를 찾을 수 없음");
        }

        if (deathUICanvas == null)
        {
            deathUICanvas = GameObject.Find("DeathUICanvas");
            if (deathUICanvas == null) Debug.Log("[PlayerDamage] DeathUICanvas를 찾을 수 없음");
        }

        if (globalVolume == null)
        {
            globalVolume = GameObject.Find("GlobalPostProcessing")?.GetComponent<Volume>();  // true: 비활성 오브젝트도 찾음
            
            if (globalVolume == null)
            {
                Debug.LogError("[PlayerDamage] 씬에 isGlobal 체크된 Volume 오브젝트가 없습니다!");
            }
        }

        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.saturation.value = 0f;  // 기본 0 = 정상 색상
        }
        else
        {
            Debug.LogWarning("[PlayerDamage] Global Volume 또는 ColorAdjustments가 없음 (흑백 효과 안 됨)");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
            Debug.Log($"[PlayerDamage] 내 플레이어 HP 초기화: {currentHealth.Value}/{maxHealth}");
        }

        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeadChanged;

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isDead.OnValueChanged -= OnDeadChanged;
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;

        if (IsOwner)
        {
            Debug.Log("[OnNetworkDespawn] fallback → TitleScene 로드");
            SceneManager.LoadScene("TitleScene");
        }
        base.OnNetworkDespawn();
    }

    private void OnHealthChanged(float previous, float current)
    {
        Debug.Log($"[PlayerDamage Client-{OwnerClientId}] HP Changed: {previous} → {current} (LocalClientId: {NetworkManager.Singleton.LocalClientId})");

        if (IsOwner && current <= 1f && previous > 1f)
        {
            Die();
        }
    }

    private void OnDeadChanged(bool previous, bool current)
    {
        if (current)
        {
            Debug.Log($"[PlayerDamage] 플레이어 사망 (Client-{OwnerClientId})");

            // 1. 사망 애니메이션 트리거 (모든 클라이언트에서)
            if (anim != null)
            {
                anim.SetBool("IsDead", true);
            }

            // 2. 움직임 스크립트 비활성화 (로컬 플레이어만)
            if (IsOwner && playerMovementScript != null)
            {
                playerMovementScript.enabled = false;
                playerShootScript.enabled = false;
            }

            // 3. 사망 UI 표시 (로컬 플레이어만)
            if (IsOwner && deathUICanvas != null)
            {
                deathUICanvas.SetActive(true);
                Debug.Log("[PlayerDamage] 사망 UI 표시");
            }

            // 4. 카메라 흑백 효과 (로컬 플레이어만 적용 - Post Processing)
            if (IsOwner && colorAdjustments != null)
            {
                colorAdjustments.saturation.value = -100f;  // 완전 흑백
                Debug.Log("[PlayerDamage] 카메라 흑백 효과 적용");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (
            collision.gameObject.CompareTag("bullet") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Bullet") ||
            collision.gameObject.layer == LayerMask.NameToLayer("EnemyBullet") ||
            collision.gameObject.CompareTag("EnemyBullet"))
        {
            Bullet bullet = collision.gameObject.GetComponent<Bullet>();
            EnemyBullet enemyBullet = collision.gameObject.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                if (bullet.OwnerClientId == OwnerClientId) return;
                TakeDamage(bullet.bulletDamage.Value);
                Debug.Log($"[PlayerDamage] Bullet 충돌! 데미지 {bullet.bulletDamage.Value} 받음 (Client-{OwnerClientId})");
            }
            else if(enemyBullet != null) 
            {
                TakeDamage(enemyBullet.bulletDamage.Value);
                Debug.Log($"[PlayerDamage] Bullet 충돌! 데미지 {bullet.bulletDamage.Value} 받음 (Client-{OwnerClientId})");
            }
        }
    }

    public void TakeDamage(float incomingDamage)
    {
        if (!IsSpawned || isDead.Value) return;

        float newHealth = currentHealth.Value - incomingDamage;
        currentHealth.Value = Mathf.Max(0f, newHealth);
        Debug.Log($"[PlayerDamage] {incomingDamage} 데미지 받음 | 남은 HP: {currentHealth.Value} (Client-{OwnerClientId})");
    }

    public void Die()
    {
        if (!IsOwner) return;

        isDead.Value = true;
        currentHealth.Value = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        deathUICanvas.SetActive(isDead.Value);

        Debug.Log($"[PlayerDamage] 사망! Client-{OwnerClientId}");

        // 플레이어 객체 제거
        
    }

    public void OnClickDieConfirm()
    {
        StartCoroutine(DieCo());
    }

    IEnumerator DieCo()
    {
        if (deathUICanvas != null)
        {
            deathUICanvas.SetActive(false);
        }
        yield return new WaitForSeconds(1.0f); // 사망 UI 충분히 보여주기

        if (IsOwner)
        {
            ExitSession();
        }
    }

    public void ExitSession()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(ExitSessionCo());
    }

    IEnumerator ExitSessionCo()
    {
        Debug.Log("[ExitSessionCo] 네트워크 연결 종료 시작");

        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);

        while (SceneManager.GetActiveScene().name != "TitleScene")
        {
            yield return null;
        }
    }

    private void OnClientStopped(bool isHost)
    {
        if (IsOwner && !isHost)
        {
            Debug.Log("[OnClientStopped] Shutdown 완료 감지 → TitleScene 로드");
            SceneManager.LoadScene("TitleScene");
        }
    }

    public float GetCurrentHealth() => currentHealth.Value;
    public bool IsDead() => isDead.Value;
}