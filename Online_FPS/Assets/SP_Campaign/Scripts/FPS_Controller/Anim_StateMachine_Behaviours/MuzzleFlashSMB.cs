using UnityEngine;


public class MuzzleFlashSMB : ArmsBaseSMB
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		callbackHandler?.OnAction("Do_Muzzle", characterMgr);
	}
}