using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_CoroutineRunner : MonoBehaviour
{
    //Singleton
    private static SO_CoroutineRunner instance = null;
    public static SO_CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SO_CoroutineRunner>();
            }

            return instance;
        }
    }
}
