using UnityEngine;
using WebSocketSharp;


public class CollectableWeapon : CollectableItem
{
    //Inspector-Assigned
    [SerializeField, Range(0.0f, 100.0f)] protected float condition = 100.0f;
    [SerializeField, Range(0.0f, 100.0f)] protected int rounds = 15;


    //Properties
    public float Condition { get { return condition; } set { condition = value; } }
    public int Rounds { get { return rounds; } set { rounds = value; } }


    public override string GetText()
    {
        //Check the item is a weapon
        InventoryItemWeapon weapon = null;
        if(inventoryItem != null)
        {
            weapon = (InventoryItemWeapon)inventoryItem;
        }

        //Return error string if item is not a weapon
        if(weapon == null)
        {
            interactiveText = $"ERROR: No InventoryItem Assigned to {name}";
            return interactiveText;
        }

        //Assing the pickup text only once (avoid garbage piling up)
        if (interactiveText.IsNullOrEmpty())
        {
            if (weapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition)
            {
                interactiveText = $"{inventoryItem.InventoryName} (Condition: {condition}% - Rounds: {rounds}) \n{inventoryItem.PickupText}"; 
            }
            else if (weapon.WeaponFeedType == InventoryWeaponFeedType.Melee)
            {
                interactiveText = $"{inventoryItem.InventoryName} (Condition: {condition}) \n{inventoryItem.PickupText}"; 
            }
        }

        return interactiveText;
    }
}