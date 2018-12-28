using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//定义一个全局的泛型委托
public delegate void DestroyDelegate<T>(T t);

[RequireComponent(typeof(DestroyManager))]
public class PoolManager :MonoBehaviour{

    #region 定义一些内部类
    /// <summary>
    /// 限制对象池数量上限的抽象类
    /// </summary>
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

        protected virtual void CleanByLRU()
        {
            if (NumOfPool == 0)
                Debug.LogWarning("LRU时数组为空");
        }
    }

    /// <summary>
    /// 同一物体的对象池（游戏物体或者非Mono的普通class）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class ObjectList<T> : NumLimit
    {
        #region 所有字段
        private List<T> list;
        private List<T> onUse;
        private List<T> destroyQueue;
        public DestroyDelegate<T> des;
        #endregion

        #region 构造方法
        public ObjectList():base()
        {
            list = new List<T>();
            onUse = new List<T>();
            destroyQueue = new List<T>();
            des = RemoveFromQueue;
        }

        public ObjectList(int max):base(max)
        {
            list = new List<T>();
            onUse = new List<T>();
            destroyQueue = new List<T>();
            des = RemoveFromQueue;
        }
        #endregion

        #region 对外的接口方法       
        public T GetObject()
        {
            T t = default(T);
            //尝试从销毁队列中获取
            if (destroyQueue.Count!=0)
            {
                t = destroyQueue[0];
                DestroyManager.Instance.CancelDestroy(t);
                destroyQueue.RemoveAt(0);
                onUse.Add(t);
                return t;
            }
            //尝试从对象池中获取
            if(NumOfPool>0)
            {
                t = list[list.Count - 1];
                list.Remove(t);
                NumOfPool--;
                onUse.Add(t);               
            }
            return t;
        }
        
        public void RecycleObject(T t)
        {
            NumOfPool++;
            list.Add(t);
            onUse.Remove(t);
        }
        
        public void AddToDestroyQueue(T t)
        {
            onUse.Remove(t);
            if (list.Remove(t))
                NumOfPool--;
            if (!destroyQueue.Contains(t))
                destroyQueue.Add(t);
            DestroyManager.Instance.StartDestroy(t,des);
        }
        
        public void CleanAllTheObjects()
        {
            des -= RemoveFromQueue;

            for (int i = 0; i < list.Count; i++)
            {
                des(list[i]);
            }
            for (int i = 0; i < onUse.Count; i++)
            {
                des(onUse[i]);
            }
            for (int i = 0; i < destroyQueue.Count; i++)
            {
                DestroyManager.Instance.CancelDestroy(destroyQueue[i]);
                des(destroyQueue[i]);
            }
            list.Clear();
            onUse.Clear();
            destroyQueue.Clear();
            des += RemoveFromQueue;
        }
        
        public void DestroyImmediately(T t)
        {
            onUse.Remove(t);
            list.Remove(t);
            destroyQueue.Remove(t);

            //使用委托完成删除
            DestroyManager.Instance.CancelDestroy(t);
            des(t);
            
            //以下是反射的实现方法
            //if(typeof(T)==typeof(GameObject))
            //{
            //    DestroyManager.Instance.CancelDestroy(t);
            //    DestroyManager.DestroyReflection(t);
            //}           
        }
        #endregion

        #region 有固定的调用场景的方法，外界不能随意调用        
        public void DestroyByTime()
        {
            if (list.Count != 0)
            {
                T t = list[0];
                AddToDestroyQueue(t);
            }
        }

        public void Print()
        {
            Debug.Log("List:" + list.Count);
            Debug.Log("OnUse:" + onUse.Count);
            Debug.Log("DestroyQueue:" + destroyQueue.Count);
        }
        
        //主要是为了把从缓存中获取的物体纳入对象池的管理
        public void AddToOnUse(T t)
        {
            if(!onUse.Contains(t))
                onUse.Add(t);
        }
        #endregion

        #region 私有方法
        //到达上限的删除方法
        protected override void CleanByLRU()
        {
            base.CleanByLRU();
            T t = list[0];
            AddToDestroyQueue(t);
        }
        //从列表中移除的方法，仅供委托使用
        private void RemoveFromQueue(T t)
        {
            destroyQueue.Remove(t);
        }
        #endregion
    }

    /// <summary>
    /// 同一类对象池的管理（dictionary大池）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class ListPool<T>
    {
        #region 所有字段
        private Dictionary<string, ObjectList<T>> objectList;
        public DestroyDelegate<T> destroyDelegate;
        #endregion

        #region 构造方法
        public ListPool()
        {
            objectList = new Dictionary<string, ObjectList<T>>();
        }
        #endregion

        #region 对外的接口方法
        public T Load(string name)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                list = new ObjectList<T>();
                if (destroyDelegate != null)
                {
                    list.des += destroyDelegate;
                }
                objectList.Add(name, list);               
                return default(T);
            }
            return list.GetObject();
        }

        public T Load(string name, int max)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                list = new ObjectList<T>(max);
                if (destroyDelegate != null)
                {
                    list.des += destroyDelegate;
                }
                objectList.Add(name, list);
                return default(T);
            }
            return list.GetObject();
        }

        public void Recycle(string name, T t)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                Debug.LogWarning("不存在对应的对象池:"+name);
                return;
            }
            list.RecycleObject(t);        
        }

        public void DestroyObj(string name, T t)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                Debug.LogWarning("不存在对应的对象池:" + name);
                return;
            }
            list.AddToDestroyQueue(t);
        }

        public void DestroyCertainObjs(string name)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                Debug.LogWarning("不存在对应的对象池:" + name);
                return;
            }
            list.CleanAllTheObjects();
        }

        public void DestroyImmediately(string name, T t)
        {
            ObjectList<T> list = null;
            if (!objectList.TryGetValue(name, out list))
            {
                Debug.LogWarning("不存在对应的对象池:" + name);
                return;
            }
            list.DestroyImmediately(t);
        }
        #endregion

        #region 有固定的调用场景的public方法
        public void Print(string s)
        {
            Debug.Log(s);
            foreach(KeyValuePair<string,ObjectList<T>> pair in objectList)
            {
                Debug.Log("------------------");
                Debug.Log(pair.Key);
                pair.Value.Print();
            }
        }
        
        //将从缓存中获取的物体纳入内存池管理
        public void AddToOnUse(string name, T t)
        {
            ObjectList<T> list = null;
            objectList.TryGetValue(name, out list);
            list.AddToOnUse(t);
        }
        
        public void DestroyByTime()
        {
            foreach(ObjectList<T> pool in objectList.Values)
            {
                pool.DestroyByTime();
            }
        }
        #endregion
    }

    /// <summary>
    /// 全局单一物体的对象池（软资源或者游戏物体缓存）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class SinglePool<T>:NumLimit where T : Object
    {
        #region 所有字段
        private Dictionary<string, T> objectList;
        private List<string> order;
        #endregion

        #region 构造方法
        public SinglePool():base()
        {
            objectList = new Dictionary<string, T>();
            order = new List<string>();
        }

        public SinglePool(int max):base(max)
        {
            objectList = new Dictionary<string, T>();
            order = new List<string>();
        }
        #endregion

        #region 对外的接口方法
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
                order.Add(name);
                objectList.Add(name, t);
            }
            return t;
        }
        #endregion
       
        #region  私有方法
        protected override void CleanByLRU()
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
        #endregion

        #region 有固定的调用场景的public方法
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
        #endregion
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

    [SerializeField]
    private bool DestroyTimeFlag = true;
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
            gameObjectPool.destroyDelegate = new DestroyDelegate<GameObject>(DestroyAchive);
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(DestroyByTime());
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
            gameObjectPool.AddToOnUse(name,obj);
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
        string name = type.Name;
        obj = (T)normalClassPool.Load(name);
        if (obj == null)
        {
            obj = new T();
            normalClassPool.AddToOnUse(name, obj);
        }
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

    #region 删除方法
    /// <summary>
    /// 非泛型方法，传入游戏物体，开始删除历程
    /// </summary>
    /// <param name="obj"></param>
    public void DestroyObject(GameObject obj)
    {
        obj.SetActive(false);
        gameObjectPool.DestroyObj(obj.name, obj);
    }

    /// <summary>
    /// 泛型方法，传入类型和对象，开始删除历程
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    public void DestroyObject<T>(T t)
    {
        string name = typeof(T).Name;
        normalClassPool.DestroyObj(name, t);
    }

    /// <summary>
    /// 非泛型方法，立刻删除某个游戏物体
    /// </summary>
    /// <param name="obj"></param>
    public void DestroyImmediately(GameObject obj)
    {
        gameObjectPool.DestroyImmediately(obj.name, obj);
    }

    /// <summary>
    /// 泛型方法，立刻删除某个非mono的类，需要传入类型和对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    public void DestroyImmediately<T>(T t)
    {
        string name = typeof(T).Name;
        normalClassPool.DestroyImmediately(name, t);
    }

    /// <summary>
    /// 删除某一路径下的所有被对象池监控的游戏物体
    /// </summary>
    /// <param name="s"></param>
    public void DestroyCertainObjs(string s)
    {
        gameObjectPool.DestroyCertainObjs(s);
    }

    /// <summary>
    /// 删除某一类的被对象池监控的所有实例，泛型方法传入类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DestroyCertainObjs<T>()
    {
        string name = typeof(T).Name;
        normalClassPool.DestroyCertainObjs(name);
    }
    #endregion

    #region 特殊方法
    //用于委托的删除函数方法体
    private void DestroyAchive(GameObject obj)
    {
        Destroy(obj);
    }

    public void Print()
    {
        gameObjectPool.Print("---------------gameObjectPool-------------------");
        normalClassPool.Print("---------------normalClassPool-------------------");
        softSourcePool.Print("---------------softScourcePool-------------------");
        gameObjectCache.Print("---------------gameObjectCache-------------------");
    }

    //固定时间遍历对象池，将池中的物体纳入删除历程
    IEnumerator DestroyByTime()
    {
        while(DestroyTimeFlag)
        {
            yield return new WaitForSeconds(20);
            gameObjectPool.DestroyByTime();
            normalClassPool.DestroyByTime();
        }
        yield return null;
    }
    #endregion
}