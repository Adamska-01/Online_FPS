using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerTest : MonoBehaviour
{
    public AudioClip clip;

    void Start()
    {
        AudioManager.Instance?.SetTrackVolume("Zombies", 10.0f, 5.0f);

        InvokeRepeating("PlayTest", 1, 1);
    } 

    private void PlayTest()
    {
        AudioManager.Instance.PlayOneShotSound("Player", clip, transform.position, 0.5f, 0.0f, 128);
    }
}
