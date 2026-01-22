using Unity.Netcode;
using UnityEngine;

public class EnemyTransferAuthority : NetworkBehaviour
{
    public static EnemyTransferAuthority Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤 패턴 + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Disconnect 이벤트 등록 (항상 살아있도록)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 호스트 변경 시 이벤트 등록 (새 호스트에서만)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnServerStarted += OnHostChanged;
        }
    }

    private void OnHostChanged()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        Debug.Log("[EnemyTransferAuthority] 새 호스트가 됨 → Orphan Enemy Ownership 재배분 시작");

        ReassignOrphanEnemies();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        Debug.Log($"[EnemyTransferAuthority] Client {clientId} Disconnect → Ownership 양도 시작");

        ReassignOrphanEnemies(clientId);
    }

    private void ReassignOrphanEnemies(ulong? specificOldOwner = null)
    {
        var allNetObjs = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var netObj in allNetObjs)
        {
            if (!netObj.CompareTag("Enemy") || !netObj.IsSpawned) continue;

            bool shouldReassign = false;

            if (specificOldOwner.HasValue)
            {
                // 특정 클라이언트 Disconnect 시 그 클라이언트 소유 Enemy만
                if (netObj.OwnerClientId == specificOldOwner.Value)
                    shouldReassign = true;
            }
            else
            {
                // Orphan 체크 (호스트 변경 시)
                if (netObj.OwnerClientId == 0 ||
                    !NetworkManager.Singleton.ConnectedClients.ContainsKey(netObj.OwnerClientId))
                    shouldReassign = true;
            }

            if (shouldReassign)
            {
                ulong newOwner = GetAnyAliveClientId();
                netObj.ChangeOwnership(newOwner);
                Debug.Log($"Enemy {netObj.name} (old Owner: {netObj.OwnerClientId}) → {newOwner}에게 양도");
            }
        }
    }

    private ulong GetAnyAliveClientId()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // 자신 제외하고 첫 번째 살아있는 클라이언트
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return clientId;
        }
        return NetworkManager.Singleton.LocalClientId; // 아무도 없으면 호스트가 가져감
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnServerStarted -= OnHostChanged;
            }
        }
    }
}