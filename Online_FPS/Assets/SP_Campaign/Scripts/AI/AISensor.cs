using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISensor : MonoBehaviour
{
    private AIStateMachine parentStateMachine = null;
    public AIStateMachine ParentStateMachine { set { parentStateMachine = value; } }


    private void OnTriggerEnter(Collider other)
    {
        if(parentStateMachine != null)
        {
            parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (parentStateMachine != null)
        {
            parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (parentStateMachine != null)
        {
            parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, other);
        }
    }
}
