using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager :MonoBehaviour{

    #region 定义一些内部类
    private abstract class NumLimit
    {
        protected readonly int MAX_NUM;
        protected int _numOfPool = 0;
        public int NumOfPool
        {
            set
            {
                if (value < 0)
                {
                    _numOfPool = 0;
                    return;
                }

                while(value>MAX_NUM)
                {
                    CleanByLRU();
                    value--;
                }
                _numOfPool = value;
            }
            get
            {
                return _numOfPool;
            }
        }

        public NumLimit(int max)
        {
            if(max<=0)
            {
                Debug.LogError("对象池设定的上限不可小于等于0,已修改为默认值");
                MAX_NUM = 10;
            }
            else
                MAX_NUM = max;
        }

        public NumLimit()
        {
            MAX_NUM = 10;
        }

        public virtual void CleanByLRU()
        {
            if (NumOfPool == 0)
                Debug.LogWarning("LRU时数组为空");
        }
    }

    private class ObjectList<T> : NumLimit
    {
        private List<T> list;
        private List<T> onUse;
        private List<T> destroyQueue;

        public ObjectList()
        {
            list = new List<T>();
            onUse = new List<T>();
            destroyQueue = new List<T>();
        }

        public ObjectList(int max):base(max)
        {
            list = new List<T>();
            onUse = new List<T>();
            destroyQueue = new List<T>();
        }

        public T GetLastOne()
        {
            if (NumOfPool == 0)
            {
                return default(T);
            }
            else
            {
                T t = list[list.Count - 1];
                list.Remove(t);
                onUse.Add(t);
                NumOfPool--;
                return t;
            }
        }

        public void AddObject(T t)
        {
            NumOfPool++;
            list.Add(t);
            if(onUse.Contains(t))
            {
                onUse.Remove(t);
            }
        }

        public void AddToOnUse(T t)
        {
            if (!onUse.Contains(t))
                onUse.Add(t);
        }

        public override void CleanByLRU()
        {
            base.CleanByLRU();
            T t = list[0];
            list.Remove(t);
            DestroyObject(t);
        }

        //TODO
        public void DestroyObject(T t)
        {
            if (list.Contains(t))
                list.Remove(t);
            if (onUse.Contains(t))
                onUse.Remove(t);
            if (!destroyQueue.Contains(t))
                destroyQueue.Add(t);
        }

        //TODO
        public void DestroyAll()
        {
            list.Clear();
            onUse.Clear();
            destroyQueue.Clear();
        }

        //TODO
        public void RealDestroy()
        {
            if (destroyQueue.Count != 0)
            {
                T t = destroyQueue[0];
                destroyQueue.RemoveAt(0);
            }
        }
    }

    private class ListPool<T>
    {
        private Dictionary<string, ObjectList<T>> objectList;

        public ListPool()
        {
            objectList = new Dictionary<string, ObjectList<T>>();
        }

        public T Load(string name)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                return default(T);
            }
            return list.GetLastOne();
        }

        public void Recycle(string name, T t)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                list = new ObjectList<T>();
                objectList.Add(name, list);
            }
            list.AddObject(t);        
        }

        public void Recycle(string name, T t, int max)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                list = new ObjectList<T>(max);
                objectList.Add(name, list);
            }
            list.AddObject(t);
        }

        //TODO
        public void DestroyObj(string name, T t)
        {
           
        }

        public void Print(string s)
        {
            Debug.Log(s);
        }
    }

    private class SinglePool<T>:NumLimit where T : Object
    {
        private Dictionary<string, T> objectList;
        private List<string> order;

        public SinglePool():base(5)
        {
            objectList = new Dictionary<string, T>();
            order = new List<string>();
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
                Resources.UnloadAsset(t);
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

    public void RecycleObject(GameObject obj,int max)
    {
        obj.SetActive(false);
        gameObjectPool.Recycle(obj.name, obj,max);
    }

    public void RecycleObject<T>(T t,int max)
    {
        System.Type type = typeof(T);
        normalClassPool.Recycle(type.Name, t,max);
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

    private void Release()
    {

    }
    #endregion
}