using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum ScreenFadeType { FadeIn, FadeOut }

public class PlayerHUD : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] private GameObject crosshair = null;
    [SerializeField] private TMP_Text healthText = null;
    [SerializeField] private TMP_Text staminaText = null;
    [SerializeField] private TMP_Text interactionText = null;
    [SerializeField] private TMP_Text missionText = null;
    [SerializeField] private Image screenFade = null;

    [SerializeField] private float missionTextDisplayTime = 3.0f;

    //Private
    private float currentFadeLevel = 1.0f;
    IEnumerator coroutine = null;


    void Start()
    {
        //Set Fade image alpha
        if(screenFade != null)
        {
            Color color = screenFade.color;
            color.a = currentFadeLevel;
            screenFade.color = color;
        }
        
        //Hide
        Invoke("HideMissionText", missionTextDisplayTime);
    }


    public void SetInteractionText(string _text)
    {
        if (interactionText != null)
        {
            if(_text == null)
            {
                interactionText.SetText(string.Empty);
                interactionText.gameObject.SetActive(false);
            }
            else
            {
                interactionText.SetText(_text);
                interactionText.gameObject.SetActive(true);
            }
        }
    }

    public void Fade(float _fadeTime, ScreenFadeType _type)
    {
        //Stop any current coroutine
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        //Set target fade
        float targetFade = 0.0f;
        switch (_type)
        {
            case ScreenFadeType.FadeIn:
                targetFade = 0.0f;
                break;
            case ScreenFadeType.FadeOut:
                targetFade = 1.0f;
                break;
        }

        //Start Fade
        coroutine = FadeInternal(_fadeTime, targetFade);
		StartCoroutine(coroutine);
    }

    private IEnumerator FadeInternal(float _fadeTime, float _targetFade)
    {
        if (screenFade == null)
            yield break;

        float timer = 0.0f;
        float srcFade = currentFadeLevel;
        Color oldColor = screenFade.color;
        if(_fadeTime < 0.1f) //Avoid division by 0
        {
            _fadeTime = 0.1f;
        }

        while(timer < _fadeTime)
        {
            timer += Time.deltaTime;
            currentFadeLevel = Mathf.Lerp(srcFade, _targetFade, timer / _fadeTime);
            oldColor.a = currentFadeLevel;
            screenFade.color = oldColor;

            yield return null;
        }

        oldColor.a = currentFadeLevel = _targetFade;
        screenFade.color = oldColor;
    }

    //Refreshes the values of UI elements
    public void Invalidate(CharacterManager _chrManager)
    {
        if (_chrManager == null)
            return;

        healthText?.SetText("Health: " + ((int)_chrManager.Health).ToString());
        staminaText?.SetText("Stamina: " + ((int)_chrManager.Stamina).ToString());
    }
    
    public void ShowMissionText(string _text)
    {
        if(missionText != null)
        {
            missionText.text = _text;
            missionText.gameObject.SetActive(true);    
        }
    }

    private void HideMissionText()
    {
        if (missionText != null)
        {
            missionText.gameObject.SetActive(false);
        }
    }
}
