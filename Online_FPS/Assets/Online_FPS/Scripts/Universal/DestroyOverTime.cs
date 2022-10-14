using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    public float time;
    private float currentTime;


    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= time)
            Destroy(gameObject);
    }
}
