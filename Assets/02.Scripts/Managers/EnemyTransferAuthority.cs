using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 클라이언트 Disconnect 또는 호스트 변경 시 Enemy의 Ownership을 자동 양도하는 관리자
/// DontDestroyOnLoad로 항상 살아있으며, 호스트(서버)에서만 동작
/// </summary>
public class EnemyTransferAuthority : MonoBehaviour
{
    public static EnemyTransferAuthority Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public void OnEnable()
    {
        // 새 호스트가 되었을 때만 이벤트 등록
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnServerStarted += OnHostMigrated;
        }
    }

    /// <summary>
    /// 호스트 마이그레이션이 발생했을 때 (새 호스트에서 호출됨)
    /// Orphan 상태의 Enemy를 새 호스트에게 양도
    /// </summary>
    private void OnHostMigrated()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("[EnemyTransfer] 새 호스트 됨 → Orphan Enemy Ownership 재배분 시작");
        ReassignOrphanEnemies();
    }

    /// <summary>
    /// 클라이언트가 Disconnect 되었을 때
    /// 해당 클라이언트가 소유하던 Enemy의 Ownership 양도
    /// </summary>
    private void OnClientDisconnected(ulong disconnectedClientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[EnemyTransfer] Client {disconnectedClientId} Disconnect → Ownership 양도 시작");
        ReassignOrphanEnemies(disconnectedClientId);
    }

    /// <summary>
    /// Orphan 상태이거나 특정 클라이언트가 소유하던 Enemy를 살아있는 클라이언트에게 양도
    /// </summary>
    /// <param name="specificOldOwner">특정 클라이언트 ID만 대상으로 할 때 사용 (null이면 모든 Orphan 대상)</param>
    private void ReassignOrphanEnemies(ulong? specificOldOwner = null)
    {
        var allNetObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        int reassignedCount = 0;

        foreach (var netObj in allNetObjects)
        {
            if (!netObj.CompareTag("Enemy") || !netObj.IsSpawned)
                continue;

            bool needsReassign = false;

            if (specificOldOwner.HasValue)
            {
                // 특정 클라이언트 Disconnect 시 그 클라이언트 소유 Enemy만
                if (netObj.OwnerClientId == specificOldOwner.Value)
                    needsReassign = true;
            }
            else
            {
                // Orphan 상태 (Owner가 0이거나 현재 연결된 클라이언트에 없음)
                if (netObj.OwnerClientId == 0 ||
                    !NetworkManager.Singleton.ConnectedClients.ContainsKey(netObj.OwnerClientId))
                    needsReassign = true;
            }

            if (needsReassign)
            {
                ulong newOwnerId = GetNextAliveClientId();
                netObj.ChangeOwnership(newOwnerId);
                Debug.Log($"[EnemyTransfer] Enemy {netObj.name} (old Owner: {netObj.OwnerClientId}) → {newOwnerId} 양도");
                reassignedCount++;
            }
        }

        if (reassignedCount == 0)
        {
            Debug.Log("[EnemyTransfer] 재배분할 Orphan Enemy가 없습니다.");
        }
    }

    /// <summary>
    /// 살아있는 클라이언트 중 하나를 선택 (자신 제외, 없으면 자신=호스트)
    /// </summary>
    private ulong GetNextAliveClientId()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return clientId;
        }

        return NetworkManager.Singleton.LocalClientId; // 아무도 없으면 호스트가 가져감
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnServerStarted -= OnHostMigrated;
            }
        }
    }
}