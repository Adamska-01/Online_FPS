using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public abstract class InventoryMountInfo
{

}

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
    public abstract InventoryWeaponMountInfo GetWeapon(int mountIndex);
    public abstract InventoryAmmoMountInfo GetAmmo(int mountIndex);
    public abstract InventoryBackpackMountInfo GetBackpack(int mountIndex);
    public abstract bool UseBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract bool ReloadWeaponItem(int _mountIndex, bool _playAudio = true);
    public abstract void DropBackpackItem(int _mountIndex, bool _playAudio = true);
    public abstract void DropWeaponItem(int _mountIndex, bool _playAudio = true);
    public abstract void DropAmmoItem(int _mountIndex, bool _playAudio = true);
}
