using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GC1
{
    public class GarbageCollection : MonoBehaviour
    {

        List<Test> list = new List<Test>();

        // Use this for initialization
        void Start()
        {
            Test t1 = new Test("t1");
            Test t2 = new Test("t2");
            Test t3 = new Test("t3");
            list.Add(t1);
            list.Add(t2);
            list.Add(t3);
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                if(list.Count!=0)
                {
                    Debug.Log("remove:"+list[0].s);
                    list.RemoveAt(0);                  
                }
            }
            else if(Input.GetMouseButtonDown(1))
            {
                System.GC.Collect();
            }
        }
    }

    class Test
    {
        public string s = null;

        public Test(string str)
        {
            s = str;
            Debug.Log("structure:"+s);
        }
        ~Test()
        {
            Debug.Log("destructor:"+s);
        }
    }
}



