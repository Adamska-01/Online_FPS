
using RootMotion.Demos;
using UnityEngine;

public class ActivateFlashlightSMB : ArmsBaseSMB
{
    // Inspector-Assigned
    public bool activate = false;
    public FlashlightType flashlightType = FlashlightType.Primary;

    // Internals
    private int commandStreamHash = Animator.StringToHash("Command Stream");    // The current sub-state machine to play for the selected weapon
    private int flashlightHash    = Animator.StringToHash("Flashlight");        // Is the current weapon armed
    protected bool done = false;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        characterMgr?.ActivateFlashlightMesh_AnimatorCallback(true, flashlightType);

        // We haven't processed a command for this state yet
        done = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (characterMgr != null && done)
            return;

        if (!animator.GetBool(flashlightHash) && activate)
            return;

        float commandValue = animator.GetFloat(commandStreamHash);
        if(commandValue > 0.75f)
        {
            Debug.Log("Activating Flashlight");
            characterMgr.ActivateFlashlightLight_AnimatorCallback(activate, flashlightType);
            done = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (characterMgr != null && !activate)
        {
            characterMgr.ActivateFlashlightMesh_AnimatorCallback(false, flashlightType);
            characterMgr.ActivateFlashlightLight_AnimatorCallback(false, flashlightType);
        }
    }
}
