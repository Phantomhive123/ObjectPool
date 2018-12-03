using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool {

    private static ObjectPool instance;

    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
                instance = new ObjectPool();
            return instance;
        }
    }

    private Dictionary<string, List<GameObject>> gameObjectPool;

    public ObjectPool()
    {
        gameObjectPool = new Dictionary<string, List<GameObject>>();
    }

    public GameObject LoadObject(string name)
    {
        List<GameObject> objList = null;
        GameObject obj = null;
        if(!gameObjectPool.TryGetValue(name,out objList))
        {
            objList = new List<GameObject>();
            gameObjectPool.Add(name, objList);
        }

        if(objList.Count == 0)
        {
            obj = Resources.Load<GameObject>(name);
            obj = Object.Instantiate(obj);
            obj.name = name;
        }
        else
        {
            obj = objList[objList.Count - 1];
            obj.SetActive(true);
            objList.Remove(obj);
        }
        PrintDictionary();
        return obj;
    }

    public void RecycleObject(GameObject obj)
    {
        List<GameObject> objList = null;
        if(!gameObjectPool.TryGetValue(obj.name,out objList))
        {
            objList = new List<GameObject>();
            gameObjectPool.Add(obj.name, objList);
        }

        obj.SetActive(false);
        objList.Add(obj);
        PrintDictionary();
    }

    private void PrintDictionary()
    {
        foreach(KeyValuePair<string, List<GameObject>> l in gameObjectPool)
        {
            Debug.Log("name:" + l.Key+"   "+ "number:" + l.Value.Count);
        }
    }
}
