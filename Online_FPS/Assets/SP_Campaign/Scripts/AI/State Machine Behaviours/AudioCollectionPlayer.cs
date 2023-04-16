using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCollectionPlayer : AIStateMachineLink
{
    //Inspector-Assigned
    [SerializeField] private ComChannelName commandChannel = ComChannelName.ComChannel1; //The bank to play
    [SerializeField] private AudioCollection collection = null;
    [SerializeField] private CustomCurve customCurve = null;

    //Private
    private int previousCommand = 0;
    private AudioManager audioManager = null;
    private int commandChanelHash = 0;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        audioManager = AudioManager.Instance;
        previousCommand = 0;

        //TODO: Store hashes in state machine loop
        if (commandChanelHash == 0)
        {
            commandChanelHash = Animator.StringToHash(commandChannel.ToString());
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Don't play sounds if the layer weight is == 0
        if (layerIndex != 0 && animator.GetLayerWeight(layerIndex).Equals(0.0f))
            return;

        if (stateMachine == null)
            return;

        //Process Animation curve (from either the animation itself or a custom one)
        //In the customCommand we subtract the int portion of the normalizedTime we are only interested in the decimal portion
        //Fun fact: the int portion says how many times the animation has looped, wheras the decimal portion says the actial anim time
        int customCommand = (customCurve == null) ? 0 : Mathf.FloorToInt(customCurve.Evaluate(stateInfo.normalizedTime - (long)stateInfo.normalizedTime));
        int command = customCommand != 0 ? customCommand : Mathf.FloorToInt(animator.GetFloat(commandChanelHash)); 
        if (command != previousCommand && command > 0 && audioManager != null && collection != null)
        {
            int bank = Mathf.Max(0, Mathf.Min(command - 1, collection.BankCount - 1));
            //Play sound
            audioManager.PlayOneShotSound(collection.AudioGroup,
                                          collection[bank],
                                          stateMachine.transform.position,
                                          collection.Volume,
                                          collection.SpatialBlend,
                                          collection.Priority);
        }

        previousCommand = command;
    }
}
