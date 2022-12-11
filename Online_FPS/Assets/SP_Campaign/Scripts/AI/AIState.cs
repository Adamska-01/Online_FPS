using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState : MonoBehaviour
{
    protected AIStateMachine stateMachine;
    public virtual void SetStateMachine(AIStateMachine _machine) { stateMachine = _machine; }

    //Default handlers
    public virtual void OnEnterState() { }
    public virtual void OnExitState() { }
    public virtual void OnAnimatorUpdated()
    {
        if (stateMachine.UseRootPosition)
        {
            stateMachine.NavAgent.velocity = stateMachine.Anim.deltaPosition / Time.deltaTime;
        }
        if (stateMachine.UseRootRotation)
        {
            stateMachine.transform.rotation = stateMachine.Anim.rootRotation;
        }
    }
    public virtual void OnAnimatorIKUpdated() { }
    public virtual void OnTriggerEvent(AITriggerEventType _eventType, Collider _other) { }
    public virtual void OnDestinationReached(bool _isReached) { }

    public abstract AIStateType OnUpdate();
    public abstract AIStateType GetStateType();


    //Calculate sphere collider world position (taking parents' scales into account)
    public static void ConvertSphereColliderToWorldSpace(SphereCollider col, out Vector3 pos, out float radius)
    {
        //Default values
        pos     = Vector3.zero;
        radius  = 0.0f;

        //if no valid sphere collider return 
        if (col == null)
            return;

        //Calculate world space position of sphere center (Take parent scales into account)
        pos = col.transform.position;
        pos.x += col.center.x * col.transform.lossyScale.x;
        pos.y += col.center.y * col.transform.lossyScale.y;
        pos.z += col.center.z * col.transform.lossyScale.z;


        //Calculate world space radius of sphere (Take parent scales into account)
        radius = Mathf.Max(col.radius * col.transform.lossyScale.x,
                           col.radius * col.transform.lossyScale.y,
                           col.radius * col.transform.lossyScale.z);
    }

    //Returns the signed anfle between 2 vectors (in degrees)
    public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector)
    {
        if (fromVector == toVector)
            return 0.0f;

        //Get angle between the two vectors
        float angle = Vector3.Angle(fromVector, toVector);

        //Get sign of the angle using cross product 
        Vector3 cross = Vector3.Cross(fromVector, toVector);
        angle *= Mathf.Sign(cross.y);

        return angle;
    }
}
