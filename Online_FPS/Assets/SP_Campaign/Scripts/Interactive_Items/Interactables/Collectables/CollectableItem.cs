using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WebSocketSharp;

public class CollectableItem : InteractiveItem
{
    //Inspector-Assigned
    [Header("Inventory")]
    [SerializeField] protected Inventory inventory = null;

    [Header("Collectable Item Properties")]
    [SerializeField] protected InventoryItem inventoryItem = null;


    //Properties 
    //Why? Allow to change inventory at runtime (eg. let NPCs pick up items)
    public Inventory LinkedInventory { get { return inventory; } set { inventory = value; } }
    public InventoryItem LinkedInventoryItem { get { return inventoryItem; } }


    //Internals 
    protected string interactiveText = null;


    public override string GetText()
    {
        //Assing the pickup text only once (avoid garbage piling up)
        if(interactiveText.IsNullOrEmpty())
        {
            if(inventoryItem != null)
            {
                interactiveText = inventoryItem.InventoryName + "\n" + inventoryItem.PickupText;
            }
            else
            {
                interactiveText = $"ERROR: No InventoryItem Assigned to {name}";
            }
        }

        return interactiveText;
    }

    public override void Activate(CharacterManager _chrManager)
    {
        //Add item to the inventory and destroy this collectable visualization
        if(inventory != null)
        {
            if(inventory.AddItem(this))
            {
                Destroy(this);
            }
        }
    }
}
