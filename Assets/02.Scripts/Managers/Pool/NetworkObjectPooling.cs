using Unity.Netcode;
using UnityEngine;

// Netcode가 오브젝트 생성/파괴를 요청할 때, 내 풀링 시스템을 사용하도록 연결하는 클래스
public class NetworkedObjectPooling : INetworkPrefabInstanceHandler
{
    private GameObject prefab;
    private string ownerId; // 풀링 구분용 ID

    public NetworkedObjectPooling(GameObject prefab, string ownerId)
    {
        this.prefab = prefab;
        this.ownerId = ownerId;
    }

    // Netcode가 "이 프리팹 생성해줘(Spawn)" 라고 할 때 호출됨
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        // 1. Instantiate 대신 내 풀에서 가져온다
        var netObj = prefab.GetComponent<NetworkObject>();
        var instance = GameManager.Pool.GetFromPool(netObj, this.ownerId);

        // 2. 위치/회전 설정
        instance.transform.position = position;
        instance.transform.rotation = rotation;

        return instance;
    }

    // Netcode가 "이 프리팹 없애줘(Despawn)" 라고 할 때 호출됨
    public void Destroy(NetworkObject networkObject)
    {
        // 1. Destroy 대신 내 풀로 반환한다
        var bulletScript = networkObject.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.ReturnPool(this.ownerId);
        }
        else
        {
            // 혹시 Bullet 컴포넌트가 없으면 그냥 비활성화 처리 (예외처리)
            networkObject.gameObject.SetActive(false);
        }
    }
}