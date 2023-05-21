using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LockdownTrigger : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] protected float downloadTime = 10.0f;
    [SerializeField] protected Slider downloadBar = null;
    [SerializeField] protected TMP_Text hintText = null;
    [SerializeField] protected MaterialController materialController = null;
    [SerializeField] protected GameObject lockedLight= null;
    [SerializeField] protected GameObject unlockedLight= null;

    //Private
    private ApplicationManager appManager = null;
    private GameSceneManager gameSceneManager = null;
    private bool inTrigger = false;
    private float downloadProgress = 0.0f;
    private AudioSource audioSource = null;
    private bool downloadComplete = false;


    private void OnEnable()
    {
        appManager = ApplicationManager.Instance;
        audioSource = GetComponent<AudioSource>();

        downloadProgress = 0.0f;

        //Register material controller
        if(materialController != null)
        {
            materialController.OnStart();
        }

        //Get lockdown state (if it exists)
        if(appManager != null)
        {
            string lockeddown = appManager.GetGameState("LOCKDOWN");

            if(string.IsNullOrEmpty(lockeddown) || lockeddown.Equals("TRUE")) //donwload uncompleted
            {
                materialController?.Activate(false);
                unlockedLight?.SetActive(false);
                lockedLight?.SetActive(true);

                downloadComplete = false;
            }
            else if (lockeddown.Equals("FALSE")) //donwload completed
            {
                materialController?.Activate(true);
                unlockedLight?.SetActive(true);
                lockedLight?.SetActive(false);

                downloadComplete = true;
            }
        }

        //Set all UI elements to starting condition
        ResetSoundAndUI();
    }

    private void ResetSoundAndUI()
    {
        //Stop the downloading sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        //Reset download bar
        if(downloadBar != null)
        {
            downloadBar.value = downloadProgress;
            downloadBar.gameObject.SetActive(false);
        }
        //Default text
        hintText.SetText("Hold 'Use' Button To Deactivate");
    }


    private void Update()
    {
        if (downloadComplete) //Puzzle already solved
            return;

        if(inTrigger)
        {
            if(Input.GetButton("Use"))
            {
                //Play the downloading sound
                if(audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                //Increment download progress
                downloadProgress = Mathf.Clamp(downloadProgress + Time.deltaTime, 0.0f, downloadTime);

                //Update UI if download is not completed
                if(downloadProgress != downloadTime)
                {
                    if(downloadBar != null)
                    {
                        downloadBar.gameObject.SetActive(true);
                        downloadBar.value = downloadProgress / downloadTime;
                    }
                    return;
                }
                else 
                {
                    //Download complete
                    downloadComplete = true;

                    //Turn off UI
                    ResetSoundAndUI();

                    //Change hint text
                    hintText?.SetText("Successful Deactivation");

                    //Shutdown lockdown 
                    appManager.SetGameState("LOCKDOWN", "FALSE");

                    //Swap texture and lights over
                    materialController?.Activate(true);
                    unlockedLight?.SetActive(true);
                    lockedLight?.SetActive(false);

                    return;
                }
            }
        }

        //Reset and UI sound (button released/not in trigger)
        downloadProgress = 0.0f;
        ResetSoundAndUI();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (inTrigger || downloadComplete)
            return;

        if (other.CompareTag("Player"))
            inTrigger = true;   
    }

    private void OnTriggerExit(Collider other)
    {
        if (downloadComplete)
            return;

        if (other.CompareTag("Player"))
            inTrigger = false;
    }
}
