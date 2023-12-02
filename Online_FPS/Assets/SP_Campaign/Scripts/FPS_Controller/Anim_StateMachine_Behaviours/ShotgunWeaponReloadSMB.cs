using UnityEngine;


public class ShotgunWeaponReloadSMB : PartialWeaponReloadSMB
{
    // Internals
    protected int commandStreamHash = Animator.StringToHash("Command Stream");
    protected float previousCommandValue = 0.0f;


    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float commandValue = animator.GetFloat(commandStreamHash);
        
        if(callbackHandler != null && !commandValue.Equals(previousCommandValue) && !commandValue.Equals(0.0f))
        {
            previousCommandValue = commandValue;

            if(commandValue.Equals(0.0f))
            {
                callbackHandler.OnAction("Disable Shotgun Shell");
            }
            else if (commandValue.Equals(2.0f))
            {
                callbackHandler.OnAction("Enable Shotgun Shell");
            }
        }
    }
}
