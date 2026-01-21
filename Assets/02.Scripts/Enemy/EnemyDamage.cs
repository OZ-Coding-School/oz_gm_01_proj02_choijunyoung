using Unity.Netcode;
using UnityEngine;

public class EnemyDamage : NetworkBehaviour, IDamageable
{
    private EnemyData enemyData;
    private float curhealth;
    [SerializeField] private ParticleSystem dieExplosion;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<float> damage = new NetworkVariable<float>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private float maxHealth;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 스폰 시 데이터 초기화 (누구나 할 수 있지만, 실제 세팅은 Owner만)
        if (IsOwner || NetworkManager.Singleton.LocalClient.IsSessionOwner)  // DA에서 안전하게
        {
            if (enemyData == null)
            {
                // EnemyDataManager에서 가져오거나, 여기서 직접 참조
                var dataManager = GetComponent<EnemyDataManager>();
                if (dataManager != null && dataManager._enemyData != null)
                {
                    enemyData = dataManager._enemyData;
                }
            }

            maxHealth = enemyData.MaxHP;
            currentHealth.Value = maxHealth;
            damage.Value = enemyData.Damage;

            Debug.Log($"[EnemyDamage] {enemyData.EnemyName} 스폰됨 | MaxHP: {maxHealth}, Damage: {damage.Value}");
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(float previous, float current)
    {
        Debug.Log($"체력 변화: {previous} → {current}");
        // 여기서 Hit VFX, 사운드, HP 바 업데이트 등 추가
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    public void TakeDamage(float incomingDamage)
    {
        if (!IsSpawned) return;

        //if (!IsOwner && !NetworkManager.Singleton.LocalClient.IsSessionOwner) return;
        
        float newHealth = currentHealth.Value - incomingDamage;
        currentHealth.Value = Mathf.Max(0f, newHealth);


        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }
    public void Die()
    {
        if (dieExplosion != null)
        {
            dieExplosion.transform.position = transform.position; 
            dieExplosion.Stop();
            dieExplosion.Play();
        }

        if (IsSpawned)
        {
            transform.root.GetComponent<NetworkObject>().Despawn(true);  
        }
    }

    // 외부에서 현재 값 확인용
    public float GetCurrentHealth() => currentHealth.Value;
    public float GetDamage() => damage.Value;
}