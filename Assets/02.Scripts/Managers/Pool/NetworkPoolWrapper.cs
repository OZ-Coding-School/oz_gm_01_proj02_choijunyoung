using Unity.Netcode;
using UnityEngine;

public class NetworkPoolWrapper : INetworkPrefabInstanceHandler
{
    private NetworkObject prefab;
    private PoolManager pool;

    public NetworkPoolWrapper(NetworkObject prefab, PoolManager pool)
    {
        this.prefab = prefab;
        this.pool = pool;
    }

    // Netcode가 프리팹 만들어야할 때 호출
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        // 매니저에서 풀에 있는거 꺼내옴
        var netObj = pool.GetFromPool(prefab);
        if (netObj != null)
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
        }
        return netObj;
    }

    // Netcode가 프리팹 부숨 호출
    public void Destroy(NetworkObject networkObject)
    {
        pool.ReturnPool(networkObject);
    }
}
