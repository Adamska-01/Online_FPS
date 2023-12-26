using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Weapon", fileName = "New Weapon Item")]
public class InventoryItemWeapon : InventoryItem
{
    [Header("Weapon Properties")]
    [Tooltip("Is this a singled handed or a dual handed weapon?")]
    [SerializeField] protected InventoryWeaponType weaponType = InventoryWeaponType.SingleHanded;

    [Tooltip("Is this a melee weapon or requires ammunitions?")]
    [SerializeField] protected InventoryWeaponFeedType weaponFeedType = InventoryWeaponFeedType.None;

    [Tooltip("What inventory object does this weapon use for ammunition?")]
    [SerializeField] protected InventoryItemAmmo ammo = null;

    [Tooltip("What is the ammo capacity of the gun? \n\nOnly required for guns that support non-partial reload type")]
    [SerializeField] protected int ammoCapacity = 0;

    [Tooltip("What is the reload type (Partial - Non-Partial)\n\n" +
             "Partial - Individual rounds/cartridges can be loaded into the gun one at a time. Ammo capacity is determined by ammoCapacity (above) of the weapon.\n\n" +
             "NonPartial - Reloads are done by switching magazines. Weapon ammo cpacity is determined by capacity of ammo item.")]
    [SerializeField] protected InventoryWeaponReloadType reloadType = InventoryWeaponReloadType.None;

    [Tooltip("Max Range in meters of this weapon.")]
    [SerializeField] protected float range = 0.0f;
        
    [Tooltip("Weapon Sound Radius.")]
    [Range(0, 20)]
    [SerializeField] protected int soundRadius = 1;

    [Tooltip("Should this weapon auto-fire when FIRE button is being held in a pressed state.")]
    [SerializeField] protected bool autoFire = false;

    [Tooltip("Does this weapon have a dual firing more (like a sniper mode)")]
    [SerializeField] protected bool dualMode = false;

    [Tooltip("How much the condition of the weapon depletes with each use.")]
    [Range(0.0f, 100.0f)]
    [SerializeField] protected float conditionDepletion = 1.0f;

    [Header("Damage Properties")]
    [Tooltip("Thinkness of Raycast used for raycasting potential damage. \n\n" +
             "If zero, a standard raycast is used.\nIf non-zero, a SphereCast is used with the desired radius.\n\n" +
             "Thickness can be used to emulate a blast radius like in the case of a Shotgun or Gravity Gun.")]
    [SerializeField] protected float rayRadius = 0.0f;

    [Tooltip("Maximum damage done to the Head of an enemy with a single hit.")]
    [SerializeField] protected int headDamage = 100;

    [Tooltip("Maximum damage done to the body of an enemy with a single hit.")]
    [SerializeField] protected int bodyDamage = 20;

    [Tooltip("How damage is diluted over the range of the weapon.")]
    [SerializeField] protected AnimationCurve damageAttenuation = new AnimationCurve();

    [Tooltip("Force applied by this weapon on a target.")]
    [SerializeField] protected float force = 100.0f;

    [Tooltip("How force is diluted over the range of the weapon.")]
    [SerializeField] protected AnimationCurve forceAttenuation = new AnimationCurve();


    [Header("FPS Arms Animation Properties")]
    [Tooltip("FPS Arms Animator Sub-State Machine index to use when performing animations for this weapon.")]
    [SerializeField] protected int weaponAnim = -1;

    [Tooltip("FPS Arms Animator Sub-State Machine attack animation index range.\n\nA Value of 3 would be used if the sub-state has 3 attack variants.")]
    [SerializeField] protected int attackAnimCount = 1;

    [Header("Camera Settings")]
    [Tooltip("Dual Mode FOV")]
    [SerializeField] protected float dualModeFOV = 45.0f;

    [Tooltip("Image used for weapon crosshair")]
    [SerializeField] protected Sprite crosshair = null;


     // Public Properties
    public InventoryWeaponType WeaponType           { get { return weaponType; } }
    public InventoryWeaponFeedType WeaponFeedType   { get { return weaponFeedType; } }
    public InventoryItemAmmo Ammo                   { get { return ammo; } }
    public InventoryWeaponReloadType ReloadType     { get { return reloadType; } }
    public float Range                              { get { return range; } }
    public float SoundRadius                        { get { return soundRadius; } }
    public float ConditionDepletion                 { get { return conditionDepletion; } }
    public bool AutoFire                            { get { return autoFire; } }
    public bool DualMode                            { get { return dualMode; } }
    public int HeadDamage                           { get { return headDamage; } }
    public int BodyDamage                           { get { return bodyDamage; } }
    public float RayRadius                          { get { return rayRadius; } }
    public float Force                              { get { return force; } }
    public int WeaponAnim                           { get { return weaponAnim; } }
    public int AttackAnimCount                      { get { return attackAnimCount; } }
    public float DualModeFOV                        { get { return dualModeFOV; } }
    public Sprite Crosshair                         { get { return crosshair; } }
    public int AmmoCapacity // Returns the correct max capcity of the weapon based on ReloadType and Ammo.
    {
        get
        {
            switch (reloadType)
            {   
                case InventoryWeaponReloadType.None: 
                    return 0;
                case InventoryWeaponReloadType.Partial: 
                    return ammoCapacity;
                case InventoryWeaponReloadType.NonPartial:
                    if (ammo == null)
                    {
                        return -1;
                    }
                    return ammo.Capacity;
            }

            return -1;
        }
    }



    // --------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedDamage
    // Desc :   Given a distance in meters and a body part string ("Head" or "Body") will return
    //          the damage that weapon does to that body part 
    // --------------------------------------------------------------------------------------------
    public int GetAttentuatedDamage(string bodyPart, float distance)
    {
        float normalizedDistance = Mathf.Clamp(distance / range, 0.0f, 1.0f);
        if (bodyPart.Equals("Head"))
        {
            return (int)(damageAttenuation.Evaluate(normalizedDistance) * headDamage);
        } 
        
        return (int)(damageAttenuation.Evaluate(normalizedDistance) * bodyDamage);
    }

    // ---------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedForce
    // Desc :   Given a distance to a target return the amount of force that will be recieved 
    //          from this weapon at this distance
    // ---------------------------------------------------------------------------------------------
    public float GetAttentuatedForce(float distance)
    {
        if (force == 0.0f)
        {
            return 0.0f;
        }

        return forceAttenuation.Evaluate(Mathf.Clamp(distance / range, 0.0f, 1.0f)) * force;
    }
}