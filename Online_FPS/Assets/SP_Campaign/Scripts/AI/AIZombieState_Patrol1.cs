using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --------------------------------------------------------------------------
// CLASS	:	AIZombieState_Patrol1
// DESC		:	Generic patroling Behaviour for a Zombie
// --------------------------------------------------------------------------
public class AIZombieState_Patrol1 : AIZombieState
{
    //Inspector assigned 
    [SerializeField] private float slerpSpeed = 5.0f;

    [SerializeField, Range(0.0f, 360.0f)] private float turnOnSpotThreshold = 80.0f;
    [SerializeField, Range(0.0f, 3.0f)]   private float speed = 1.0f;


    public override void OnEnterState()
    {
        Debug.Log("Entering Patrol State");
        base.OnEnterState();

        if (zombieStateMachine == null)
        {
            Debug.Log("zombieStateMachine is null");
            return;
        }

        //Set zombie stats
        zombieStateMachine.NavAgentControl(true, false);
        zombieStateMachine.Speed = speed;
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Feeding = false;
        zombieStateMachine.AttackType = 0;

        //Set destination 
        zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.GetWaypointPosition(false));
        
        //Make sure nav agent is switched on
        zombieStateMachine.NavAgent.isStopped = false;
    }


    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    public override AIStateType OnUpdate()
    {
        if (zombieStateMachine == null)
            return AIStateType.Patrol;

        //Check if the player is around 
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player)
        {
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
        }

        //Check if the player's light is visible 
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Light)
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
            //The more hungry, the more the zombie will be motivated to travel greater distances to get food
            if((1.0f - zombieStateMachine.Satisfaction) > (zombieStateMachine.visualThreat.Distance / zombieStateMachine.SensorRadius))
            {
                zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
                return AIStateType.Pursuit;
            }
        }

        //Find angle between current Forward and the position we need to steer at
        float angle = Vector3.Angle(zombieStateMachine.transform.forward, (zombieStateMachine.NavAgent.steeringTarget - zombieStateMachine.transform.position));
        if(angle > turnOnSpotThreshold)
        {
            return AIStateType.Alerted;
        }

        //Rotate zombie
        if(!zombieStateMachine.UseRootRotation)
        {
            Quaternion newRot = Quaternion.LookRotation(zombieStateMachine.NavAgent.desiredVelocity);
            
            //WARNING: By using slerp to rotate the zombie, the agent will lose "navigation accuracy",
            //         but the rotation will look a lot smoother (no snap). Some tuning of the "T" value
            //         might be needed if the rotation does not keep up.
            zombieStateMachine.transform.rotation = Quaternion.Slerp(zombieStateMachine.transform.rotation, newRot, Time.deltaTime * slerpSpeed);
        }

        //Set next waypoint 
        if(zombieStateMachine.NavAgent.isPathStale  || 
           !zombieStateMachine.NavAgent.hasPath     || 
           zombieStateMachine.NavAgent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        {
            zombieStateMachine.GetWaypointPosition(true);
        }

        //Stay in patrol state if none of the above  
        return AIStateType.Patrol;
    }

    public override void OnDestinationReached(bool _isReached) 
    {
        if (zombieStateMachine == null || !_isReached)
            return;

        //Set next destination
        if(zombieStateMachine.TargetType == AITargetType.WayPoint)
        {
            zombieStateMachine.GetWaypointPosition(true);
        }
    }

    public override void OnAnimatorIKUpdated() 
    {
        if (zombieStateMachine == null)
            return;

        //Make the head look at the target
        //zombieStateMachine.Anim.SetLookAtPosition(zombieStateMachine.TargetPosition + Vector3.up);
        //zombieStateMachine.Anim.SetLookAtWeight(0.55f);
    }    
}
