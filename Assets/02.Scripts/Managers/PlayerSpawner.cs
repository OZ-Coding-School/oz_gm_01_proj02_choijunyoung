using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private Transform[] spawnPoints;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "GameScene") return;

        // 태그로 스폰 포인트 찾기
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");
        if (spawnObjects.Length == 0)
        {
            Debug.LogError("PlayerSpawnPoint 태그 붙은 오브젝트가 하나도 없음");
            return;
        }

        spawnPoints = new Transform[spawnObjects.Length];
        for (int i = 0; i < spawnObjects.Length; i++)
        {
            spawnPoints[i] = spawnObjects[i].transform;
        }

        Debug.Log($"{spawnPoints.Length}개의 스폰 포인트 찾음");

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network 아직 안 켜짐. 1초 후 다시 시도");
            Invoke(nameof(SpawnMyPlayer), 1f);
            return;
        }

        SpawnMyPlayer();
    }

    private void SpawnMyPlayer()
    {
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            Debug.Log("이미 내 플레이어 객체 등록됨. 스폰 스킵");
            return;
        }

        int index = Random.Range(0, spawnPoints.Length);
        Transform spawnTransform = spawnPoints[index];

        GameObject playerInstance = Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("플레이어 프리팹에 NetworkObject 없음!");
            Destroy(playerInstance);
            return;
        }

        // SpawnAsPlayerObject로 내 클라이언트의 플레이어 객체로 등록
        netObj.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

        Debug.Log($"내 플레이어 스폰 완료! 위치: {spawnTransform.position}, Client ID: {NetworkManager.Singleton.LocalClientId}");
    }
}