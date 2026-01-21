using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 로컬 클라이언트가 끊김 → 무시
            return;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost) // 이전 호스트 끊김
        {
            Debug.Log("이전 세션 오너 끊김! 새 세션 오너 선정됨");
            ReassignAllEnemyAuthority();
        }
    }

    private void ReassignAllEnemyAuthority()
    {
        // 모든 Enemy 찾기 (태그 "Enemy" 또는 FindObjectsOfType<EnemyStateManager>())
        EnemyStateManager[] enemies = Object.FindObjectsByType<EnemyStateManager>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            NetworkObject no = enemy.GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned)
            {
                // 새 세션 오너(Client-2)로 Ownership 이전
                no.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                Debug.Log($"Enemy Ownership 재할당 → Client-{NetworkManager.Singleton.LocalClientId}");
            }
        }
    }
}