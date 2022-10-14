using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramerateCap : MonoBehaviour
{ 
    void Start()
    {
        Application.targetFrameRate = 360;
    } 
}
