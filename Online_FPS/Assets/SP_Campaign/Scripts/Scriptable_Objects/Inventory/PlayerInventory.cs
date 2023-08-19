using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Player Inventory", fileName = "New Player Inventory")]
public class PlayerInventory : Inventory, ISerializationCallbackReceiver
{
    //Inspector-Assigned
    [Header("Mount Configuration and Starting Items")]
    [SerializeField] protected List<InventoryWeaponMountInfo> weaponMounts = new List<InventoryWeaponMountInfo>();
    [SerializeField] protected List<InventoryAmmoMountInfo> ammoMounts = new List<InventoryAmmoMountInfo>();
    [SerializeField] protected List<InventoryBackpackMountInfo> backpackMounts = new List<InventoryBackpackMountInfo>();

    [Header("Shared Variables")]
    [SerializeField] protected SharedTimedStringQueue notificationQueue = null;

    [Header("Shared Variables - Broadcasters")]
    [SerializeField] protected SharedVector3 playerPosition = null;
    [SerializeField] protected SharedVector3 playerDirection = null;


    //Internals
    protected List<InventoryWeaponMountInfo> weapons = new List<InventoryWeaponMountInfo>();
    protected List<InventoryAmmoMountInfo> ammos = new List<InventoryAmmoMountInfo>();
    protected List<InventoryBackpackMountInfo> backpacks = new List<InventoryBackpackMountInfo>();


    public override InventoryWeaponMountInfo GetWeapon(int mountIndex)
    {
        //Only 2 weapons allowed 
        if (mountIndex < 0 || mountIndex > 1 || mountIndex >= weapons.Count)
            return null;

        return weapons[mountIndex];
    }
    public override InventoryAmmoMountInfo GetAmmo(int mountIndex)
    {
        if (mountIndex < 0 || mountIndex >= ammos.Count)
            return null;

        return ammos[mountIndex];
    }
    public override InventoryBackpackMountInfo GetBackpack(int mountIndex)
    {
        if (mountIndex < 0 || mountIndex >= backpacks.Count)
            return null;

        return backpacks[mountIndex];
    }



    //-------------------------------------------------------------------
    //-------------------------- Serialization --------------------------
    //-------------------------------------------------------------------
    public void OnAfterDeserialize()
    {
        weapons.Clear();
        ammos.Clear();
        backpacks.Clear();

        //Create deep copies of the lists
        foreach (InventoryWeaponMountInfo info in weaponMounts)
        {
            InventoryWeaponMountInfo clone = new InventoryWeaponMountInfo();
            clone.condition = info.condition;
            clone.inGunRounds = info.inGunRounds;
            clone.weapon = info.weapon;

            weapons.Add(clone);

            //This implementation supports only 2 weapons so ignore any others specified
            if (weapons.Count == 2)
                break;
        }
        foreach (InventoryAmmoMountInfo info in ammoMounts)
        {
            InventoryAmmoMountInfo clone = new InventoryAmmoMountInfo();
            clone.ammo = info.ammo;
            clone.rounds = info.rounds;

            ammos.Add(clone);
        }
        foreach (InventoryBackpackMountInfo info in backpackMounts)
        {
            InventoryBackpackMountInfo clone = new InventoryBackpackMountInfo();
            clone.item = info.item;

            backpacks.Add(clone);
        }
    }
    public void OnBeforeSerialize() { } //Not needed
}
