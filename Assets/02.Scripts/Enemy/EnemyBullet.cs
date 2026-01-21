using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EnemyBullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject hitEnvFXPrefab;     // 벽/바닥 충돌 시 이펙트
    [SerializeField] private float speed = 20f;             // 총알 속도
    [SerializeField] private float lifeTime = 10f;          // 자동 소멸 시간

    public NetworkVariable<float> bulletDamage = new NetworkVariable<float>(0f);  // EnemyDamage에서 받은 데미지
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner || HasAuthority)
        {
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * speed;
            }
            StartCoroutine(DespawnTimer(lifeTime));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        bool isPlayerHit = collision.gameObject.CompareTag("Player") ||
                           collision.gameObject.layer == LayerMask.NameToLayer("Player");

        bool isEnvironmentHit = collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                                collision.gameObject.layer == LayerMask.NameToLayer("Wall");

        if (isPlayerHit || isEnvironmentHit)
        {
            // FX 생성 (로컬로 OK)
            if (hitEnvFXPrefab != null && collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                GameObject fx = Instantiate(hitEnvFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 5f);
            }

            // Despawn은 Owner에게만 요청 (TargetRpc)
            RequestDespawnToOwnerRpc(OwnerClientId);
        }
    }

    [Rpc(SendTo.Authority, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDespawnToOwnerRpc(ulong ownerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != ownerClientId) return;
        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    public void SetDamage(float dmg)
    {
        if (IsOwner || HasAuthority)
        {
            bulletDamage.Value = dmg;
        }
    }

    private IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        if ((IsOwner || HasAuthority) && IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

}