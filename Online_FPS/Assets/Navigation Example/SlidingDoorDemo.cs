using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DoorState
{
    Open,
    Animating, 
    Closed
};


public class SlidingDoorDemo : MonoBehaviour
{
    public float slidingDistance = 4.0f;
    public float duration = 1.5f;
    public AnimationCurve jumpCurve = new AnimationCurve();

    private Vector3 openPos = Vector3.zero;
    private Vector3 closePos = Vector3.zero;
    private DoorState doorState = DoorState.Closed;

    void Start()
    {
        closePos = transform.position;
        openPos = closePos + (transform.right * slidingDistance);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && doorState != DoorState.Animating)
        {
            //Toggle open/close
            StartCoroutine(AnimateDoor((doorState == DoorState.Open) ? DoorState.Closed : DoorState.Open));
        }
    }

    private IEnumerator AnimateDoor(DoorState newState)
    {
        //Set animation state
        doorState = DoorState.Animating;
        float time = 0.0f;

        //Determine start and end positions 
        Vector3 startPos = (newState == DoorState.Open) ? closePos : openPos;
        Vector3 endPos = (newState == DoorState.Open) ? openPos : closePos;
        
        while(time < duration)
        {
            //Lerp
            float t = time / duration;
            transform.position = Vector3.Lerp(startPos, endPos, jumpCurve.Evaluate(t));


            time += Time.deltaTime;

            yield return null;
        }

        //Set state
        doorState = newState;

        //Sanity check (force end pos)
        transform.position = endPos; 
    }
}
