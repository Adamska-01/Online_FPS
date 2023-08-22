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



    public override InventoryWeaponMountInfo GetWeapon(int _mountIndex)
    {
        //Only 2 weapons allowed 
        if (_mountIndex < 0 || _mountIndex > 1 || _mountIndex >= weapons.Count)
            return null;

        return weapons[_mountIndex];
    }

    public override InventoryAmmoMountInfo GetAmmo(int _mountIndex)
    {
        if (_mountIndex < 0 || _mountIndex >= ammos.Count)
            return null;

        return ammos[_mountIndex];
    }

    public override InventoryBackpackMountInfo GetBackpack(int _mountIndex)
    {
        if (_mountIndex < 0 || _mountIndex >= backpacks.Count)
            return null;

        return backpacks[_mountIndex];
    }
   
    public override bool AddItem(CollectableItem collectableItem)
    {
        return true;
    }

    public override bool UseBackpackItem(int _mountIndex, bool _playAudio = true)
    {
        if (_mountIndex < 0 || _mountIndex >= backpacks.Count)
            return false;

        //Get backpack mount from index
        InventoryBackpackMountInfo backpackMountInfo = backpacks[_mountIndex];
        if (backpackMountInfo == null || backpackMountInfo.item == null)
            return false;

        //Get the item object from the mount
        InventoryItem backpackItem = backpackMountInfo.item;

        //Get the player position for playing audio (it is 2D audio at the moment, but you never know what comes next..)
        Vector3 pos = playerPosition != null ? playerPosition.Value : Vector3.zero;

        //Consume item and replace it if it returns a replacement item (eg. use can of beans and return an empty can of bean)
        InventoryItem replacementItem = backpackItem.Use(pos, _playAudio);
        backpacks[_mountIndex].item = replacementItem;

        return true;
    }

    public override bool ReloadWeaponItem(int _mountIndex, bool _playAudio = true)
    {
        Debug.Log("Weapon reloaded");
        return true;
    }

    public override void DropBackpackItem(int _mountIndex, bool _playAudio = true)
    {
        if (_mountIndex < 0 || _mountIndex >= backpacks.Count)
            return;

        //Get backpack mount from index
        InventoryBackpackMountInfo backpackMountInfo = backpacks[_mountIndex];
        if (backpackMountInfo == null || backpackMountInfo.item == null)
            return;

        //Calculate spawn position (in front of the player)
        Vector3 pos = playerPosition != null ? playerPosition.Value : Vector3.zero; 
        pos += playerDirection != null ? playerDirection.Value : Vector3.zero;
       
        //Drop
        backpackMountInfo.item.Drop(pos, _playAudio);   

        //Nullify the slot so it is empty
        backpacks[_mountIndex].item = null;
    }

    public override void DropWeaponItem(int _mountIndex, bool _playAudio = true)
    {
        if (_mountIndex < 0 || _mountIndex >= weapons.Count)
            return;

        //Get backpack mount from index
        InventoryWeaponMountInfo weaponMountInfo = weapons[_mountIndex];
        if (weaponMountInfo == null || weaponMountInfo.weapon == null)
            return;

        InventoryItemWeapon weapon = weapons[_mountIndex].weapon;

        //Calculate spawn position (in front of the player)
        Vector3 pos = playerPosition != null ? playerPosition.Value : Vector3.zero;
        pos += playerDirection != null ? playerDirection.Value : Vector3.zero;

        //Drop
        CollectableWeapon sceneWeapon = weapon.Drop(pos, _playAudio) as CollectableWeapon;
        //Copy over the weapon instance data
        if (sceneWeapon != null)
        {
            sceneWeapon.Condition = weaponMountInfo.condition;
            sceneWeapon.Rounds = weaponMountInfo.inGunRounds;
        }

        //Nullify the slot so it is empty
        weapons[_mountIndex].weapon = null;
        weapons[_mountIndex].condition = 100.0f;
        weapons[_mountIndex].inGunRounds = 0;
    }
    
    public override void DropAmmoItem(int _mountIndex, bool _playAudio = true)
    {
        if (_mountIndex < 0 || _mountIndex >= ammos.Count)
            return;

        //Get backpack mount from index
        InventoryAmmoMountInfo ammoMountInfo = ammos[_mountIndex];
        if (ammoMountInfo == null || ammoMountInfo.ammo == null)
            return;

        //Calculate spawn position (in front of the player)
        Vector3 pos = playerPosition != null ? playerPosition.Value : Vector3.zero;
        pos += playerDirection != null ? playerDirection.Value : Vector3.zero;

        //Drop
        CollectableAmmo sceneAmmo = ammoMountInfo.ammo.Drop(pos, _playAudio) as CollectableAmmo;
        //Copy over the ammo instance data
        if(sceneAmmo != null)
        {
            sceneAmmo.Rounds = ammoMountInfo.rounds;
        }

        //Nullify the slot so it is empty
        ammos[_mountIndex].ammo = null;
        ammos[_mountIndex].rounds = 0;
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
