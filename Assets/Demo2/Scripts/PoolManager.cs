using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager :MonoBehaviour{

    private static PoolManager instance = null;

    public static PoolManager Instance
    {
        get
        {
            return instance;
        }
    }

    private ListPool<GameObject> gameObjectPool;
    private ListPool<object> normalClassPool;
    private SinglePool<Object> softSourcePool;
    private SinglePool<GameObject> gameObjectCache;

    private void Start()
    {
        if(instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        else
        {
            gameObjectPool = new ListPool<GameObject>();
            normalClassPool = new ListPool<object>();
            softSourcePool = new SinglePool<Object>();
            gameObjectCache = new SinglePool<GameObject>();
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 非泛型方法，加载游戏物体，传入参数为想要加载的GameObject的名字
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject LoadObject(string name)
    {
        GameObject obj = null;
        obj = gameObjectPool.Load(name);
        if(obj == null)
        {
            obj = gameObjectCache.LoadFromResources<GameObject>(name);
            obj = Instantiate(obj);
            obj.name = name;
        }
        else
            obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 泛型方法，用于加载用户自定义的类，无传入参数，但是需要制定泛型的类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadObject<T>() where T: new()
    {
        T obj = default(T);
        System.Type type = typeof(T);
        obj = (T)normalClassPool.Load(type.Name);
        if(obj == null)
            obj = new T();
        return obj; 
    }

    /// <summary>
    /// 泛型方法，用于回收用户自定义的类，需要参数和类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    public void RecycleObject<T>(T t)
    {
        System.Type type = typeof(T);
        normalClassPool.Recycle(type.Name,t);
    }

    /// <summary>
    /// 非泛型方法，回收GameObject，不需要定义类型
    /// </summary>
    /// <param name="obj"></param>
    public void RecycleObject(GameObject obj)
    {
        obj.SetActive(false);
        gameObjectPool.Recycle(obj.name,obj);
    }

    public void Print()
    {
        gameObjectPool.Print("---------------gameObjectPool-------------------");
        normalClassPool.Print("---------------normalClassPool-------------------");
        softSourcePool.Print("---------------softScourcePool-------------------");
        gameObjectCache.Print("---------------gameObjectCache-------------------");
    }
}

class ListPool<T>
{
    private Dictionary<string, List<T>> objectList;

    public ListPool()
    {
        objectList = new Dictionary<string, List<T>>();
    }

    public T Load(string name)
    {
        List<T> list = null;
        if(!objectList.TryGetValue(name,out list))
        {
            list = new List<T>();
            objectList.Add(name, list);
        }

        if (list.Count == 0)
            return default(T);
        else
        {
            T t = list[list.Count - 1];
            list.Remove(t);
            return t;
        }
    }

    public void Recycle(string name,T t)
    {      
        List<T> list = null;
        if(!objectList.TryGetValue(name,out list))
        {
            list = new List<T>();
            objectList.Add(name, list);
        }
        list.Add(t);
    }

    public void Print(string s)
    {
        Debug.Log(s);
        foreach(KeyValuePair<string,List<T>> c in objectList)
        {
            Debug.Log("name:" + c.Key + "   num:" + c.Value.Count);
        }
    }
}

class SinglePool<T> where T:Object
{
    private Dictionary<string, T> objectList;

    public SinglePool()
    {
        objectList = new Dictionary<string, T>();
    }

    public T LoadFromResources<E>(string name) where E:T
    {
        T t = default(T);
        if (!objectList.TryGetValue(name, out t))
        {
            t = Resources.Load<E>(name);
            objectList.Add(name, t);
        }
        return t;
    }

    public void Print(string s)
    {
        Debug.Log(s);
        foreach(KeyValuePair<string,T> c in objectList)
        {
            Debug.Log("name:" + c.Key);
        }
    }
}