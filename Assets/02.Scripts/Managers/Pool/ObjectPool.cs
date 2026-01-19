using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Queue<T> pool = new Queue<T>();

    public Transform Root { get; private set; }

    public ObjectPool(T prefab, int initCount, Transform parent = null)
    {
        this.prefab = prefab;
        if (parent != null)
        {
            Root = parent;
        }
        else Root = new GameObject($"{prefab.name}_pool").transform;

        Root.SetParent(parent, false);
        for (int i = 0; i < initCount; i++)
        {
            var inst = Object.Instantiate(prefab, Root);
            inst.name = prefab.name;
            inst.gameObject.SetActive(false);
            pool.Enqueue(inst);
        }
    }

    public T Dequeue()
    {
        if (Root != null && !Root.gameObject.activeInHierarchy)
        {
            Root.gameObject.SetActive(true);
        }

        if (pool.Count == 0)
        {
            return null;
        }
        var inst = pool.Dequeue();
        inst.gameObject.SetActive(true);
        return inst;

    }

    public void Enqueue(T instance)
    {
        if (instance == null)
        {
            return;
        }
        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);

    }
}