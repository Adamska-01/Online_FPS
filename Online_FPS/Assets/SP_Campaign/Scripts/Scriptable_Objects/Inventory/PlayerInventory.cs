using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Player Inventory", fileName = "New Player Inventory")]
public class PlayerInventory : Inventory, ISerializationCallbackReceiver
{
    //Inspector-Assigned
    [Header("Mount Configuration and Starting Items")]
    [SerializeField] protected List<InventoryWeaponMountInfo> weaponMounts = new List<InventoryWeaponMountInfo>();
    [SerializeField] protected List<InventoryAmmoMountInfo> ammoMounts = new List<InventoryAmmoMountInfo>();
    [SerializeField] protected List<InventoryBackpackMountInfo> backpackMounts = new List<InventoryBackpackMountInfo>();

    [Header("Audio Recordings")]
    [SerializeField] protected bool autoPlayOnPickup = true;
    [SerializeField] protected List<InventoryItemAudio> audioRecordings = new List<InventoryItemAudio>();

    [Header("Shared Variables")]
    [SerializeField] protected SharedTimedStringQueue notificationQueue = null;

    [Header("Shared Variables - Broadcasters")]
    [SerializeField] protected SharedVector3 playerPosition = null;
    [SerializeField] protected SharedVector3 playerDirection = null;


    //Public Properties
    public override bool AutoPlayOnPickup { get => autoPlayOnPickupInternal; set => autoPlayOnPickupInternal = value; }


    //Internals
    protected List<InventoryWeaponMountInfo> weapons = new List<InventoryWeaponMountInfo>();
    protected List<InventoryAmmoMountInfo> ammos = new List<InventoryAmmoMountInfo>();
    protected List<InventoryBackpackMountInfo> backpacks = new List<InventoryBackpackMountInfo>();
    protected List<InventoryItemAudio> recordings = new List<InventoryItemAudio>();
    protected bool autoPlayOnPickupInternal = true;
    
    //Index of a recording currently being played
    protected int activeAudioRecordingIndex = -1;


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

    public override InventoryItemAudio GetAudioRecording(int _recordingIndex)
    {
        if (_recordingIndex < 0 || _recordingIndex >= recordings.Count)
            return null;

        return recordings[_recordingIndex];
    }

    public override int GetActiveAudioRecording()
    {
        return activeAudioRecordingIndex;
    }

    public override int GetAudioRecordingCount()
    {
        return recordings.Count;
    }

    public override int PlayAudioRecordingCount()
    {
        throw new NotImplementedException();
    }

    public override bool PlayAudioRecording(int _recordingIndex)
    {
        if (_recordingIndex < 0 || _recordingIndex >= recordings.Count)
            return false;

        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.Instance;
        if(audioPlayer != null)
        {
            audioPlayer.OnEndAudio.RemoveListener(StopAudioListener); //Remove first if an audio is being played (sanity check)
            audioPlayer.OnEndAudio.AddListener(StopAudioListener);

            //Play and set current index
            audioPlayer.PlayAudio(recordings[_recordingIndex]);
            activeAudioRecordingIndex = _recordingIndex;
        }

        return true;
    }

    void StopAudioListener()
    {
        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.Instance;
        if (audioPlayer != null)
        {
            audioPlayer.OnEndAudio.RemoveListener(StopAudioListener);
        }

        activeAudioRecordingIndex = -1;
    }

    public override void StopAudioRecording()
    {
        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.Instance;
        if (audioPlayer != null)
        {
            audioPlayer.StopAudio();
        }
    }

    public override bool AddItem(CollectableItem _collectableItem, bool _playAudio = true)
    {
        if (_collectableItem == null || _collectableItem.LinkedInventoryItem == null)
            return false;

        InventoryItem invItem = _collectableItem.LinkedInventoryItem;

        switch (invItem.Category)
        {
            case InventoryItemType.Ammunition:
                return AddAmmoItem(invItem as InventoryItemAmmo, _collectableItem as CollectableAmmo, _playAudio);
            case InventoryItemType.Consumable:
                return AddBackpackItem(invItem, _collectableItem, _playAudio);
            case InventoryItemType.Knowledge:
                break;
            case InventoryItemType.Recording:
                return AddRecordingItem(invItem as InventoryItemAudio, _collectableItem as CollectableAudio, _playAudio);
            case InventoryItemType.Weapon:
                return AddWeaponItem(invItem as InventoryItemWeapon, _collectableItem as CollectableWeapon, _playAudio);
        }

        return false;
    }

    public override void AssignWeapon(int _mountIndex, InventoryWeaponMountInfo _mountInfo)
    {
        if (_mountInfo == null || _mountInfo.weapon == null)
            return;

        if (_mountIndex < 0 || _mountIndex >= weapons.Count)
            return;

        //Assign the new mount info
        weapons[_mountIndex] = _mountInfo;

        //Play the pickup sound 
        weapons[_mountIndex].weapon.Pickup((playerPosition != null) ? playerPosition.Value : Vector3.zero);

        //Broadcast that attempt was successful
        if (notificationQueue != null)
        {
            notificationQueue.Enqueue($"Weapon Mounted: {_mountInfo.weapon.InventoryName}");
        }
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
        if (_mountIndex < 0 || _mountIndex >= weapons.Count)
            return false;

        InventoryWeaponMountInfo weaponMountInfo = weapons[_mountIndex];
        if (weaponMountInfo == null || weaponMountInfo.weapon == null)
            return false;

        InventoryItemWeapon weapon = weaponMountInfo.weapon;
        InventoryItemAmmo ammo = weapon.Ammo;

        //if no ammo assigned or the ammo is full on ammo, just return 
        if (ammo == null || weaponMountInfo.inGunRounds >= weapon.AmmoCapacity)
            return false;

        //Calculate spawn position of any eventual dropped item (in front of the player)
        Vector3 pos = playerPosition != null ? playerPosition.Value : Vector3.zero;
        pos += playerDirection != null ? playerDirection.Value : Vector3.zero;

        //If non-partial reload type simply look in the ammo belt 
        if (weapon.ReloadType == InventoryWeaponReloadType.NonPartial)
        {
            //Search for a clip with the most bullets
            int ammoMountCandidate = -1;
            int roundCount = -1;
            for (int i = 0; i < ammos.Count; i++)
            {
                InventoryAmmoMountInfo ammoMountInfo = ammos[i];

                //If ammo at this mount is not the correct type just skip
                if (ammoMountInfo.ammo != ammo)
                    continue;

                //Check if this is the best candidate
                if (ammoMountInfo.rounds > roundCount)
                {
                    roundCount = ammoMountInfo.rounds;
                    ammoMountCandidate = i;
                }
            }

            //No ammo found or no clip with more bullets found 
            if (ammoMountCandidate == -1 || roundCount <= weaponMountInfo.inGunRounds)
                return false;

            //Else proceed with the switch of the ammunition
            int oldInGunRounds = weaponMountInfo.inGunRounds;
            weaponMountInfo.inGunRounds = ammos[ammoMountCandidate].rounds;

            //In terms of the inventory.. using a weaponis reloading it. Firig a weapon
            //is handled somewhere else entirely 
            weapon.Use(Vector3.zero, _playAudio);

            //If no rounds were in the gun before the reload, we have nothing to swap
            //with the mount so remove the item from the mount 
            if (oldInGunRounds == 0)
            {
                //Clear the mount 
                ammos[ammoMountCandidate].ammo = null;

                //Create an instance of the ammo type to simulate the ejected clip
                CollectableAmmo sceneAmmo = ammo.Drop(pos, _playAudio) as CollectableAmmo;
                if (sceneAmmo != null)
                {
                    sceneAmmo.Rounds = 0;
                }
            }
            else //Do not throw it away, just swap it
            {
                ammos[ammoMountCandidate].rounds = oldInGunRounds;
            }
        }
        else if (weapon.ReloadType == InventoryWeaponReloadType.Partial)
        {
            //if gun is full, aboart reload
            int roundsWanted = weapon.AmmoCapacity - weaponMountInfo.inGunRounds;

            if (roundsWanted <= 0) //Weapon full
                return false;

            //Search for the correct ammo type
            for (int i = 0; i < ammos.Count; i++)
            {
                InventoryAmmoMountInfo ammoMountInfo = ammos[i];

                //If ammo at this mount is not the correct type just skip
                if (ammoMountInfo.ammo != ammo)
                    continue;

                //Otherwise we found some ammo so lets remove some items from it
                int ammoTaken = Mathf.Min(roundsWanted, ammoMountInfo.rounds);
                weaponMountInfo.inGunRounds += ammoTaken;
                ammoMountInfo.rounds -= ammoTaken;
                roundsWanted -= ammoTaken;

                //Use the weapon to reload it
                weapon.Use(pos, _playAudio);

                //if we have emptied this ammo item then remove it from the belt
                if (ammoMountInfo.rounds <= 0)
                {
                    ammoMountInfo.ammo = null;
                }

                if (roundsWanted <= 0) //fully reloaded, else continue to check further ammo
                    break;
            }
        }

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

        //Notify listeners that a weapon has been dropped 
        OnWeaponDropped?.Invoke(weapon);
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
        if (sceneAmmo != null)
        {
            sceneAmmo.Rounds = ammoMountInfo.rounds;
        }

        //Nullify the slot so it is empty
        ammos[_mountIndex].ammo = null;
        ammos[_mountIndex].rounds = 0;
    }

    public override int GetAvailableAmmo(InventoryItemAmmo _ammo, bool _includeWeapon = false)
    {
        if (_ammo != null)
            return 0;

        //Do the sum
        int roundCount = 0;
        for (int i = 0; i < ammos.Count; i++)
        {
            InventoryAmmoMountInfo ammoMountInfo = ammos[i];
            if (ammoMountInfo.ammo != _ammo)
                continue;

            roundCount += ammoMountInfo.rounds;
        }

        //Also include ammo in the weapon mount if requested
        if (_includeWeapon)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                InventoryWeaponMountInfo weaponMountInfo = weapons[i];
                if (weaponMountInfo == null || weaponMountInfo.weapon.Ammo != _ammo)
                    continue;

                roundCount += weaponMountInfo.inGunRounds;
            }
        }

        return roundCount;
    }

    public override bool IsReloadAvailable(int _weaponMountIndex)
    {
        if (_weaponMountIndex < 0 || _weaponMountIndex >= weapons.Count)
            return false;

        //Get the weapon mount and the weapon from that mount 
        InventoryWeaponMountInfo weaponMountInfo = weapons[_weaponMountIndex];
        InventoryItemWeapon weapon = weaponMountInfo.weapon;

        //If no weapon, no ammo or no ammo needed return false
        if (!weapon ||
            weapon.ReloadType == InventoryWeaponReloadType.None ||
            weapon.WeaponType == InventoryWeaponType.None ||
            weaponMountInfo.inGunRounds >= weapon.AmmoCapacity ||
            weapon.Ammo == null) return false;


        //If its a Non-Partial Reload type then simply search the belt for a clip
        //with the most bullets
        if (weapon.ReloadType == InventoryWeaponReloadType.NonPartial)
        {
            // Search for a clip in our belt that has the highest round count
            // that matches the ammo of the gun
            int roundCount = -1;
            for (int i = 0; i < ammos.Count; i++)
            {
                InventoryAmmoMountInfo ammoMountInfo = ammos[i];
                if (ammoMountInfo.ammo != weapon.Ammo)
                    continue;

                if (ammoMountInfo.rounds > roundCount)
                {
                    roundCount = ammoMountInfo.rounds;
                }
            }

            // If the highest available rounds found is less than or equal to the
            // amount we are looking for as a minimum to make a reload worth while return false
            if (roundCount <= weaponMountInfo.inGunRounds) return false;

            // A clip has been found so return true to allow a reload to proceed
            return true;
        }
        else if (weapon.ReloadType == InventoryWeaponReloadType.Partial)
        {
            // Loop through items on ammo belt searching for the correct ammo type
            for (int i = 0; i < ammos.Count; i++)
            {
                // If not the right type of ammo then continue
                InventoryAmmoMountInfo ammoMountInfo = ammos[i];
                if (ammoMountInfo.ammo != weapon.Ammo)
                    continue;

                // Partial reload so only looks for at last 1
                if (ammoMountInfo.rounds > 0)
                    return true;
            }

            return false;
        }

        // Fall through case in case we add more reload types
        return false;
    }

    public override InventoryMountInfo Search(InventoryItem _matchItem)
    {
        if (_matchItem == null)
            return null;

        //Search backpack first
        for (int i = 0; i < backpacks.Count; i++)
        {
            if (backpacks[i].item == _matchItem)
                return backpacks[i];
        }
        //...then Ammo Belt
        for (int i = 0; i < ammos.Count; i++)
        {
            if (ammos[i].ammo == _matchItem)
                return ammos[i];
        }
        //...then weapon's mounts
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].weapon == _matchItem)
                return weapons[i];
        }

        return null; //Not found
    }

    public override int Remove(InventoryItem _matchItem)
    {
        if (_matchItem == null)
            return 0;

        int removeCount = 0;

        //Search backpack first
        for (int i = 0; i < backpacks.Count; i++)
        {
            if (backpacks[i].item == _matchItem)
            {
                backpacks[i].item = null;
                removeCount++;
            }
        }
        //...then Ammo Belt
        for (int i = 0; i < ammos.Count; i++)
        {
            if (ammos[i].ammo == _matchItem)
            {
                ammos[i].ammo = null;
                ammos[i].rounds = 0;
                removeCount++;
            }
        }
        //...then weapon's mounts
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].weapon == _matchItem)
            {
                weapons[i].weapon = null;
                weapons[i].condition = 0;
                weapons[i].inGunRounds = 0;
                removeCount++;
            }
        }

        return removeCount; //Return item count removed
    }

    public override bool RemoveWeapon(int _mountIndex)
    {
        //Is there something to remove...if not return false
        if (_mountIndex < 0 || _mountIndex >= weapons.Count || weapons[_mountIndex].weapon == null)
            return false;

        weapons[_mountIndex].weapon = null;
        weapons[_mountIndex].condition = 0;
        weapons[_mountIndex].inGunRounds = 0;

        return true;
    }

    public override bool RemoveBackpack(int _mountIndex)
    {
        //Is there something to remove...if not return false
        if (_mountIndex < 0 || _mountIndex >= backpacks.Count || backpacks[_mountIndex].item == null)
            return false;

        backpacks[_mountIndex].item = null;

        return true;
    }

    public override bool RemoveAmmo(int _mountIndex)
    {
        //Is there something to remove...if not return false
        if (_mountIndex < 0 || _mountIndex >= ammos.Count || ammos[_mountIndex].ammo == null)
            return false;

        ammos[_mountIndex].ammo = null;
        ammos[_mountIndex].rounds = 0;

        return true;
    }

    public override List<InventoryWeaponMountInfo> GetAllWeapons() => weapons; 
    public override List<InventoryAmmoMountInfo> GetAllAmmo() => ammos;
    public override List<InventoryBackpackMountInfo> GetAllBackpack() => backpacks;

    protected bool AddAmmoItem(InventoryItemAmmo _inventoryItem, CollectableAmmo _collectableItem, bool _playAudio)
    {
        //Check for empty slots
        for (int i = 0; i < ammos.Count; i++)
        {
            if (ammos[i].ammo == null) //Empty
            {
                //Store item in the slot
                ammos[i].ammo = _inventoryItem;

                //Copy over the instance data 
                ammos[i].rounds = _collectableItem.Rounds;

                //Play pick up sound
                _inventoryItem.Pickup(_collectableItem.transform.position, _playAudio); 

                //Broadcast that attempt was successful
                if (notificationQueue != null)
                {
                    notificationQueue.Enqueue($"Added {_inventoryItem.InventoryName} to Ammo Belt");
                }

                return true; //Success
            }
        }

        //Broadcast that attempt was NOT successful
        if (notificationQueue != null)
        {
            notificationQueue.Enqueue($"Could not pickup {_inventoryItem.InventoryName}\nNo Room in Ammo Belt");
        }

        return false; //Fail
    }

    protected bool AddBackpackItem(InventoryItem _inventoryItem, CollectableItem _collectableItem, bool _playAudio)
    {
        //Check for empty slots
        for (int i = 0; i < backpacks.Count; i++)
        {
            if (backpacks[i].item == null) //Empty
            {
                //Store item in the slot
                backpacks[i].item = _inventoryItem;

                _inventoryItem.Pickup(_collectableItem.transform.position, _playAudio); //Play pick up sound

                //Broadcast that attempt was successful
                if(notificationQueue != null)
                {
                    notificationQueue.Enqueue($"Added {_inventoryItem.InventoryName} to Backpack");
                }

                return true; //Success
            }
        }

        //Broadcast that attempt was NOT successful
        if (notificationQueue != null)
        {
            notificationQueue.Enqueue($"Could not pickup {_inventoryItem.InventoryName}\nNo Room in Backpack");
        }

        return false; //Fail
    }

    protected bool AddWeaponItem(InventoryItemWeapon _inventoryItem, CollectableWeapon _collectableItem, bool _playAudio)
    {
        //Get the correct weapon mount (single/dual handed)
        int mountIndex = (_inventoryItem.WeaponType == InventoryWeaponType.SingleHanded) ? 0 : 1;

        //Create a temporary mount to describe the weapon
        InventoryWeaponMountInfo weaponMountTemp = new InventoryWeaponMountInfo();
        weaponMountTemp.weapon = _inventoryItem;
        weaponMountTemp.condition = _collectableItem.Condition;
        weaponMountTemp.inGunRounds = _collectableItem.Rounds;

        //Notify listeners that a weapon has been changed
        OnWeaponChange?.Invoke(weaponMountTemp);

        return true; //Fail
    }

    private bool AddRecordingItem(InventoryItemAudio _invItem, CollectableAudio _collectableItem, bool _playAudio)
    {
        if(_invItem != null)
        {
            //Play the pick up sound 
            _invItem.Pickup(_collectableItem.transform.position, _playAudio);

            //Add audio recording to the list 
            recordings.Add(_invItem);

            //Play on pick if configured to do so (last picked one)
            if(autoPlayOnPickupInternal)
            {
                PlayAudioRecording(recordings.Count - 1);
            }

            if(notificationQueue != null)
            {
                notificationQueue.Enqueue("Audio Recording Added");
            }

            //Data successfully retrieved
            return true;
        }
        
        return false;
    }

    //-------------------------------------------------------------------
    //-------------------------- Serialization --------------------------
    //-------------------------------------------------------------------
    public void OnAfterDeserialize()
    {
        weapons.Clear();
        ammos.Clear();
        backpacks.Clear();
        recordings.Clear();

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
        foreach (InventoryItemAudio recording in audioRecordings)
        {
            recordings.Add(recording);
        }

        autoPlayOnPickupInternal = autoPlayOnPickup;
        
        //Reset audio recording selection
        activeAudioRecordingIndex = -1;
    }
    public void OnBeforeSerialize() { } //Not needed
}
