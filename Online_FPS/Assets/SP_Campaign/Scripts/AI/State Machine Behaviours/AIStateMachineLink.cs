using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ComChannelName { ComChannel1, ComChannel2, ComChannel3, ComChannel4 }

// --------------------------------------------------------------------------
// CLASS	:	AIStateMachineLink
// DESC		:	Used as a base class for any StateMachineBehaviour that needs
//              to communicate with its AI State Machine.
// --------------------------------------------------------------------------
public class AIStateMachineLink : StateMachineBehaviour
{
    protected AIStateMachine stateMachine;
    public AIStateMachine StateMachine { set{ stateMachine = value; } }
}
