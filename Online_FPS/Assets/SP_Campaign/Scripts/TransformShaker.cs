using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TransformShaker : MonoBehaviour
{
    // Inspector-Assigned 
    [SerializeField] SharedVector3 shakeVector = null;
    [SerializeField] float magnitudeScale = 1.0f;

    // Internals 
    Vector3 localPosition = Vector3.zero;


    void Start()
    {
        localPosition = transform.localPosition;        
    }


    void Update()
    {
        if(shakeVector != null)
        {
            transform.localPosition = localPosition + (shakeVector.Value * magnitudeScale);
        }   
    }
}
