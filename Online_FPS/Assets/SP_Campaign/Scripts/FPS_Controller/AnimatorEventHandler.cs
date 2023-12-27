using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Just a 'bridge' class that calls the 'DoDamage' function in the character manager
/// </summary>
public class AnimatorEventHandler : MonoBehaviour
{
    // Internal 
    protected CharacterManager characterManager = null;


    void Start()
    {
        characterManager = GetComponentInParent<CharacterManager>();
    }


    public void FireWeaponEvent(int direction)
    {
        characterManager?.DoDamage(direction);
    }
}
