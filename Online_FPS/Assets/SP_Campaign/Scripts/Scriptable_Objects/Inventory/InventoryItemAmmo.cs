using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Ammunition", fileName = "New Ammunition")]
public class InventoryItemAmmo : InventoryItem
{
    [Header("Ammo Properties")]
    [Tooltip("The maximum number of rounds/cartridges an item of this type can hold.")]
    [SerializeField] protected int capacity = 0;


    //Public Properties
    public int Capacity { get { return capacity; } }
}
