using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
public class Bullet : NetworkBehaviour
{
    [SerializeField] GameObject hit_Env_FX_Prefab;
    [SerializeField] float speed = 20f;
    float damage;

    Rigidbody rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
    }
    private void OnEnable()
    {
        Debug.Log("총알 활성화");

        if(IsOwner) StartCoroutine(DelayShoot());
        StartCoroutine(DespawnTimer(5f));
    }

    public override void OnNetworkSpawn()
    {
        

        if (IsOwner)
        {
            rb.linearVelocity = transform.forward * speed;
        }
         StartCoroutine(DespawnTimer(5f));
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Damage(collision, damage);
            RequestDespawnServerRpc();
        }
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            ContactPoint contact = collision.GetContact(0);
            GameObject fx = Instantiate(hit_Env_FX_Prefab, contact.point, Quaternion.LookRotation(contact.normal));

            Destroy(fx, 5f);
            RequestDespawnServerRpc();
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void Damage(Collision collision, float damage)
    {
        Debug.Log("적 명중! 데미지 :" + damage);
        collision.gameObject.GetComponent<TestEnemyDamage>().TakeDamage(damage);
        collision.gameObject.GetComponent<EnemyBase>().TakeDamage(damage);

    }
    IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
    [ServerRpc]
    private void RequestDespawnServerRpc()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    public void ReturnPool(string userId = null)
    {
        if (PoolManager.instance != null) PoolManager.instance.ReturnPool(this.GetComponent<NetworkObject>(), userId);
    }

    IEnumerator DelayShoot()
    {
        yield return null;

    }
}
