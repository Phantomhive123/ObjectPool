using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TestExample : MonoBehaviour {

    List<GameObject> gameobjList=new List<GameObject>();
    List<object> objList=new List<object>();
    List<object> objList1 = new List<object>();
    public GameObject TheObj;

    //List<Object> softScource=new List<Object>();

	// Use this for initialization
	void Start () {
        PoolManager.Instance.LoadObject<Texture2D>("Square");
        PoolManager.Instance.LoadObject<Texture2D>("Cube");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Print()
    {
        ClearConsole();
        PoolManager.Instance.Print();
    }

    public void LoadGameObj(string s)
    {
        GameObject obj = PoolManager.Instance.LoadObject(s);
        gameobjList.Add(obj);
    }

    public void LoadNormaClass()
    {
        test obj = PoolManager.Instance.LoadObject<test>();
        objList.Add(obj);
    }

    public void RecycleGameObj()
    {
        if (gameobjList.Count == 0)
            Debug.Log("no GameObject to recycle");
        else
        {
            PoolManager.Instance.RecycleObject(gameobjList[gameobjList.Count - 1]);
            gameobjList.Remove(gameobjList[gameobjList.Count - 1]);
        }
    }

    public void RecycleNormalClass()
    {
        if (objList.Count == 0)
            Debug.Log("no normal class to recycle");
        else
        {
            PoolManager.Instance.RecycleObject<test>((test)objList[objList.Count - 1]);
            objList.Remove(objList[objList.Count - 1]);
        }
    }

    public void LoadNormalClass1()
    {
        another a = PoolManager.Instance.LoadObject<another>();
        objList1.Add(a);
    }

    public void RecycleNormalClass1()
    {
        if (objList1.Count == 0)
            Debug.Log("no normal class to recycle");
        else
        {
            PoolManager.Instance.RecycleObject<another>((another)objList1[objList1.Count - 1]);
            objList1.Remove(objList1[objList1.Count - 1]);
        }
    }

    public void DestroyCertainGameObjects(string s)
    {
        for(int i=0;i<gameobjList.Count;i++)
        {
            if (gameobjList[i].name == s)
                gameobjList.RemoveAt(i);
        }
        PoolManager.Instance.DestroyCertainObjs(s);
    }

    public void DestroyGameObj()
    {
        gameobjList.Remove(TheObj);
        PoolManager.Instance.DestroyObject(TheObj);
    }

    public void DestroyGameObjImmediately()
    {
        gameobjList.Remove(TheObj);
        PoolManager.Instance.DestroyImmediately(TheObj);
    }

    [MenuItem("Tools/Clear Console %&c")]
    public static void ClearConsole()
    {
        Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
        Type logEntries = assembly.GetType("UnityEditor.LogEntries");
        MethodInfo clearConsoleMethod = logEntries.GetMethod("Clear");
        clearConsoleMethod.Invoke(new object(), null);
    }
}

public class test
{

}

public class another
{

}
