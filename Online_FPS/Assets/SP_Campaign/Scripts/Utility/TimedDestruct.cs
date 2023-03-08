using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestruct : MonoBehaviour
{
    [SerializeField] private float time = 10.0f;


    private void Awake()
    {
        Invoke("DestroyNow", time);
    }


    private void DestroyNow()
    {
        Destroy(gameObject);
    }
}
