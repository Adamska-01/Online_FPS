using UnityEngine;


public class MuzzleFlashSMB : ArmsBaseSMB
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		foreach (var handler in callbackHandlers)
		{
			handler?.OnAction("Do_Muzzle", characterMgr);
		}
	}
}