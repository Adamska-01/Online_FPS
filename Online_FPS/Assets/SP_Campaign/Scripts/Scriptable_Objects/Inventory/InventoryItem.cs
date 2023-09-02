using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SoundManagerConstants;


//Public Enums that categorize item behaviours
public enum InventoryItemType         { None, Ammunition, Consumable, Knowledge, Recording, Weapon }
public enum InventoryWeaponType       { None, SingleHanded, TwoHanded }
public enum InventoryWeaponFeedType   { None, Melee, Ammunition }
public enum InventoryWeaponReloadType { None, Partial, NonPartial }
public enum InventoryAction           { None, Consume, Reload }


// --------------------------------------------------------------------------------
// CLASS	:	InventoryItem   
// DESC		:	Base class for all inventoryItem scriptable objects.
// --------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Base", fileName = "New Inventory Item")]
public class InventoryItem : ScriptableObject
{
    [Header("General Properties")]
    [Tooltip("If enabled, only one of these will ever be allowed to exist in the inventory at one time.")]
    [SerializeField] protected bool singleton = false;

    [Tooltip("Interactive text that is displayed next to the item name.")]
    [SerializeField] protected string pickupText = "Press 'Use' to Pickup";
    
    [Tooltip("Name used for displaying this item in the inventory.")]
    [SerializeField] protected string inventoryName = null;

    [Tooltip("Sprite used to display this item in the inventory.")]
    [SerializeField] protected Sprite inventoryImage = null;

    [Tooltip("What type of configurable Action does this item support.")]
    [SerializeField] protected InventoryAction inventoryAction = InventoryAction.None;

    [Tooltip("UI Text that describe the action (Used on action buttons in the UI).")]
    [SerializeField] protected string inventoryActionText = null;

    [Tooltip("When this item is 'Used', it should be replaced in the inventory with this item. " +
             "\n\n e    g. This can be used to replace a full can of food with an empty can of food once the food has been consumed.")]
    [SerializeField] protected InventoryItem replacementItem = null;

    [Tooltip("The collectable item that is instantiated in the scene when this item is dropped from the inventory.")]
    [SerializeField] protected CollectableItem collectableItem = null;

    [Tooltip("Detailed description of the object that can be displayed by inventory UIs.")]
    [SerializeField, TextArea(5, 10)] protected string invDescription = null;

    [Tooltip("The type of inventory item this is.")]
    [SerializeField] protected InventoryItemType category = InventoryItemType.None;

    [Tooltip("Audio collection to use for this inventory item.\n\n" +
             "Bank[0] : Pickup Sounds\nBank[1] : Drop Sounds.\n Bank[2] : Use Sounds")]
    [SerializeField] protected AudioCollection audio = null;


    //Property Getters
    public string  InventoryName           { get { return inventoryName; } }
    public Sprite  InventoryImage          { get { return inventoryImage; } }
    public string  InventoryDescription    { get { return invDescription; } }
    public virtual string  PickupText      { get { return pickupText; } }
    public InventoryItemType Category      { get { return category; } }
    public AudioCollection AudioCol        { get { return audio; } }
    public InventoryAction InventoryAction { get { return inventoryAction; } }
    public string  InventoryActionText     { get { return string.IsNullOrEmpty(inventoryActionText) ? inventoryAction.ToString() : inventoryActionText; } }


    public virtual void Pickup(Vector3 _position, bool _playAudio = true)
    {
        if(audio != null && AudioManager.Instance != null && _playAudio)
        {
            AudioClip pickupAudio = audio[0]; //Pick audio in the first bank (reserved for pickups)
            if(pickupAudio != null)
            {
                //Play audio
                AudioManager.Instance.PlayOneShotSound(audio.AudioGroup,
                                                       pickupAudio,
                                                       _position,
                                                       audio.Volume,
                                                       audio.SpatialBlend,
                                                       audio.Priority);
            }
        }
    }

    public virtual CollectableItem Drop(Vector3 _position, bool _playAudio = true)
    {
        if (audio != null && AudioManager.Instance != null && _playAudio)
        {
            AudioClip dropAudio = audio[1]; //Drop audio in the second bank (reserved for drops)
            if (dropAudio != null)
            {
                //Play audio
                AudioManager.Instance.PlayOneShotSound(audio.AudioGroup,
                                                       dropAudio,
                                                       _position,
                                                       audio.Volume,
                                                       audio.SpatialBlend,
                                                       audio.Priority,
                                                       0.0f,
                                                       true);
            }
        }

        //Instantitate the collectable item and return it
        if(collectableItem != null)
        {
            CollectableItem go = Instantiate<CollectableItem>(collectableItem, _position, Quaternion.identity);
            return go;
        }

        return null;
    }

    public virtual InventoryItem Use(Vector3 _position, bool _playAudio = true, Inventory _inventory = null) //You may want to use the inventory obj for specific items (eg. repair kit for weapons -> find compatible weapon in the invenotory)
    {
        if (audio != null && AudioManager.Instance != null && _playAudio)
        {
            AudioClip useAudio = audio[2]; //Use audio in the third bank (reserved for Use)
            if (useAudio != null)
            {
                //Play audio
                AudioManager.Instance.PlayOneShotSound(audio.AudioGroup,
                                                       useAudio,
                                                       _position,
                                                       audio.Volume,
                                                       audio.SpatialBlend,
                                                       audio.Priority,
                                                       0.0f,
                                                       true);
            }
        }

        //Return the item that should replace this in the inventory after use 
        return replacementItem; 
    }
}
