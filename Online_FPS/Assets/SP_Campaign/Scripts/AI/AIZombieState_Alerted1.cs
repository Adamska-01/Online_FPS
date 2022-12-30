using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Alerted1 : AIZombieState
{
    //Inspector Assigned
    [SerializeField, Range(1.0f, 60.0f)] private float maxDuration = 10.0f;
    [SerializeField, Range(0.0f, 360.0f)] private float wayPointAngleThreshold = 90.0f;
    [SerializeField, Range(0.0f, 360.0f)] private float threatAngleThreshold = 10.0f;
    [SerializeField] private float directionChangeTime = 1.5f;

    private float timer = 0.0f;
    private float directionChangeTimer = 0.0f;


    public override void OnEnterState()
    {
        Debug.Log("Entering Alerted State");
        base.OnEnterState();

        if (zombieStateMachine == null)
            return;

        //Set zombie stats
        zombieStateMachine.NavAgentControl(true, false);
        zombieStateMachine.Speed = 0;
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Feeding = false;
        zombieStateMachine.AttackType = 0;

        //Set timer 
        timer = maxDuration;
        directionChangeTimer = 0.0f;
    }


    public override AIStateType GetStateType()
    {
        return AIStateType.Alerted;
    }

    public override AIStateType OnUpdate()
    {
        //Decrement timer
        timer -= Time.deltaTime;
        directionChangeTimer += Time.deltaTime;
        if (timer <= 0.0f)
        {
            //Go back to the waypoint the zombie was on before he got disturbed (false)
            zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.GetWaypointPosition(false));
            zombieStateMachine.NavAgent.isStopped = false;
            timer = maxDuration;
        }

        //Check if the player is around 
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
        }

        //Check if audio threat is detected
        if (zombieStateMachine.audioThreat.Type == AITargetType.Audio)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);

            //Set timer back to max (stay in the alerted state)
            timer = maxDuration;
        }

        //Check if the player's light is visible 
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Light)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);

            //Set timer back to max (stay in the alerted state)
            timer = maxDuration;
        }

        //Assuming that there isn't an audio threat, check for food (lowest priority)
        if (zombieStateMachine.audioThreat.Type == AITargetType.None &&
            zombieStateMachine.visualThreat.Type == AITargetType.Visual_Food &&
            zombieStateMachine.TargetType == AITargetType.None)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
        }

        float angle;
        //Handle visual and audio threats
        if ((zombieStateMachine.TargetType == AITargetType.Audio || zombieStateMachine.TargetType == AITargetType.Visual_Light) && !zombieStateMachine.IsTargetReached)
        {
            //Find angle between forward vec. and target
            angle = AIState.FindSignedAngle(zombieStateMachine.transform.forward, zombieStateMachine.TargetPosition - zombieStateMachine.transform.position);

            //Go in persuit if the angle is less than the treshold
            if (zombieStateMachine.TargetType == AITargetType.Audio && Mathf.Abs(angle) < threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }

            if (directionChangeTimer > directionChangeTime)
            {
                //If more intelligent, the more chance to turn the right way 
                if (Random.value < zombieStateMachine.Intelligence)
                {
                    zombieStateMachine.Seeking = (int)Mathf.Sign(angle);
                }
                else
                {
                    zombieStateMachine.Seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                }
                directionChangeTimer = 0.0f;
            }
        }
        else if (zombieStateMachine.TargetType == AITargetType.WayPoint && !zombieStateMachine.NavAgent.pathPending) //Handle waypoints
        {
            //Find angle between forward vec. and steering target
            angle = AIState.FindSignedAngle(zombieStateMachine.transform.forward, zombieStateMachine.NavAgent.steeringTarget - zombieStateMachine.transform.position);

            //Check if alligned enough (resume patro)
            if (Mathf.Abs(angle) < wayPointAngleThreshold)
            {
                return AIStateType.Patrol;
            }
            zombieStateMachine.Seeking = (int)Mathf.Sign(angle);
        }
        else
        {
            if (directionChangeTimer > directionChangeTime)
            {
                zombieStateMachine.Seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                directionChangeTimer = 0.0f;
            }
        }

        return AIStateType.Alerted;
    }
}

