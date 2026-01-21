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
        if (HasAuthority)
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
        if (!HasAuthority) return;
        // 플레이어만 타격 대상 (태그 또는 레이어로 필터링)
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            ulong targetClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            ApplyDamageServerRpc(targetClientId, damage);
            Debug.Log($"[EnemyBullet] 플레이어({targetClientId})에게 {damage} 데미지 요청");

            if (IsSpawned) NetworkObject.Despawn(true);
            //기존코드
            //    var playerDamage = collision.gameObject.GetComponent<PlayerDamage>();
            //    if (playerDamage != null)
            //    {
            //        playerDamage.TakeDamage(damage);
            //        Debug.Log($"[EnemyBullet] 플레이어에게 {damage} 데미지 적용");
            //    }
            //    else
            //    {
            //        Debug.LogWarning($"[EnemyBullet] 플레이어에 PlayerDamage 컴포넌트 없음: {collision.gameObject.name}");
            //    }

            //    // 충돌 즉시 Despawn
            //    RequestDespawnServerRpc();
            //}
            //else
            //{
            //    // 벽/바닥/기타 환경 충돌 → 이펙트 + Despawn
            //    if (hitEnvFXPrefab != null)
            //    {
            //        ContactPoint contact = collision.contacts[0];
            //        GameObject fx = Instantiate(hitEnvFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            //        Destroy(fx, 5f);  // 이펙트 수명
        }
        else
        {
            if(hitEnvFXPrefab != null)
            {
                ContactPoint contact = collision.contacts[0];
                SpawnHitFXClientRpc(contact.point, contact.normal); // 모든 클라에 FX 동기화 
            }
            if (IsSpawned) NetworkObject.Despawn(true);
        }

        //    RequestDespawnServerRpc();
        //}
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyDamageServerRpc(ulong targetClientId, float dmg)
    {
        Debug.Log($"[ApplyDamageRpc] Called on Client-{NetworkManager.Singleton.LocalClientId} for target {targetClientId}, dmg={dmg}");
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) { Debug.LogError("PlayerObject null!"); return; }
            PlayerDamage playerDamage = client.PlayerObject?.GetComponent<PlayerDamage>();
            if(playerDamage == null) { Debug.LogError("PlayerDamage 컴포넌트 없음!"); return; }

            float old = playerDamage.currentHealth.Value;
            float newH = Mathf.Max(0f, old - dmg);
            playerDamage.currentHealth.Value = newH;
            Debug.Log($"[ApplyDamageRpc] HP 업데이트: {old} → {newH} (target Client-{targetClientId})");
        }
        else
        {
            Debug.LogError($"ConnectedClients에 {targetClientId} 없음!");
        }
    }

    [ClientRpc]
    private void SpawnHitFXClientRpc(Vector3 pos, Vector3 normal)
    {
        GameObject fx = Instantiate(hitEnvFXPrefab, pos, Quaternion.LookRotation(normal));
        Destroy(fx, 5f);
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    private IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);

        if (IsSpawned && HasAuthority)
        {
            NetworkObject.Despawn(true);
        }
    }

}