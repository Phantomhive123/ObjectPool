using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestExample : MonoBehaviour {

    List<GameObject> gameobjList=new List<GameObject>();
    List<Object> objList=new List<Object>();
    List<Object> softScource=new List<Object>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Print()
    {
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
}

public class test: Object
{

}
