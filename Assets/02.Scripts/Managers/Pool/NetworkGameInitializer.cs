using Unity.Netcode;
using UnityEngine;

public class NetworkGameInitializer : NetworkBehaviour
{
    private bool hasInitialized = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += AttemptInit;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) AttemptInit();
    }

    private void AttemptInit()
    {
        if (hasInitialized || !IsServer) return; // 서버만 실행!
        hasInitialized = true;
        InitGlobalPools();
    }

    private void InitGlobalPools()
    {
        if (GlobalWeaponConfig.Instance == null) return;

        var weapons = GlobalWeaponConfig.Instance.allWeapons;
        int maxPlayers = GlobalWeaponConfig.Instance.maxPlayers;

        Transform poolRoot = GameManager.Pool.transform.Find("Ammo_Pool");
        if (poolRoot == null)
        {
            poolRoot = new GameObject("Ammo_Pool").transform;
            poolRoot.SetParent(GameManager.Pool.transform);
        }

        foreach (var weapon in weapons)
        {
            string poolKey = weapon.bulletPrefab.name;

            // 여기서 CreatePool 내부 로직이 중요합니다.
            // 단순히 Instantiate만 하는 게 아니라, 반드시 netObj.Spawn()을 해줘야 합니다.
            // GameManager.Pool.CreatePool 코드가 Spawn()을 포함하고 있다고 가정합니다.

            GameManager.Pool.CreatePool(
                weapon.bulletPrefab.GetComponent<NetworkObject>(),
                weapon.maxAmmo * maxPlayers,
                poolRoot,
                poolKey
            );
        }
    }
}