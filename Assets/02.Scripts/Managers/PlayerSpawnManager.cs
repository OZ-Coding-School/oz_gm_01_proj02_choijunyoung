using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnPositionSetter : NetworkBehaviour
{
    private Transform[] spawnPoints;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "GameScene") return;

        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");

        if (spawnObjects.Length == 0)
        {
            Debug.LogError("[PlayerSpawnPositionSetter] 'PlayerSpawnPoint' 태그 오브젝트 없음!");
            return;
        }

        spawnPoints = new Transform[spawnObjects.Length];
        for (int i = 0; i < spawnObjects.Length; i++)
        {
            spawnPoints[i] = spawnObjects[i].transform;
        }

        Debug.Log($"[PlayerSpawnPositionSetter] {spawnPoints.Length}개 스폰 포인트 찾음");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer) return;

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int index = Random.Range(0, spawnPoints.Length);
        Transform target = spawnPoints[index];

        // 즉시 하지 말고 1프레임 지연 (또는 LateUpdate로)
        StartCoroutine(TeleportAfterFrame(target));
    }

    private IEnumerator TeleportAfterFrame(Transform target)
    {
        yield return null;  // 한 프레임 기다림 (또는 yield return new WaitForEndOfFrame();)

        transform.position = target.position;
        transform.rotation = target.rotation;

        if (TryGetComponent<NetworkTransform>(out var nt))
        {
            nt.Teleport(target.position, target.rotation, transform.localScale);
            // 또는 nt.SetState(target.position, target.rotation, transform.localScale, true);  // 일부 버전
        }

        Debug.Log($"[Delayed Teleport] → {target.name} ({target.position})");
    }
}