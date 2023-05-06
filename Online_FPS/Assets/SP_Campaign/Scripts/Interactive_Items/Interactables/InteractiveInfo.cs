using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InteractiveInfo : InteractiveItem
{
    //Inspector-Assigned
    [SerializeField, TextArea(3, 0)] private string infoText;


    public override string GetText()
    {
        return infoText;
    }
}
