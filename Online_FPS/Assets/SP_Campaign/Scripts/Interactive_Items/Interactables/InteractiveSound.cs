using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveSound : InteractiveItem
{
    //Inspector-Assigned
    [SerializeField, TextArea(3, 0)] private string infoText = null;
    [SerializeField, TextArea(3, 0)] private string activatedText = null;
    [SerializeField] private float activatedTextDuration = 3.0f;
    //Sound
    [SerializeField] private AudioCollection audioCollection = null;
    [SerializeField] private int bank = 0;

    //Private
    private IEnumerator coroutine = null;
    private float hideActivetedTextTime = 0.0f;


    public override string GetText()
    {
        if(coroutine != null || Time.time < hideActivetedTextTime)
        {
            return activatedText;
        }
        else
        {
            return infoText;
        }
    }

    public override void Activate(CharacterManager _chrManager)
    {
        if (coroutine == null)
        {
            hideActivetedTextTime = Time.time + activatedTextDuration;

            coroutine = DoActivation();
            StartCoroutine(coroutine);
        }
    }

    private IEnumerator DoActivation()
    {
        if (audioCollection == null || AudioManager.Instance == null)
            yield break;

        //Fetch clip from collection
        AudioClip clip = audioCollection[bank];
        if (clip == null)
            yield break;

        //Play it as one shot sound
        AudioManager.Instance.PlayOneShotSound(audioCollection.AudioGroup,
                                               clip,
                                               transform.position,
                                               audioCollection.Volume,
                                               audioCollection.SpatialBlend,
                                               audioCollection.Priority);

        //Run while clip is playing 
        yield return new WaitForSeconds(clip.length);

        //Unblock coroutine instantiation
        coroutine = null;
    }
}
