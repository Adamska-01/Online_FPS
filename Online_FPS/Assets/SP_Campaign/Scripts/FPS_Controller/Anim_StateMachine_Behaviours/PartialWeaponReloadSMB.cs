using UnityEngine;


public class PartialWeaponReloadSMB : ArmsBaseSMB
{
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // Internals 
    protected int reloadHash = Animator.StringToHash("Reload");
    protected int reloadRepeatHash = Animator.StringToHash("Reload Repeat");


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int reloadRepeat = animator.GetInteger(reloadRepeatHash);

        reloadRepeat = Mathf.Max(reloadRepeat - 1, 0);

        animator.SetInteger(reloadRepeatHash, reloadRepeat);

        if (characterMgr != null && reloadRepeat == 0)
        {
            characterMgr.ReloadWeapon_AnimatorCallback(weaponType);
            animator.SetBool(reloadHash, false);
        }
    }
}
