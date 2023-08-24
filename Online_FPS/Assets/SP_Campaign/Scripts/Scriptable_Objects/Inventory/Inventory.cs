using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public abstract class InventoryMountInfo
{ }

[System.Serializable]
public class InventoryWeaponMountInfo :  InventoryMountInfo
{
    public InventoryItemWeapon weapon = null;
    [Range(0.0f, 100.0f)] public float condition = 100.0f;
    [Range(0.0f, 100.0f)] public int inGunRounds = 0;
}

[System.Serializable]
public class InventoryAmmoMountInfo : InventoryMountInfo
{
    public InventoryItemAmmo ammo = null;
    public int rounds = 0;
}

[System.Serializable]
public class InventoryBackpackMountInfo : InventoryMountInfo
{
    public InventoryItem item = null;
}


// --------------------------------------------------------------------------------
// CLASS	:	Inventory
// DESC		:	Base class for an inventory object type
// --------------------------------------------------------------------------------
public abstract class Inventory : ScriptableObject
{
    //Weapon Events
    public UnityEvent<InventoryItemWeapon> OnWeaponDropped = new UnityEvent<InventoryItemWeapon>();   
    public UnityEvent<InventoryWeaponMountInfo> OnWeaponChange = new UnityEvent<InventoryWeaponMountInfo>();


    public abstract InventoryWeaponMountInfo GetWeapon(int mountIndex);
    public abstract InventoryAmmoMountInfo GetAmmo(int mountIndex);
    public abstract InventoryBackpackMountInfo GetBackpack(int mountIndex);

    public abstract bool AddItem(CollectableItem _collectableItem, bool _playAudio = true);
    public abstract void AssignWeapon(int _mountIndex, InventoryWeaponMountInfo _mountInfo);
    public abstract bool UseBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract bool ReloadWeaponItem(int _mountIndex, bool _playAudio = true);

    public abstract void DropBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract void DropWeaponItem(int _mountIndex, bool _playAudio = true);
    public abstract void DropAmmoItem(int _mountIndex, bool _playAudio = true);

    public abstract int GetAvailableAmmo(InventoryItemAmmo _ammo, bool _includeWeapon = false); //Get all available rounds of a specific ammo type
    public abstract bool IsReloadAvailable(int _weaponMountIndex);
    public abstract InventoryMountInfo Search(InventoryItem _matchItem); 
    public abstract int Remove(InventoryItem _matchItem); //Return the number of items removed
    public abstract bool RemoveWeapon(int _mountIndex);
    public abstract bool RemoveBackpack(int _mountIndex);
    public abstract bool RemoveAmmo(int _mountIndex);

    // Low Level Mount Array access
    public abstract List<InventoryWeaponMountInfo> GetAllWeapons();
    public abstract List<InventoryAmmoMountInfo> GetAllAmmo();
    public abstract List<InventoryBackpackMountInfo> GetAllBackpack();

}
