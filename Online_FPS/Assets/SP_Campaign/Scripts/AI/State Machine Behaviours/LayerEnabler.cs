using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerEnabler : AIStateMachineLink
{
    public bool OnEnter = false;
    public bool OnExit = false;


    //Called prior to the first frame the animation assigned to this state.
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(stateMachine)
        {
            stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnEnter);
        }
    }

    //Called on the last frame the animator prior to leaving the state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine)
        {
            stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnExit);
        }
    }
}
