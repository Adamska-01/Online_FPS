using UnityEngine;


public class ShotgunWeaponReloadSMB : PartialWeaponReloadSMB
{
	// Internals
	protected int commandStreamHash = Animator.StringToHash("Command Stream");
	protected float previousCommandValue = 0.0f;


	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		float commandValue = animator.GetFloat(commandStreamHash);

		if (callbackHandlers.Count <= 0 || commandValue.Equals(previousCommandValue) || commandValue.Equals(0.0f))
			return;

		previousCommandValue = commandValue;

		var actionType = string.Empty;
		switch(commandValue)
		{
			case 0.0f:
				actionType = "Disable Shotgun Shell";
				break;

			case 2.0f:
				actionType = "Enable Shotgun Shell";
				break;
		}

		if (string.IsNullOrEmpty(actionType))
			return;

		foreach (var handler in callbackHandlers)
		{
			handler.OnAction("Disable Shotgun Shell");
		}
	}
}