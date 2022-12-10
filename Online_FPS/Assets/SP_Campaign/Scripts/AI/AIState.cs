using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState : MonoBehaviour
{
    protected AIStateMachine stateMachine;
    public void SetStateMachine(AIStateMachine _machine) { stateMachine = _machine; }

    //Default handlers
    public virtual void OnEnterState() { }
    public virtual void OnExitState() { }
    public virtual void OnAnimatorUpdated() 
    {
        if(stateMachine.UseRootPosition)
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
}
