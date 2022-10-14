using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Clip = SoundManagerConstants.Clips;
using output = SoundManagerConstants.AudioOutput;

public class PlayerSounds : MonoBehaviour
{ 
    public void PlayLeftFootstep()
    {
        SoundManager.instance.PlaySound(Clip.LEFT_FOOTSTEP, output.SFX, transform.position);
    }

    public void PlayRightFootstep()
    {
        SoundManager.instance.PlaySound(Clip.RIGHT_FOOTSTEP, output.SFX, transform.position);
    }  
}
