using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance {  get; private set; }
    public Dictionary<string, object> pools = new Dictionary<string, object>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void CreatePool<T>(T prefab, int initCount, Transform parent = null) where T : MonoBehaviour
    {
        if(prefab == null) return;
        
        string key = prefab.name;
        if (pools.ContainsKey(key)) return;
        if (parent == null) parent = this.transform;
        pools.Add(key, new ObjectPool<T>(prefab, initCount, parent));

        NetworkObject netObj = prefab.GetComponent<NetworkObject>();
        if(netObj != null && NetworkManager.Singleton != null)
        {
            var wrapper = new NetworkPoolWrapper(netObj, this);

            NetworkManager.Singleton.PrefabHandler.AddHandler(netObj, wrapper);
        }
    }

    public T GetFromPool<T>(T prefab) where T : MonoBehaviour
    {
        if(prefab == null) return null;
        if(!pools.TryGetValue(prefab.name, out var box)) return null;
       
        var pool = box as ObjectPool<T>;
        if (pool != null) return pool.Dequeue();
        else return null;
    }
    
    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var obj = GetFromPool(prefab.GetComponent<NetworkObject>());
        if(obj != null)
        {
            obj.transform.position = pos;
            obj.transform.rotation = rot;
        }
        return obj;
    }

    public void ReturnPool<T>(T instance) where T : MonoBehaviour
    {
        if (instance == null) return;

        string key = instance.gameObject.name.Replace("(Clone)", "");

        if (!pools.TryGetValue(key, out var box))
        {
            Destroy(instance.gameObject);
            return;
        }

        var pool = box as ObjectPool<T>;
        if (pool != null) pool.Enqueue(instance);
        else return;
    }

}
