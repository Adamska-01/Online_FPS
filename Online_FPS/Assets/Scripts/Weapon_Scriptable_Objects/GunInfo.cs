using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS/New Gun")]
public class GunInfo : ItemInfo
{
    public float damageHead;
    public float damageBody;
    public float damageLeg;

    public float fireRate;
     
    public Transform leftHandTarget;
}
