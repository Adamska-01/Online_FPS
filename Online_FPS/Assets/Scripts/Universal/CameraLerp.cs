using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLerp : MonoBehaviour
{
    private float timeElapsed;
    private float lerpDuration = 1.0f; 
    [SerializeField] private Transform deathTransf;
     

    public void LerpCamera()
    {
        StartCoroutine(StartLerp());
    }

    IEnumerator StartLerp()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while(timeElapsed < lerpDuration)
        {
            transform.position = Vector3.Lerp(startPos, deathTransf.position, timeElapsed / lerpDuration);
            transform.rotation = Quaternion.Lerp(startRot, deathTransf.rotation, timeElapsed / lerpDuration);

            timeElapsed += Time.deltaTime;

            yield return null;
        }
    }
}
