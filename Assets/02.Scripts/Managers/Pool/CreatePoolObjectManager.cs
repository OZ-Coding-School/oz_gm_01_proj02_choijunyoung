using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]

public class CreatePoolObjectManager : NetworkBehaviour
{
    public static CreatePoolObjectManager instance { get; private set; }

    [Header("Create Pool Settings")]
    [SerializeField] private SOWeapon[] allWeapons;
    private const int MAX_PLAYERS = 4;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if(GameManager.Pool != null) InitGlobalPools();
            else
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            }

        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted,List<ulong> clientsTimedOut)
    {
        if(GameManager.Pool != null)
        {
            InitGlobalPools();
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    private void InitGlobalPools()
    {
        var gmInit = GameManager.Pool.transform;
        var parent = gmInit.Find("Ammo_Pool");
        if (parent == null)
        {
            parent = new GameObject("Ammo_Pool").transform;
            parent.SetParent(gmInit, false);
        }

        // 플레이어들 총알 Pool
        foreach (var weapon in allWeapons)
        {
            GameManager.Pool.CreatePool(weapon.bulletPrefab.GetComponent<NetworkObject>(), weapon.maxAmmo * MAX_PLAYERS, parent, null);
        }
    }
}
