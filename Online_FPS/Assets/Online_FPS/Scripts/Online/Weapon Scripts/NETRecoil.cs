using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NETRecoil : MonoBehaviour
{
    [SerializeField] private NETPlayerController player;
    [HideInInspector] public float recoilIntensityCounter;
    public float recoilMaxIntensity;
    public float increaseRate, decreaseRate;


    void Update()
    {
        if (!player.isFiring && !player.isFiringSingleShot)
        {
            recoilIntensityCounter -= decreaseRate;
            recoilIntensityCounter = Mathf.Clamp(recoilIntensityCounter, 0, recoilMaxIntensity);
        }
    }
}
