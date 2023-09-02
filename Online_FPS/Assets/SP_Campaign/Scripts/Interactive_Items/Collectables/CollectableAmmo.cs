using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using WebSocketSharp;

public class CollectableAmmo : CollectableItem
{
    //Inspector-Assigned
    [SerializeField, Range(0.0f, 100.0f)] protected int rounds = 15;


    //Properties
    public int Capacity 
    { 
        get 
        {
            InventoryItemAmmo ammoItem = inventoryItem as InventoryItemAmmo;
            if (ammoItem != null)
            {
                return ammoItem.Capacity;
            }

            return -1; 
        } 
    }
    public int Rounds { get { return rounds; }  set { rounds = Mathf.Clamp(value, 0, Capacity); } }


    public override string GetText()
    {
        //Assing the pickup text only once (avoid garbage piling up)
        if (interactiveText.IsNullOrEmpty())
        {
            if (inventoryItem == null)
            {
                interactiveText = $"ERROR: No InventoryItem Assigned to {name}";
            }
            else
            {
                interactiveText = inventoryItem.InventoryName + $" (Rounds: {rounds} / {Capacity}) \n{inventoryItem.PickupText}";
            }
        }

        return interactiveText;
    }
}
