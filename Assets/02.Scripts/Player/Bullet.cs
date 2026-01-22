using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
public class Bullet : NetworkBehaviour
{
    [SerializeField] GameObject hit_Env_FX_Prefab;
    [SerializeField] float speed = 20f;
    public NetworkVariable<float> bulletDamage = new NetworkVariable<float>(0f);

    Rigidbody rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
    }
    private void OnEnable()
    {
        Debug.Log("총알 활성화");

        StartCoroutine(DespawnTimer(10f));
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            rb.linearVelocity = transform.forward * speed;
            StartCoroutine(DespawnTimer(10f));
        }
         
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
        collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // FX만 생성 (로컬로 OK, 또는 ClientRpc로 동기화)
            if (hit_Env_FX_Prefab != null)
            {
                ContactPoint contact = collision.contacts[0];
                GameObject fx = Instantiate(hit_Env_FX_Prefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 5f);
            }

            // Despawn은 Owner에게만 요청
            DespawnBulletRpc(OwnerClientId);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (hit_Env_FX_Prefab != null)
            {
                ContactPoint contact = collision.contacts[0];
                GameObject fx = Instantiate(hit_Env_FX_Prefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 5f);
            }
            DespawnBulletRpc(OwnerClientId);
        }
    }

    [Rpc(SendTo.Authority, InvokePermission = RpcInvokePermission.Everyone)]
    private void DespawnBulletRpc(ulong ownerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != ownerClientId) return;
        if (IsSpawned && IsOwner)
        {
            NetworkObject.Despawn(true);
        }
    }

    public void SetDamage(float dmg)
    {
        if (IsOwner)
        {
            bulletDamage.Value = dmg;
        }
    }

    public void Damage(Collision collision, float damage, string type)
    {
        Debug.Log("적 명중! 데미지 :" + damage);
        //if(type =="Enemy") collision.gameObject.GetComponent<EnemyDamage>().TakeDamage(damage);
        //if (type == "Player") collision.gameObject.GetComponent<PlayerDamage>().TakeDamage(damage);

    }
    IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        if (IsSpawned && IsOwner)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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

}
