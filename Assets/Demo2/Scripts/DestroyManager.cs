﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyManager : MonoBehaviour {

    private Dictionary<object, Coroutine> destroyList;

    private static DestroyManager instance;

    public static DestroyManager Instance
    {
        get
        {
            return instance;
        }
        set { }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            DestroyImmediate(gameObject);
    }

    // Use this for initialization
    void Start () {

        destroyList = new Dictionary<object, Coroutine>();

	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void StartDestroy<T>(T t, DestroyDelegate<T> destroyDelegate)
    {
        Coroutine handle = StartCoroutine(DelayDestroy(t,destroyDelegate));
        destroyList.Add(t, handle);
    }

    public void CancelDestroy(object obj)
    {
        Coroutine coro = null;
        destroyList.TryGetValue(obj,out coro);
        if(coro!=null)
        {
            StopCoroutine(coro);
            destroyList.Remove(obj);            
        }
    }

    IEnumerator DelayDestroy<T>(T t, DestroyDelegate<T> destroyDelegate)
    {
        yield return new WaitForSeconds(10f);
        if(destroyList.ContainsKey(t))
        {
            destroyDelegate(t);
        }
        else
        {
            Debug.LogWarning("GameObject has been destroyed");
        }
    }
}
