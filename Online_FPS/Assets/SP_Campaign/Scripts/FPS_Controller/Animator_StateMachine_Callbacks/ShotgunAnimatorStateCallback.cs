using UnityEngine;


public class ShotgunAnimatorStateCallback : WeaponAnimatorStateCallback
{
	[SerializeField] 
	GameObject shotgunShell = null;


	public override void OnAction(string _context, CharacterManager _chrMgr = null)
	{
		base.OnAction(_context, _chrMgr);
		
		if (shotgunShell == null)
			return;

		switch(_context)
		{
			case "Disable Shotgun Shell":
				shotgunShell.SetActive(false);
				break;
			case "Enable Shotgun Shell":
				shotgunShell.SetActive(true);
				break;
		}
	}
}