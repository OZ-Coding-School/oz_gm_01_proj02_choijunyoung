using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EnemyBullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject hitEnvFXPrefab;     // 벽/바닥 충돌 시 이펙트
    [SerializeField] private float speed = 20f;             // 총알 속도
    [SerializeField] private float lifeTime = 10f;          // 자동 소멸 시간

    private float damage;                                   // EnemyDamage에서 받은 데미지
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        StartCoroutine(DespawnTimer(lifeTime));
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 플레이어만 타격 대상 (태그 또는 레이어로 필터링)
        if (collision.gameObject.CompareTag("Player") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // 플레이어 데미지 적용
            var playerDamage = collision.gameObject.GetComponent<PlayerDamage>();
            if (playerDamage != null)
            {
                playerDamage.TakeDamage(damage);
                Debug.Log($"[EnemyBullet] 플레이어에게 {damage} 데미지 적용");
            }
            else
            {
                Debug.LogWarning($"[EnemyBullet] 플레이어에 PlayerDamage 컴포넌트 없음: {collision.gameObject.name}");
            }

            // 충돌 즉시 Despawn
            RequestDespawnServerRpc();
        }
        else
        {
            // 벽/바닥/기타 환경 충돌 → 이펙트 + Despawn
            if (hitEnvFXPrefab != null)
            {
                ContactPoint contact = collision.contacts[0];
                GameObject fx = Instantiate(hitEnvFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 5f);  // 이펙트 수명
            }

            RequestDespawnServerRpc();
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    private IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);

        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ServerRpc]
    private void RequestDespawnServerRpc()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}