using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


// --------------------------------------------------------------------------
// CLASS	:	InventoryAudioPlayer
// DESC		:	The Component used by the inventory system to playback audio
//              recordings
// --------------------------------------------------------------------------
public class InventoryAudioPlayer : MonoBehaviour
{
    //Singleton Members
    protected static InventoryAudioPlayer instance = null;
    public static InventoryAudioPlayer Instance { get { return instance; } }


    //Inspector-Assigned
    [Header("Shared Variables")]
    [SerializeField] protected SharedVector3 playerPos = null;
    [SerializeField] protected SharedTimedStringQueue notificationQueue = null;
    [SerializeField] protected SharedString transcriptText = null;

    [Header("Audio Configurations")]
    [SerializeField] protected AudioCollection stateNotificationSounds = null;

    [Header("Event Listeners")]
    public UnityEvent<InventoryItemAudio> OnBeginAudio = new UnityEvent<InventoryItemAudio>();
    public UnityEvent<float> OnUpdateAudio = new UnityEvent<float>();
    public UnityEvent OnEndAudio = new UnityEvent();


    //Internals 
    protected AudioSource audioSource = null;
    private IEnumerator coroutine = null;


    private void Awake()
    {
        //Store singleton ref
        instance = this;

        //Store Audio Source Component
        audioSource = GetComponent<AudioSource>();

        //Configure audio source to play even when time is paused
        if(audioSource != null )
        {
            audioSource.ignoreListenerPause = true;
        }
    }


    public void PlayAudio(InventoryItemAudio _audioItem)
    {
        //Stop current audio
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        //Stop playing any sound that the audio source is already playing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        //Failure - stop immediately 
        if (_audioItem == null || audioSource == null || _audioItem.AudioCol == null)
        {
            StopAudio();
            return;
        }

        AudioClip clip = _audioItem.AudioCol[2];
        if(clip == null) 
        {
            StopAudio();
            return;
        }

        //Configure Audio Source
        audioSource.clip = clip;
        audioSource.volume = _audioItem.AudioCol.Volume; 
        audioSource.spatialBlend = _audioItem.AudioCol.SpatialBlend; 
        audioSource.priority = _audioItem.AudioCol.Priority; 
        audioSource.Play();

        //Fire Begin Event
        OnBeginAudio.Invoke(_audioItem);

        //Start/Update coroutine
        coroutine = UpdateAudio(_audioItem);
        StartCoroutine(coroutine);
    }

    public void StopAudio()
    {
        //Reset audio source 
        if (audioSource != null)
        {
            audioSource.clip = null;
        }

        //Stop coroutine if still running 
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        //Clear transcript text
        if (transcriptText != null)
        {
            transcriptText.Value = null;
        }

        //Raise end event
        OnEndAudio.Invoke();
    }

    private IEnumerator UpdateAudio(InventoryItemAudio _audioItem)
    {
        if(_audioItem != null && audioSource != null)
        {
            //Keep track of audio timeline
            int previousStateKeyIndex = 0;
            int previousCaptionKeyIndex = 0;

            List<TimedStateKey> stateKeys = _audioItem.StateKeys;
            List<TimedCaptionKey> captionKeys = _audioItem.CaptionKeys;

            while(audioSource.isPlaying)
            {
                //Invoke Update Event with normalized time (0-1 range)
                OnUpdateAudio.Invoke(audioSource.time / audioSource.clip.length);

                //Process any state keys 
                if (stateKeys != null && ApplicationManager.Instance)
                {
                    //Loop from the previous key we found that we have not yet executed
                    for (int i = previousStateKeyIndex; i < stateKeys.Count; i++)
                    {
                        TimedStateKey keyFrame = stateKeys[i];
                        if (keyFrame != null)
                        {
                            //If we haven't reached this key yet then store this
                            //as our previous key and abort so we can test from this
                            //key next time
                            if (keyFrame.time > audioSource.time)
                            {
                                previousStateKeyIndex = i;
                                break;
                            }

                            //Set the state described by the keyframe
                            if (ApplicationManager.Instance.SetGameState(keyFrame.key, keyFrame.value))
                            {
                                //Add Key Message to Shared Notification Queue
                                if (notificationQueue != null)
                                {
                                    notificationQueue.Enqueue(keyFrame.UIMessage);
                                }

                                //Play notification Sound
                                if (AudioManager.Instance && stateNotificationSounds)
                                {
                                    AudioClip clip = stateNotificationSounds.RandomClip;
                                    if (clip != null)
                                    {
                                        AudioManager.Instance.PlayOneShotSound(stateNotificationSounds.AudioGroup,
                                                                               clip,
                                                                               playerPos ? playerPos.Value : Vector3.zero,
                                                                               stateNotificationSounds.Volume,
                                                                               stateNotificationSounds.SpatialBlend,
                                                                               stateNotificationSounds.Priority,
                                                                               0.0f,
                                                                               true);
                                    }
                                }
                            }

                            previousStateKeyIndex++;
                        }
                    }
                }

                //Process any state keys 
                if (captionKeys != null)
                {
                    //Loop from the previous key we found that we have not yet executed
                    for (int i = previousCaptionKeyIndex; i < captionKeys.Count; i++)
                    {
                        TimedCaptionKey keyFrame = captionKeys[i];
                        if (keyFrame != null)
                        {
                            //If we haven't reached this key yet then store this
                            //as our previous key and abort so we can test from this
                            //key next time
                            if (keyFrame.time > audioSource.time)
                            {
                                previousCaptionKeyIndex = i;
                                break;
                            }

                            // Set the global shared transcript variable to the caption text
                            if (transcriptText)
                            {
                                transcriptText.Value = keyFrame.text;
                            }

                            previousCaptionKeyIndex++;
                        }
                    }
                }

                yield return null;
            }
        }

        StopAudio(); //Stop and Rais ending event
    }
}
