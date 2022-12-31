using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if(machine != null)
        {
            machine.InMeleeRange = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if (machine != null)
        {
            machine.InMeleeRange = false;
        }
    }
}
