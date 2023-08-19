using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Anti-Infection", fileName = "New Anti-Infection Item")]
public class InventoryItemAntiInfection : InventoryItem
{
    [Header("Anti-Infection Properties")]
    [Tooltip("The amount infection is reduced on consumption.")]
    [SerializeField, Range(0.0f, 100.0f)] protected float reductionAmount = 0.0f;

    [Header("Shared Variables")]
    [Tooltip("The SharedFloat that receives the boost.")]
    [SerializeField] protected SharedFloat recipient = null;


    //Public Properties
    public float ReductionAmount { get { return reductionAmount; } }



    public override InventoryItem Use(Vector3 _position, bool _playAudio = true, Inventory _inventory = null)
    {
        //Add Health
        if (recipient != null)
        {
            recipient.Value = Mathf.Max(recipient.Value - reductionAmount, 0.0f);
        }

        //Call base class for default sound processing 
        return base.Use(_position, _playAudio, _inventory);
    }
}