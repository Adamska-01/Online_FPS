using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//ONLY bool is supported (at the momement)
public enum AnimatorParameterType { Trigger, Bool, Int, Float, String };

[System.Serializable]
public class AnimatorParameter
{
    public AnimatorParameterType parameterType = AnimatorParameterType.Bool;
    public string parameterName = null;
    public string parameterValue = null;
}

[System.Serializable]
public class AnimatorConfigurator
{
    [SerializeField] public Animator anim = null;
    [SerializeField] public List<AnimatorParameter> animatorParams = new List<AnimatorParameter>();
}


public class InteractiveGenericSwitch : InteractiveItem
{
    [Header("Game Staate Management")]
    [SerializeField] protected List<GameState> requiredStates = new List<GameState>();
    [SerializeField] protected List<GameState> activateStates = new List<GameState>();
    [SerializeField] protected List<GameState> deactivateStates = new List<GameState>();

    [Header("Message")]
    [SerializeField, TextArea(3, 10)] protected string stateNotSetText = "";
    [SerializeField, TextArea(3, 10)] protected string stateSetText = "";
    [SerializeField, TextArea(3, 10)] protected string ObjectActiveText = "";

    [Header("Activation Parameters")] //Activation Sound
    [SerializeField] protected float activationDelay = 1.0f;
    [SerializeField] protected float deactivationDelay = 1.0f;
    [SerializeField] protected AudioCollection activationSounds = null;
    [SerializeField] protected AudioSource audioSource = null;
    //[SerializeField] protected string audioGroupName = null;

    [Header("Operating Mode")] //Operation
    [SerializeField] protected bool startActivate = false;
    [SerializeField] protected bool canToggle = false;

    [Header("Configuarable Entities")]
    //Animators that need to be affected by the trigger
    [SerializeField] protected List<AnimatorConfigurator> animations = new List<AnimatorConfigurator>();
    //Materials that need to have their emissive properties affected by the trigger 
    [SerializeField] protected List<MaterialController> materialControllers = new List<MaterialController>();
    //Gameobject that need to be activated or deactivated by this trigger
    [SerializeField] protected List<GameObject> objectActivators = new List<GameObject>();
    [SerializeField] protected List<GameObject> objectDeactivators = new List<GameObject>();

    //Private/Protected
    protected IEnumerator coroutine = null;
    protected bool activated = false;
    protected bool firstUse = false;


    private void Start()
    {
        //Registers this collider with the app manager (IMPORTANT, otherwise you can't interact with it)
        base.Start();

        //Activate Material Controller
        for (int i = 0; i < materialControllers.Count; i++)
        {
            if(materialControllers[i] != null)
            {
                materialControllers[i].OnStart();
            }
        }

        //Turn off all objects that should be activated
        for (int i = 0; i < objectActivators.Count; i++)
        {
            if (objectActivators[i] != null)
            {
                objectActivators[i].SetActive(false);
            }
        }

        //Turn on all objects that should be deactivated
        for (int i = 0; i < objectDeactivators.Count; i++)
        {
            if (objectDeactivators[i] != null)
            {
                objectDeactivators[i].SetActive(true);
            }
        }

        if(startActivate)
        {
            Activate(null);
            firstUse = false; //Can still be used again
        }
    }


    public override string GetText()
    {
        //If we have no application app manager or this switch is disabled the return null
        ApplicationManager appManager = ApplicationManager.Instance;
        if (!enabled || appManager == null)
            return string.Empty;

        //If its already activated the just return the activated text
        if(activated)
        {
            return ObjectActiveText;
        }

        //We need to test all the states that need to be set to see if
        //this item can be activated as that determines the text we send back
        bool requiredStates = AreRequiredStatesSet();
        if(!requiredStates)
        {
            return stateNotSetText;
        }
        else
        {
            return stateSetText;
        }
    }

    public override void Activate(CharacterManager _chrManager)
    {
        ApplicationManager appManager = ApplicationManager.Instance;
        if (appManager == null)
            return;

        //If we are already in a different state to the starting state and we are not in
        //toggle mode then this item has been switched on/off and can't no longer be alteread
        if (firstUse && !canToggle)
            return;

        if(!activated)
        {
            bool requiredStates = AreRequiredStatesSet();
            if (!requiredStates)
                return;
        }

        //Object state has been switched
        activated = !activated;
        firstUse = true;

        //Play the activation sound effect
        if(activationSounds != null && activated)
        {
            AudioClip clipToPlay = activationSounds[0]; //Activate sound should always be on bank 0
            if (clipToPlay == null)
                return;

            //if audio source has been specified then use it. This is good for playing
            //looping sounds or sounds that need to happen nowhere near the source
            if(audioSource != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.volume = activationSounds.Volume; 
                audioSource.spatialBlend = activationSounds.SpatialBlend; 
                audioSource.priority = activationSounds.Priority; 
                audioSource.outputAudioMixerGroup = AudioManager.Instance.GetAudioGroupFromTrackName(activationSounds.AudioGroup);

                audioSource.Play();
            }
        }

        //Get the coroutine to perform the delayed activation and if it's playing stop it
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        //Start a new coroutine to do the activation
        coroutine = DoDelayedActivation();
        StartCoroutine(coroutine);
    }

    protected virtual IEnumerator DoDelayedActivation()
    {
        foreach (AnimatorConfigurator configurator in animations)
        {
            if(configurator != null)
            {
                foreach (AnimatorParameter param in configurator.animatorParams)
                {
                    //TODO: Support other types
                    switch (param.parameterType)
                    {
                        case AnimatorParameterType.Trigger:
                            break;
                        case AnimatorParameterType.Bool:
                            bool bolean = bool.Parse(param.parameterValue);
                            configurator.anim.SetBool(param.parameterName, activated ? bolean : !bolean);
                            break;
                        case AnimatorParameterType.Int:
                            break;
                        case AnimatorParameterType.Float:
                            break;
                        case AnimatorParameterType.String:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        yield return new WaitForSeconds(activated ? activationDelay : deactivationDelay);

        //Set the states that should be set when activating/deactivating
        SetActivationStates();

        if(activationSounds != null && !activated)
        {
            AudioClip clipToPlay = activationSounds[1]; //Deativate sound should always be on bank 1
            if (audioSource != null && clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.volume = activationSounds.Volume;
                audioSource.spatialBlend = activationSounds.SpatialBlend;
                audioSource.priority = activationSounds.Priority;
                audioSource.outputAudioMixerGroup = AudioManager.Instance.GetAudioGroupFromTrackName(activationSounds.AudioGroup);

                audioSource.Play();
            }
        }

        //If we get here then we allow to enable this object. So first turn
        //on any game objects that should be made active by this action
        for (int i = 0; i < objectActivators.Count; i++)
        {
            if (objectActivators[i] != null)
            {
                objectActivators[i].SetActive(activated);
            }
        }
        for (int i = 0; i < objectDeactivators.Count; i++)
        {
            if (objectDeactivators[i] != null)
            {
                objectDeactivators[i].SetActive(!activated);
            }
        }

        //Activate material controllers
        for (int i = 0; i < materialControllers.Count; i++)
        {
            if (materialControllers[i] != null)
            {
                materialControllers[i].Activate(activated);
            }
        }
    }

    protected bool AreRequiredStatesSet()
    {
        ApplicationManager appManager = ApplicationManager.Instance;
        if (appManager == null)
            return false;

        //Assume the states are all set and then loop to find a state to disprove this 
        for (int i = 0; i < requiredStates.Count; i++)
        {
            GameState state = requiredStates[i];

            //Does the current state exist in the app dictionary?
            string result = appManager.GetGameState(state.key);
            if (string.IsNullOrEmpty(result) || !result.Equals(state.value))
                return false;
        }

        return true;
    }

    protected void SetActivationStates()
    {
        ApplicationManager appManager = ApplicationManager.Instance;
        if (appManager == null)
            return;

        if(activated)
        {
            foreach (GameState state in activateStates)
            {
                appManager.SetGameState(state.key, state.value);
            }
        }
        else
        {
            foreach (GameState state in deactivateStates)
            {
                appManager.SetGameState(state.key, state.value);
            }
        }
    }
}
