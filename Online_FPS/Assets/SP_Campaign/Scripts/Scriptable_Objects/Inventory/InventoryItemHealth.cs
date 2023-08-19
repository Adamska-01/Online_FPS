using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Health", fileName = "New Health Item")]
public class InventoryItemHealth : InventoryItem
{
    [Header("Health Properties")]
    [Tooltip("The amount of health is boosted on consumption.")]
    [SerializeField, Range(0.0f, 100.0f)] protected float healingAmount = 0.0f;

    [Header("Shared Variables")]
    [Tooltip("The SharedFloat that receives the boost.")]
    [SerializeField] protected SharedFloat recipient = null;


    //Public Properties
    public float HealingAmount { get { return healingAmount; } }



    public override InventoryItem Use(Vector3 _position, bool _playAudio = true, Inventory _inventory = null)
    {
        //Add Health
        if(recipient != null)
        {
            recipient.Value = Mathf.Min(recipient.Value + healingAmount, CharacterManager.MAX_HEALTH);
        }

        //Call base class for default sound processing 
        return base.Use(_position, _playAudio, _inventory);
    }
}