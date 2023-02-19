using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink
{
    [SerializeField] private int rootPosition = 0;
    [SerializeField] private int rootRotation = 0;

    private bool rootMotionProcessed = false;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(stateMachine)
        {
            stateMachine.AddRootMotionRequest(rootPosition, rootRotation);
            rootMotionProcessed = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine && rootMotionProcessed)
        {
            stateMachine.AddRootMotionRequest(-rootPosition, -rootRotation);
            rootMotionProcessed = false;
        }
    }
}
