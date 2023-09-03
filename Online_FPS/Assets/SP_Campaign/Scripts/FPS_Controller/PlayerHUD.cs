using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum ScreenFadeType { FadeIn, FadeOut }

public class PlayerHUD : MonoBehaviour
{
    //Inspector-Assigned
    [Header("UI Sliders")]
    [SerializeField] private Slider healthSlider      = null;
    [SerializeField] private Slider staminaSlider     = null;
    [SerializeField] private Slider infectionSlider   = null;
    [SerializeField] private Slider flashlightSlider  = null;
    [SerializeField] private Slider nightVisionSlider = null;
    
    [Header("UI Texts")]
    [SerializeField] private TMP_Text interactionText  = null;
    [SerializeField] private TMP_Text notificationText = null;
    [SerializeField] private TMP_Text transcriptText   = null;
    [SerializeField] private Image screenFade          = null;

    [Header("Shared Variables")]
    [SerializeField] private SharedFloat health      = null;
    [SerializeField] private SharedFloat stamina     = null;
    [SerializeField] private SharedFloat infection   = null;
    [SerializeField] private SharedFloat flashlight  = null;
    [SerializeField] private SharedFloat nightVision = null;
    [SerializeField] private SharedString interactionString = null;
    [SerializeField] private SharedString transcriptString  = null;
    [SerializeField] private SharedTimedStringQueue notificationQueue = null;

    //Internal
    private float currentFadeLevel = 1.0f;
    private IEnumerator coroutine = null;
    private Action OnHealthUpdate;
    private Action OnStaminaUpdate;
    private Action OnInfectionUpdate;
    private Action OnFlashlightUpdate;
    private Action OnNightvisionUpdate;
    private Action OnInteractionTextUpdate;
    private Action OnTranscriptTextUpdate;
    private Action OnNotificationQueueUpdate;


    private void Awake()
    {
        //Cache lamda event listeners
        OnHealthUpdate = () => { healthSlider.value = health.Value; };
        OnStaminaUpdate = () => { staminaSlider.value = stamina.Value; };
        OnInfectionUpdate = () => { infectionSlider.value = infection.Value; };
        OnFlashlightUpdate = () => { flashlightSlider.value = flashlight.Value; };
        OnNightvisionUpdate = () => { nightVisionSlider.value = nightVision.Value; };
        OnInteractionTextUpdate = () => { interactionText.text = interactionString.Value; };
        OnTranscriptTextUpdate = () => { transcriptText.text = transcriptString.Value; };
        OnNotificationQueueUpdate = () => { notificationText.text = notificationQueue.CurrentDequeuedText; };
    }

    void Start()
    {
        //Set Fade image alpha
        if(screenFade != null)
        {
            Color color = screenFade.color;
            color.a = currentFadeLevel;
            screenFade.color = color;
        }

        //Make a first assignment (all future assignments are handled by the events)
        if (healthSlider != null && health != null)                 OnHealthUpdate?.Invoke(); 
        if (staminaSlider != null && stamina != null)               OnStaminaUpdate?.Invoke();
        if (infectionSlider != null && infection != null)           OnInfectionUpdate?.Invoke(); 
        if (flashlightSlider != null && flashlight != null)         OnFlashlightUpdate?.Invoke(); 
        if (nightVisionSlider != null && nightVision != null)       OnNightvisionUpdate?.Invoke(); 
        if (interactionText != null && interactionString != null)   OnInteractionTextUpdate?.Invoke(); 
        if (transcriptText != null && transcriptString != null)     OnTranscriptTextUpdate?.Invoke();
        if (notificationText != null && notificationQueue != null)  OnNotificationQueueUpdate?.Invoke();
    }

    private void OnEnable()
    {
        if (healthSlider != null && health != null)                health.OnVariableValueChanged += OnHealthUpdate;
        if (staminaSlider != null && stamina != null)              stamina.OnVariableValueChanged += OnStaminaUpdate;
        if (infectionSlider != null && infection != null)          infection.OnVariableValueChanged += OnInfectionUpdate;
        if (flashlightSlider != null && flashlight != null)        flashlight.OnVariableValueChanged += OnFlashlightUpdate;
        if (nightVisionSlider != null && nightVision != null)      nightVision.OnVariableValueChanged += OnNightvisionUpdate;
        if (interactionText != null && interactionString != null)  interactionString.OnVariableValueChanged += OnInteractionTextUpdate;
        if (transcriptText != null && transcriptString != null)    transcriptString.OnVariableValueChanged += OnTranscriptTextUpdate;
        if (notificationText != null && notificationQueue != null) notificationQueue.OnVariableValueChanged += OnNotificationQueueUpdate;
    }

    private void OnDisable()
    {
        if (healthSlider != null && health != null)                health.OnVariableValueChanged -= OnHealthUpdate;
        if (staminaSlider != null && stamina != null)              stamina.OnVariableValueChanged -= OnStaminaUpdate;
        if (infectionSlider != null && infection != null)          infection.OnVariableValueChanged -= OnInfectionUpdate;
        if (flashlightSlider != null && flashlight != null)        flashlight.OnVariableValueChanged -= OnFlashlightUpdate;
        if (nightVisionSlider != null && nightVision != null)      nightVision.OnVariableValueChanged -= OnNightvisionUpdate;
        if (interactionText != null && interactionString != null)  interactionString.OnVariableValueChanged -= OnInteractionTextUpdate;
        if (transcriptText != null && transcriptString != null)    transcriptString.OnVariableValueChanged -= OnTranscriptTextUpdate;
        if (notificationText != null && notificationQueue != null) notificationQueue.OnVariableValueChanged -= OnNotificationQueueUpdate;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"DeltaTime: {Time.deltaTime}");
            notificationQueue.Enqueue($"DeltaTime: {Time.deltaTime}");
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
}
