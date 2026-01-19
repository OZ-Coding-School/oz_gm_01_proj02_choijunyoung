using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab; 
    [SerializeField] private int enemyCount = 10;
    [SerializeField] private float spawnRadius = 50f;

    private ObjectPoolSystem poolSystem;

    public override void OnNetworkSpawn()
    {
        // 오직 서버에서만 실행
        //if (!IsServer) return;

        poolSystem = GetComponent<ObjectPoolSystem>();

        // 컴포넌트가 안 붙어있을 때
        if (poolSystem == null)
        {
            Debug.LogError("[EnemySpawner] ObjectPoolSystem 컴포넌트가 없습니다! 인스펙터에서 추가해주세요.");
            return;
        }

        // 적 스폰 시작
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            // 랜덤 위치 구하기
            Vector3 spawnPos = GetRandomPointOnNavMesh(transform.position, spawnRadius);

            // 풀에서 적 객체 가져오기
            // GetInstance(IsOwner) -> 서버가 Owner가 됩니다.
            NetworkObject enemyNetObj = poolSystem.GetInstance(true);

            if (enemyNetObj != null)
            {
                // 위치 및 회전 설정
                NavMeshAgent agent = enemyNetObj.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.Warp(spawnPos);
                }
                else
                {
                    enemyNetObj.transform.position = spawnPos;
                }

                enemyNetObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                // 네트워크 스폰
                // 풀에서 가져온 객체는 비활성화 상태일 수 있으므로 활성화 후 Spawn
                enemyNetObj.gameObject.SetActive(true);

                if (!enemyNetObj.IsSpawned)
                {
                    enemyNetObj.Spawn(true);
                }
            }
        }

        Debug.Log($"[EnemySpawner] {enemyCount} 마리의 적을 스폰했습니다.");
    }

   
    private Vector3 GetRandomPointOnNavMesh(Vector3 center, float range)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return center;
    }
}