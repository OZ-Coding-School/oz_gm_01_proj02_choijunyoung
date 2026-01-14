using UnityEngine;
using Unity.Netcode;

public class GameManager
{
    private static GameObject _root;
    private static PoolManager _pool;
    private static PlayerSettingManager _settingManager;

    private static void Init()
    {
        if (_root == null)
        {
            _root = new GameObject("@Managers");
            Object.DontDestroyOnLoad(_root);
        }
    }
    private static void CreateManager<T>(ref T manager, string name) where T : Component
    {
        if (manager == null)
        {
            Init();
            GameObject obj = new GameObject(name);
            manager = obj.AddComponent<T>();
            Object.DontDestroyOnLoad(obj);
            obj.transform.SetParent(_root.transform);
        }
    }

    public static PoolManager Pool
    {
        get
        {
            CreateManager(ref _pool, "PoolManager");
            return _pool;
        }
    }

    public static PlayerSettingManager SettingManager
    {
        get
        {
            CreateManager(ref _settingManager, "PlayerSetting");
            return _settingManager;
        }
    }
}
