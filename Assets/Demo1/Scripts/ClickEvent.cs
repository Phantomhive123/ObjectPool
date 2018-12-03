using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickEvent : MonoBehaviour {

    List<GameObject> objectlist=new List<GameObject>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Load(string str)
    {
        GameObject obj = ObjectPool.Instance.LoadObject(str);
        objectlist.Add(obj);
    }

    public void Recycle()
    {
        if(objectlist.Count!=0)
        {
            GameObject obj = objectlist[0];
            objectlist.Remove(obj);
            ObjectPool.Instance.RecycleObject(obj);
        }
        else
        {
            Debug.Log("No object to recycle");
        }
    }

}
