using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCharacterAnimatorController : MonoBehaviour
{
    private Animator anim = null;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }


    public void SetTurnBool(bool _value)
    {
        anim.SetBool("Turn", _value);
    }
}
