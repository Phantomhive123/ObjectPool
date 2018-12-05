using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager :MonoBehaviour{

    #region 定义一些内部类
    private abstract class Pool
    {
        protected readonly int MAX_NUM;
        protected int _numOfPool = 0;
        protected int NumOfPool
        {
            set
            {
                if (_numOfPool < 0)
                {
                    _numOfPool = 0;
                }
                else if (_numOfPool >= MAX_NUM)
                {
                    CleanByLRU();
                    _numOfPool = MAX_NUM;
                }
                else
                    _numOfPool = value;
            }
            get
            {
                return _numOfPool;
            }
        }
        protected List<string> order;

        public Pool(int max)
        {
            MAX_NUM = max;
            order = new List<string>();
        }

        public virtual void CleanByLRU()
        {

        }
    }

    private class ListPool<T>:Pool
    {
        private Dictionary<string, List<T>> objectList;

        public ListPool():base(10)
        {
            objectList = new Dictionary<string, List<T>>();
            order = new List<string>();
        }

        public T Load(string name)
        {
            List<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                return default(T);
            }

            if (list.Count == 0)
                return default(T);
            else
            {
                T t = list[list.Count - 1];
                NumOfPool--;
                list.Remove(t);
                order.Remove(name);
                return t;
            }
        }

        public void Recycle(string name, T t)
        {
            NumOfPool++;
            List<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                list = new List<T>();               
            }
            else
            {
                objectList.Remove(name);
            }
            order.Add(name);
            list.Add(t);
            objectList.Add(name, list);            
        }

        public void Print(string s)
        {
            Debug.Log(s);
            foreach (KeyValuePair<string, List<T>> c in objectList)
            {
                Debug.Log("name:" + c.Key + "   num:" + c.Value.Count);
            }
            Debug.LogWarning(s);
            foreach(string str in order)
            {
                Debug.LogWarning(str);
            }
        }

        public override void CleanByLRU()
        {
            base.CleanByLRU();
            List<T> list = null;
            if (order.Count!=0 && objectList.TryGetValue(order[0], out list))
            {
                list.RemoveAt(0);
                order.RemoveAt(0);
            }
        }
    }

    private class SinglePool<T>:Pool where T : Object
    {
        private Dictionary<string, T> objectList;

        public SinglePool():base(5)
        {
            objectList = new Dictionary<string, T>();
        }

        public T LoadFromResources<E>(string name) where E : T
        {
            T t = default(T);
            if (!objectList.TryGetValue(name, out t))
            {
                t = Resources.Load<E>(name);
                if (t == default(T))
                {
                    Debug.LogWarning("no such source to load: <" + typeof(E).Name + ">" + name);
                    return t;
                }
                NumOfPool++;
                objectList.Add(name, t);
                order.Add(name);
            }
            return t;
        }

        public void Print(string s)
        {
            Debug.Log(s);
            foreach (KeyValuePair<string, T> c in objectList)
            {
                Debug.Log("name:" + c.Key);
            }
            Debug.LogWarning(s);
            foreach (string str in order)
            {
                Debug.LogWarning(str);
            }
        }

        public override void CleanByLRU()
        {
            base.CleanByLRU();
            T t = null;
            if(order.Count!=0 && objectList.TryGetValue(order[0], out t))
            {
                objectList.Remove(order[0]);
                order.RemoveAt(0);
            }
        }
    }
    #endregion

    #region 单例模式
    private static PoolManager instance = null;
    public static PoolManager Instance
    {
        get
        {
            return instance;
        }
    }
    #endregion

    #region 私有字段
    private ListPool<GameObject> gameObjectPool;
    private ListPool<object> normalClassPool;
    private SinglePool<Object> softSourcePool;
    private SinglePool<GameObject> gameObjectCache;
    #endregion

    #region 初始化
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
    #endregion

    #region 加载方法
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
    /// 泛型方法，用于加载用户自定义的类，无传入参数，但是需要指定泛型的类型
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
    /// 泛型方法，用于加载软资源，需要传入资源类型和在resource文件夹下的路径
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadObject<T>(string path) where T : Object
    {
        T obj = default(T);
        obj = (T)softSourcePool.LoadFromResources<T>(path);
        return obj;
    }
    #endregion

    #region 回收方法
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
    #endregion

    #region TODO
    public void Print()
    {
        gameObjectPool.Print("---------------gameObjectPool-------------------");
        normalClassPool.Print("---------------normalClassPool-------------------");
        softSourcePool.Print("---------------softScourcePool-------------------");
        gameObjectCache.Print("---------------gameObjectCache-------------------");
    }

    public void Destroy()
    {

    }

    private void Release()
    {

    }
    #endregion
}