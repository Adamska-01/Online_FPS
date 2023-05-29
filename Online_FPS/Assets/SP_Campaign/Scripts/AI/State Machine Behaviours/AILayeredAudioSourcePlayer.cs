using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AILayeredAudioSourcePlayer : AIStateMachineLink
{
    //Inspector Assigned
    [SerializeField] private AudioCollection collection = null;
    [SerializeField] private int bank = 0;
    [SerializeField] private bool looping = true;
    [SerializeField] private bool stopOnExit = false;

    //Private
    private float prevLayerWeight = 0.0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine == null)
            return;

        float layerWeight = animator.GetLayerWeight(layerIndex);

        if(collection != null)
        {
            //0.5f weight treshold because the weights (in my case) are either 0 or 1
            //and also because the layer 0 does not have a weight
            if(layerIndex == 0 || layerWeight > 0.5f)
            {
                Debug.Log("Playing layered sound");
                stateMachine.PlayAudio(collection, bank, layerIndex, looping);
            }
            else
            {
                Debug.Log("Stopping");
                stateMachine.StopAudio(layerIndex);
            }
        }

        //Store layer weight to detect changes mid animation
        prevLayerWeight = layerWeight;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine == null)
            return;

        float layerWeight = animator.GetLayerWeight(layerIndex);

        //If its changes we might need to start or stop the audio layer assigned to it
        if(layerWeight != prevLayerWeight && collection != null)
        {
            if(layerWeight != prevLayerWeight && collection != null)
            {
                if (layerWeight > 0.5f)
                {
                    Debug.Log("Playing layered sound");
                    stateMachine.PlayAudio(collection, bank, layerIndex, true);
                }
                else
                {
                    Debug.Log("Stopping");
                    stateMachine.StopAudio(layerIndex);
                }
            }
        }

        //Store layer weight to detect changes mid animation
        prevLayerWeight = layerWeight;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine == null && stopOnExit)
        {
            stateMachine.StopAudio(layerIndex);
        }
    }
}
