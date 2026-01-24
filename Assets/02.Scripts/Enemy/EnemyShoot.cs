using Unity.Netcode;
using UnityEngine;

public class EnemyShoot : NetworkBehaviour
{
    [Header("Shoot Settings")]
    [SerializeField] private GameObject enemyAmmoPrefab;  // Enemy_Ammo 프리팹
    [SerializeField] private Transform firePoint;         // 총구 Transform
    [SerializeField] private float bulletSpeed = 20f;
    private float damage;

    private ObjectPoolSystem ammoPool;
    private float nextFireTime = 0f;  // 내부 쿨타임용 (필요 시)

    public override void OnNetworkSpawn()
    {
        damage = GetComponent<EnemyDamage>().GetDamage();

        if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(enemyAmmoPrefab))
        {
            ammoPool = ObjectPoolSystem.ExistingPoolSystems[enemyAmmoPrefab];
        }
        else
        {
            Debug.LogError("[EnemyShoot] Enemy_Ammo 풀 없음!");
        }
    }

    // 외부(AttackState)에서 호출할 발사 메서드
    public void TryShoot(Transform target)
    {
        if (Time.time < nextFireTime) return;

        if (ammoPool == null || firePoint == null || target == null) return;

        // 방향 계산
        Vector3 dir = (target.position + (Vector3.up * 1f)- firePoint.position).normalized;
        Quaternion bulletRot = Quaternion.LookRotation(dir);

        // 풀에서 총알 가져오기
        NetworkObject bulletNetObj = ammoPool.GetInstance(true);
        if (bulletNetObj != null)
        {
            bulletNetObj.transform.position = firePoint.position;
            bulletNetObj.transform.rotation = bulletRot;

            Rigidbody rb = bulletNetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
                rb.linearVelocity = dir * bulletSpeed;
            }

            var bullet = bulletNetObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.SetDamage(damage);
            }

            bulletNetObj.gameObject.SetActive(true);
            bulletNetObj.Spawn();

            Debug.Log($"[EnemyShoot] 총알 발사! 타겟: {target.name}");
        }

        nextFireTime = Time.time + 0.3f;
    }

}