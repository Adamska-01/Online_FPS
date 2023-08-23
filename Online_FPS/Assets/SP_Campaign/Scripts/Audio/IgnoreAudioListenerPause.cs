using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreAudioListenerPause : MonoBehaviour
{
    [SerializeField] protected AudioSource source = null;


    void Start()
    {
        if(source != null)
        {
            source.ignoreListenerPause = true;
        }
    }
}
