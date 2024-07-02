using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchWeaponSMB : ArmsBaseSMB
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        characterMgr?.WPNController?.DisableWeapon_AnimatorCallback();    
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        characterMgr?.WPNController?.EnableWeapon_AnimatorCallback();    
    }
}
