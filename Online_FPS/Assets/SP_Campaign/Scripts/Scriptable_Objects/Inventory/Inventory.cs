using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum AmmoAmountRequestType
{
    AllAmmo,
    NoWeaponAmmo,
    WeaponAmmoOnly
}

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
    public UnityEvent<InventoryItemWeapon>      OnWeaponDropped = new UnityEvent<InventoryItemWeapon>();   
    public UnityEvent<InventoryWeaponMountInfo> OnWeaponChange  = new UnityEvent<InventoryWeaponMountInfo>();


    //Standard API
    public abstract InventoryWeaponMountInfo    GetWeapon(int _mountIndex);
    public abstract InventoryAmmoMountInfo      GetAmmo(int _mountIndex);
    public abstract InventoryBackpackMountInfo  GetBackpack(int _mountIndex);

    //Audio properties/Functions
    public abstract bool                        AutoPlayOnPickup { get; set; }
    public abstract InventoryItemAudio          GetAudioRecording(int _recordingIndex);
    public abstract int                         GetActiveAudioRecording();
    public abstract int                         GetAudioRecordingCount();
    public abstract int                         PlayAudioRecordingCount();
    public abstract bool                        PlayAudioRecording(int _recordingIndex);
    public abstract void                        StopAudioRecording();


    public abstract bool                        AddItem(CollectableItem _collectableItem, bool _playAudio = true);
    public abstract void                        AssignWeapon(int _mountIndex, InventoryWeaponMountInfo _mountInfo);
    public abstract bool                        UseBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract bool                        ReloadWeaponItem(int _mountIndex, bool _playAudio = true);

    public abstract void                        DropBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract void                        DropWeaponItem(int _mountIndex, bool _playAudio = true);
    public abstract void                        DropAmmoItem(int _mountIndex, bool _playAudio = true);

    public abstract int                         GetAvailableAmmo(InventoryItemAmmo _ammo, AmmoAmountRequestType _requestType = AmmoAmountRequestType.NoWeaponAmmo); // Get all available rounds of a specific ammo type
    public abstract int                         DecreaseAmmoInWeapon(int _mountIndex, int _amount = 1);
    public abstract bool                        IsReloadAvailable(int _weaponMountIndex);
    public abstract InventoryMountInfo          Search(InventoryItem _matchItem); 
    public abstract int                         Remove(InventoryItem _matchItem); //Return the number of items removed
    public abstract bool                        RemoveWeapon(int _mountIndex);
    public abstract bool                        RemoveBackpack(int _mountIndex);
    public abstract bool                        RemoveAmmo(int _mountIndex);

    // Low Level Mount Array access
    public abstract List<InventoryWeaponMountInfo>   GetAllWeapons();
    public abstract List<InventoryAmmoMountInfo>     GetAllAmmo();
    public abstract List<InventoryBackpackMountInfo> GetAllBackpack();

}
