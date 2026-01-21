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

        StartCoroutine(DespawnTimer(10f));
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            rb.linearVelocity = transform.forward * speed;
        }
         StartCoroutine(DespawnTimer(10f));
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasAuthority) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            ulong targetClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            ApplyPlayerDamageServerRpc(targetClientId, damage);
            RequestDespawnServerRpc();
            //Damage(collision, damage, "Player");
            //RequestDespawnServerRpc();
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Damage(collision, damage, "Enemy");
            RequestDespawnServerRpc();
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            ContactPoint contact = collision.GetContact(0);
            GameObject fx = Instantiate(hit_Env_FX_Prefab, contact.point, Quaternion.LookRotation(contact.normal));

            Destroy(fx, 5f);
            RequestDespawnServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyPlayerDamageServerRpc(ulong targetClientId, float dmg)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            var pd = playerObj.GetComponent<PlayerDamage>();
            if (pd == null) return;

            float old = pd.currentHealth.Value;
            float newH = Mathf.Max(0f, old - dmg);
            pd.currentHealth.Value = newH;
            Debug.Log($"[Bullet Damage Rpc] HP {old} → {newH} (target {targetClientId})");
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void Damage(Collision collision, float damage, string type)
    {
        Debug.Log("적 명중! 데미지 :" + damage);
        if(type =="Enemy") collision.gameObject.GetComponent<EnemyDamage>().TakeDamage(damage);
        //if (type == "Player") collision.gameObject.GetComponent<PlayerDamage>().TakeDamage(damage);

    }
    IEnumerator DespawnTimer(float time)
    {
        yield return new WaitForSeconds(time);
        if (IsSpawned)
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
