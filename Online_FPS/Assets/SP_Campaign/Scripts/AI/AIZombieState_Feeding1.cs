using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	AIZombieState_Feeding1
// DESC		:	A Zombie state for feeding the zombie (increase satisfaction)
// --------------------------------------------------------------------------
public class AIZombieState_Feeding1 : AIZombieState
{
    //Inspector-Assigned
    [SerializeField] private float slerpSpeed = 5.0f;

    //Animation Hash
    private int eatingStateHash = Animator.StringToHash("Feeding_State");
    private int eatingLayerIndex = -1;


    public override void OnEnterState()
    {
        Debug.Log("Entering Feeding State");
        base.OnEnterState();

        if (zombieStateMachine == null)
            return;

        //Get the eating layer index (Cinematic)
        if(eatingLayerIndex == -1)
        {
            eatingLayerIndex = zombieStateMachine.Anim.GetLayerIndex("Cinematic");
        }

        //Set zombie stats
        zombieStateMachine.Feeding = true;
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Speed = 0;
        zombieStateMachine.AttackType = 0;
        //Update position but not rotation
        zombieStateMachine.NavAgentControl(true, false);
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if(zombieStateMachine != null)
        {
            zombieStateMachine.Feeding = false;
        }
    }


    public override AIStateType GetStateType()
    {
        
        return AIStateType.Feeding;
    }

    public override AIStateType OnUpdate()
    {
        //Check satisfaction level every frame
        if (zombieStateMachine.Satisfaction > 0.9f)
        {
            zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }

        //Check if there is any visual threat (different from food) and if yes, set that as the current target 
        if (zombieStateMachine.visualThreat.Type != AITargetType.None &&
           zombieStateMachine.visualThreat.Type != AITargetType.Visual_Food)
        {
            //Make light the current target
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Alerted;
        }

        //Same check as the one up, but for audio threats 
        if (zombieStateMachine.audioThreat.Type == AITargetType.Audio)
        {
            //Make light the current target
            zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);
            return AIStateType.Alerted;
        }

        //Icrement satisfation if in the "animation feeding" state 
        if(zombieStateMachine.Anim.GetCurrentAnimatorStateInfo(eatingLayerIndex).shortNameHash == eatingStateHash)
        {
            zombieStateMachine.Satisfaction = Mathf.Min(zombieStateMachine.Satisfaction + (Time.deltaTime * zombieStateMachine.ReplenishRate), 1.0f);
        }

        if (!zombieStateMachine.UseRootRotation)
        {
            //Keep zombie facing the target at all time (food)
            Vector3 targetPos = zombieStateMachine.TargetPosition;
            targetPos.y = zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - zombieStateMachine.transform.position);
            zombieStateMachine.transform.rotation = Quaternion.Slerp(zombieStateMachine.transform.rotation, newRot, Time.deltaTime * slerpSpeed);
            zombieStateMachine.Satisfaction = Mathf.Min(zombieStateMachine.Satisfaction + ((Time.deltaTime * zombieStateMachine.ReplenishRate)/100.0f), 1.0f);
        }

        //Stay in the feeding state
        return AIStateType.Feeding;
    }
}
