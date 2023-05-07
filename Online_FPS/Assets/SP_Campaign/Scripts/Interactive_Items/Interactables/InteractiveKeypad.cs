using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveKeypad : InteractiveItem
{
    [SerializeField] protected Transform elevator = null;
    [SerializeField] protected AudioCollection audioCollection = null;
    [SerializeField] protected int bank = 0;
    [SerializeField] protected float activationDelay = 0.0f;

    private bool isActivated = false;

    public override string GetText()
    {
        ApplicationManager appDatabase = ApplicationManager.Instance;
        if (appDatabase == null)
            return string.Empty;

        //Get states
        string powerState      = appDatabase.GetGameState("POWER");
        string lockdownState   = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        //If we have not turned on the power
        if(string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE"))
        {
            return "Keypad : No Power";
        }
        //Or we have not deactivated lockdown 
        else if (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE"))
        {
            return "Keypad : Under Lockdown";
        }
        //Or we don't have access to the code yet
        else if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE"))
        {
            return "Keypad : Access Code Required";
        }

        //We have everything we need
        return "Keypad";
    }

    public override void Activate(CharacterManager _chrManager)
    {
        if (isActivated)
            return;

        ApplicationManager appDatabase = ApplicationManager.Instance;
        if (appDatabase == null)
            return;

        //Get states
        string powerState = appDatabase.GetGameState("POWER");
        string lockdownState = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        //Check the conditions (commented in 'GetText)
        if ((string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE")) ||
            (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE")) ||
            (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE")))
            return;

        //Activate elevator
        StartCoroutine(DoDelayedActivation(_chrManager));

        isActivated = true;
    }

    protected IEnumerator DoDelayedActivation(CharacterManager _chrManager)
    {
        if (elevator == null)
            yield break;

        //Play the sound 
        if(audioCollection != null)
        {
            AudioClip clip = audioCollection[bank];
            if(clip != null)
            {
                //Play it as one shot sound
                AudioManager.Instance?.PlayOneShotSound(audioCollection.AudioGroup,
                                                       clip,
                                                       elevator.position,
                                                       audioCollection.Volume,
                                                       audioCollection.SpatialBlend,
                                                       audioCollection.Priority);
            }
        }

        //Freeze fps motor so it can rotate/jump/crouch but not move off the elevator
        if (_chrManager.FPSController)
        {
            _chrManager.FPSController.FreezeMovement = true;
        }

        //Wait offset
        yield return new WaitForSeconds(activationDelay);
    
        if(_chrManager != null)
        {
            //Child character manager to elevator
            _chrManager.transform.parent = elevator;

            //Play elevator animation
            Animator anim = elevator.GetComponent<Animator>();
            if(anim != null)
            {
                anim.SetTrigger("Activate");
            }

            //Unfreeze player (elevator has invisible walls)
            if (_chrManager.FPSController)
            {
                _chrManager.FPSController.FreezeMovement = false;
            }
        }
    }
}
