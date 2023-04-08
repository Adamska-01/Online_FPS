using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerTest : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance?.SetTrackVolume("Zombies", 10.0f, 5.0f);
        Invoke("invokeFunc", 5.0f);
    } 

    public void invokeFunc()
    {
        AudioManager.Instance?.SetTrackVolume("Zombies", 0.0f, 5.0f);
    }
}
