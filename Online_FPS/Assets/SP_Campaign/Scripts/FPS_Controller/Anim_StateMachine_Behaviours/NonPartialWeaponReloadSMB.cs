using UnityEngine;


public class NonPartialWeaponReloadSMB : ArmsBaseSMB
{
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // Internals 
    protected int reloadHash = Animator.StringToHash("Reload");


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        characterMgr?.ReloadWeapon_AnimatorCallback(weaponType);

        animator.SetBool(reloadHash, false);
    }
}
