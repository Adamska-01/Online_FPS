using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	AIZombieState_Idle1
// DESC		:	An AIState that implements a zombie idle Behaviour
// --------------------------------------------------------------------------
public class AIZombieState_Idle1 : AIZombieState
{
    //Inspector Assigned 
    [SerializeField] Vector2 idleTimeRange = new Vector2(10.0f, 60.0f);

    //Private
    private float idleTime = 0.0f;
    private float timer = 0.0f;


    public override void OnEnterState()
    {
        Debug.Log("Entering Idle State");
        base.OnEnterState();

        if (zombieStateMachine == null)
            return;

        //Set timer
        idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        timer = 0.0f;

        //Set zombie stats
        zombieStateMachine.NavAgentControl(true, false);
        zombieStateMachine.Speed = 0;
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Feeding = false;
        zombieStateMachine.AttackType = 0;

        //Clear any current target 
        zombieStateMachine.ClearTarget();
    }

    public override AIStateType GetStateType()
    {
        Debug.Log("State Type being fetched by state machine");
        return AIStateType.Idle;
    }

    public override AIStateType OnUpdate()
    {
        if (zombieStateMachine == null)
            return AIStateType.Idle;

        //Check if the player is around 
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
        }

        //Check if the player's light is visible 
        if(zombieStateMachine.visualThreat.Type == AITargetType.Visual_Light)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Alerted;
        }

        //Check if audio threat is detected
        if (zombieStateMachine.audioThreat.Type == AITargetType.Audio)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);
            return AIStateType.Alerted;
        }

        //Check if Food is around (lowest priority)
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Food)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
        }

        //Increment timer
        timer += Time.deltaTime;
        if (timer > idleTime)
        {
            return AIStateType.Patrol;
        }

        //Stay idle if none of the above  
        return AIStateType.Idle;
    }
}
