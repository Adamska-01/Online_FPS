using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ArmsObject
{
    public ScriptableObject identifier = null;
    public List<GameObject> sceneObjects = new List<GameObject>();
    public Flashlight light = new Flashlight();
    public AnimatorStateCallback callback = null;
}
