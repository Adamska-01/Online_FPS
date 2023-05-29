using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingSky : MonoBehaviour
{
    [SerializeField] protected Material skyMaterial = null;
    [SerializeField] protected float speed = 1.0f;

    //Internals 
    protected float angle = 0.0f;
    protected float orignialAngle = 0.0f;

    private void OnEnable()
    {
        if(skyMaterial != null)
        {
            orignialAngle = angle = skyMaterial.GetFloat("_Rotation");
        }
    }

    private void OnDisable()
    {
        if(skyMaterial != null)
        {
            skyMaterial.SetFloat("_Rotation", orignialAngle);
        }
    }

    private void Update()
    {
        if (skyMaterial == null)
            return;

        angle += speed * Time.deltaTime;
        
        //Wrap angle
        if(angle > 360.0f)
        {
            angle -= 360.0f;
        }
        else if (angle < 0.0f)
        {
            angle += 360.0f;
        }

        skyMaterial.SetFloat("_Rotation", angle);
    }
}
