using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISoundEmitter : MonoBehaviour
{
    //Inspector Assigned
    [SerializeField] private float decayRate = 1.0f;

    //Internal 
    private SphereCollider col = null;
    private float sourceRadius = 0.0f;
    private float targetRadius = 0.0f;
    private float interpolator = 0.0f;
    private float interpolatorSpeed = 0.0f;


    private void Awake()
    {
        col = GetComponent<SphereCollider>();

        if (col == null)
            return;

        //Set radius values
        sourceRadius = targetRadius = col.radius;

        //Setup Interpolator 
        interpolator = 0.0f;
        if (decayRate > 0.02f)
        {
            interpolatorSpeed = 1.0f / decayRate; //eg. 1.0f/2.0f = .5f (will take 2sec for interpolator to go from 0 to 1)
        }
        else
        {
            interpolatorSpeed = 0.0f; //No interpolation
        }
    }

    private void Update()
    {
        if (col == null)
            return;

        interpolator = Mathf.Clamp01(interpolator + Time.deltaTime * interpolatorSpeed);
        col.radius = Mathf.Lerp(sourceRadius, targetRadius, interpolator);

        if (col.radius < Mathf.Epsilon)
        {
            col.enabled = false;
        }
        else
        {
            col.enabled = true;
        }
    }


    public void SetRadius(float newRadius, bool instantResize = false)
    {
        if (col == null || newRadius == targetRadius)
            return;

        //Don't apply interpolation if instantResize == true OR the newRadius is smaller than the current
        sourceRadius = (instantResize || newRadius > col.radius) ? newRadius : col.radius;
        targetRadius = newRadius;
        interpolator = 0.0f;
    }
}
