using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateMachineLink : StateMachineBehaviour
{
    protected AIStateMachine stateMachine;
    public AIStateMachine StateMachine { set{ stateMachine = value; } }
}
