using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	AIZombieState_Pursuit1
// DESC		:	A Zombie state for pursuing the target
// --------------------------------------------------------------------------
public class AIZombieState_Pursuit1 : AIZombieState
{
    [SerializeField, Range(0.0f, 10.0f)] private float speed = 3.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float lookAtWeight = 0.7f;
    [SerializeField, Range(0.0f, 90.0f)] private float lookAtAngleThreshold = 15.0f;

    [SerializeField] private float slerpSpeed               = 5.0f;
    [SerializeField] private float repathDistanceMultiplier = 0.035f;
    [SerializeField] private float repathVisualMinDuration  = 0.05f;
    [SerializeField] private float repathVisualMaxDuration  = 5.0f;
    [SerializeField] private float repathAudioMinDuration   = 0.25f;
    [SerializeField] private float repathAudioMaxDuration   = 5.0f;
    [SerializeField] private float maxDuration              = 40.0f;

    //Private Fields
    private float timer = 0.0f;
    private float repathTimer = 0.0f;
    private float currentLookAtWeight = 0.0f;


    public override void OnEnterState()
    {
        Debug.Log("Entering Pursuit State");
        base.OnEnterState();

        if (zombieStateMachine == null)
            return;

        //Set zombie stats
        zombieStateMachine.NavAgentControl(true, false);
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Feeding = false;
        zombieStateMachine.AttackType = 0;

        //Zombies will only pursue for so long before braking off 
        timer = 0.0f;
        repathTimer = 0.0f;

        //Set path
        zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.TargetPosition);
        zombieStateMachine.NavAgent.isStopped = false;
    }


    public override AIStateType GetStateType()
    {
        return AIStateType.Pursuit;
    }

    public override AIStateType OnUpdate()
    {
        //Increment timers and
        timer += Time.deltaTime;
        repathTimer += Time.deltaTime;
        //Go back to patrol state if been in pursuit for too long
        if (timer > maxDuration)
            return AIStateType.Patrol;

        //Check if in range for melee
        if(zombieStateMachine.TargetType == AITargetType.Visual_Player && zombieStateMachine.InMeleeRange)
        {
            return AIStateType.Attack;
        }

        // Otherwise this is navigation to areas of interest so use the standard target threshold
        if (zombieStateMachine.IsTargetReached)
        {
            switch (zombieStateMachine.TargetType)
            {
                case AITargetType.Audio:
                case AITargetType.Visual_Light:
                    zombieStateMachine.ClearTarget();   //Clear threat
                    return AIStateType.Alerted;         //Become alert and scan for targets
                
                case AITargetType.Visual_Food:
                    return AIStateType.Feeding;       
                default:
                    break;
            }
        }

        //If for any reaason the nav agent has lost its path then call then drop into alerted state
        //so it will try to re-acquire the target or eventually give up and resume patrolling 
        if(zombieStateMachine.NavAgent.isPathStale  ||
           (!zombieStateMachine.NavAgent.hasPath && !zombieStateMachine.NavAgent.pathPending)    ||
           zombieStateMachine.NavAgent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        { 
            return AIStateType.Alerted;         
        }

        //if the agent has no path just stop it
        if (zombieStateMachine.NavAgent.pathPending)
        {
            zombieStateMachine.Speed = 0.0f;
            return AIStateType.Patrol;
        }
        else //Stop angent and do other things 
        {
            zombieStateMachine.Speed = speed;

            //If we are close to the player and we still have the player in our vision then keep facing right at the player
            if (!zombieStateMachine.UseRootRotation &&
                zombieStateMachine.TargetType == AITargetType.Visual_Player &&
                zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player &&
                zombieStateMachine.IsTargetReached)
            {
                Vector3 targetPos = zombieStateMachine.TargetPosition;
                targetPos.y = zombieStateMachine.transform.position.y;
                Quaternion newRot = Quaternion.LookRotation(targetPos - zombieStateMachine.transform.position);

                //We're extremely close to the player now and it's better to set the rotation instantly (NO slerp)
                zombieStateMachine.transform.rotation = newRot;
            }
            else if (!zombieStateMachine.UseRootRotation && !zombieStateMachine.IsTargetReached) //Generic pursuit condition
            {
                Quaternion newRot = Quaternion.LookRotation(zombieStateMachine.NavAgent.desiredVelocity);

                //Use slerp as we are not close to the player (so it is ok to lose some "navigation accuracy")
                zombieStateMachine.transform.rotation = Quaternion.Slerp(zombieStateMachine.transform.rotation, newRot, Time.deltaTime * slerpSpeed);
            }
            else if (zombieStateMachine.IsTargetReached) //target was not the player
            {
                return AIStateType.Alerted;
            }
        }

        //Do we have a visual threat that is the player?
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player)
        {
            //Check if worth re-calculating position (check if position is different)
            if(zombieStateMachine.TargetPosition != zombieStateMachine.visualThreat.Position)
            {
                //repath more frequently as we get closer to the target (save some CPU cycles)
                if(Mathf.Clamp(zombieStateMachine.visualThreat.Distance * repathDistanceMultiplier, repathVisualMinDuration, repathVisualMaxDuration) < repathTimer)
                {
                    //Repath the agent
                    zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.visualThreat.Position);
                    repathTimer = 0.0f;
                }
            }

            //Make this is the current target
            zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
            //Remain is pursuit
            return AIStateType.Pursuit;
        }

        //if target is the last sighting of a player then remain in pursuit
        //as nothing else can override it (player has the highest priority)
        if (zombieStateMachine.TargetType == AITargetType.Visual_Player)
            return AIStateType.Pursuit;


        //Player's light
        if (zombieStateMachine.visualThreat.Type == AITargetType.Visual_Light)
        {
            //Check if we currently have a lower priority target
            if (zombieStateMachine.TargetType == AITargetType.Audio ||
               zombieStateMachine.TargetType == AITargetType.Visual_Food)
            {
                //Make light the current target
                zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
                return AIStateType.Alerted;
            }
            else if (zombieStateMachine.TargetType == AITargetType.Visual_Light)
            {
                //Get unique ID of the collider of our current target
                int currentID = zombieStateMachine.TargetColliderID;

                //Check if this is the same light (possible future multiplayer support)
                if (currentID == zombieStateMachine.visualThreat.Collider.GetInstanceID())
                {
                    //Position is different (maybe same threat but it has moved so repath periodically)
                    if (zombieStateMachine.TargetPosition != zombieStateMachine.visualThreat.Position)
                    {
                        //Repath more frequently as we get closer to the target (save some CPU cycles)
                        if (Mathf.Clamp(zombieStateMachine.visualThreat.Distance * repathDistanceMultiplier, repathVisualMinDuration, repathVisualMaxDuration) < repathTimer)
                        {
                            //Repath the agent
                            zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.visualThreat.Position);
                            repathTimer = 0.0f;
                        }
                    }

                    //Make this is the current target
                    zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
                    //Remain is pursuit
                    return AIStateType.Pursuit;
                }
                else //It is a different light 
                {
                    //Make this is the current target
                    zombieStateMachine.SetTarget(zombieStateMachine.visualThreat);
                    //Go in alert state
                    return AIStateType.Alerted;
                }
            }
        }
        else if (zombieStateMachine.audioThreat.Type == AITargetType.Audio)
        {
            //Check if we currently have a lower priority target
            if (zombieStateMachine.TargetType == AITargetType.Visual_Food)
            {
                //Make audio the current target
                zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);
                return AIStateType.Alerted;
            }
            else if (zombieStateMachine.TargetType == AITargetType.Audio)
            {
                //Get unique ID of the collider of our current target
                int currentID = zombieStateMachine.TargetColliderID;

                //Check if this is the same sound (possible future multiplayer support)
                if (currentID == zombieStateMachine.audioThreat.Collider.GetInstanceID())
                {
                    //Position is different (maybe same threat but it has moved so repath periodically)
                    if (zombieStateMachine.TargetPosition != zombieStateMachine.audioThreat.Position)
                    {
                        //Repath more frequently as we get closer to the target (save some CPU cycles)
                        if (Mathf.Clamp(zombieStateMachine.audioThreat.Distance * repathDistanceMultiplier, repathAudioMinDuration, repathAudioMaxDuration) < repathTimer)
                        {
                            //Repath the agent
                            zombieStateMachine.NavAgent.SetDestination(zombieStateMachine.audioThreat.Position);
                            repathTimer = 0.0f;
                        }
                    }

                    //Make this is the current target
                    zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);
                    //Remain is pursuit
                    return AIStateType.Pursuit;
                }
                else //It is a different audio 
                {
                    //Make this is the current target
                    zombieStateMachine.SetTarget(zombieStateMachine.audioThreat);
                    //Go in alert state
                    return AIStateType.Alerted;
                }
            }
        }

        //Default
        return AIStateType.Pursuit;
    }

    public override void OnAnimatorIKUpdated()
    {
        if (zombieStateMachine == null)
            return;

        //Make the head look at the player if less than the set threshold 
        if (Vector3.Angle(zombieStateMachine.transform.forward, zombieStateMachine.TargetPosition - zombieStateMachine.transform.position) < lookAtAngleThreshold)
        {
            zombieStateMachine.Anim.SetLookAtPosition(zombieStateMachine.TargetPosition + Vector3.up);
            currentLookAtWeight = Mathf.Lerp(currentLookAtWeight, lookAtWeight, Time.deltaTime);
            zombieStateMachine.Anim.SetLookAtWeight(currentLookAtWeight);

        }
        else
        {
            currentLookAtWeight = Mathf.Lerp(currentLookAtWeight, 0.0f, Time.deltaTime);
            zombieStateMachine.Anim.SetLookAtWeight(currentLookAtWeight);
        }
    }
}
