using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class AIZombieState : AIState
{
    protected AIZombieStateMachine zombieStateMachine = null;

    protected int playerLayerMask   = -1;
    protected int bodyPartLayerMask = -1;
    protected int visualRaycastMask = -1;

    private void Awake()
    {
        playerLayerMask = LayerMask.GetMask("Player", "AI_Body_Part") + 1; // + Default layer
        bodyPartLayerMask = LayerMask.NameToLayer("AI_Body_Part");
        visualRaycastMask = LayerMask.GetMask("Player", "AI_Body_Part", "Visual_Aggravator") + 1; // + Default layer
    }


    public override void SetStateMachine(AIStateMachine _machine)
    {
        if(_machine.GetType() == typeof(AIZombieStateMachine))
        {
            base.SetStateMachine(_machine);
            zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }

    public override void OnTriggerEvent(AITriggerEventType _eventType, Collider _other)
    {
        if (zombieStateMachine == null)
            return;

        if(_eventType != AITriggerEventType.Exit)
        {
            AITargetType currentType = zombieStateMachine.visualThreat.Type;

            if(_other.CompareTag("Player")) //Player has the highest priority
            {
                //Might be 2 players (?) - Future multiplayer support (?)
                float distance = Vector3.Distance(zombieStateMachine.SensorPosition, _other.transform.position);
                if(currentType != AITargetType.Visual_Player || 
                  (currentType == AITargetType.Visual_Player && distance < zombieStateMachine.visualThreat.Distance))
                {
                    if (ColliderIsVisible(_other, out RaycastHit raycastHit, playerLayerMask))
                    {
                        //Target is close and within FOV so store as the current most dangerous threat
                        zombieStateMachine.visualThreat.Set(AITargetType.Visual_Player, _other, _other.transform.position, distance);
                    }
                }
            }
            else if (_other.CompareTag("Flashlight") && currentType != AITargetType.Visual_Player)
            {
                BoxCollider flashlightTrigger = (BoxCollider)_other;
                float distanceToThreat = Vector3.Distance(zombieStateMachine.SensorPosition, flashlightTrigger.transform.position);
                float zSize = flashlightTrigger.size.z * flashlightTrigger.transform.lossyScale.z; //(take parent scale into account)
                float aggravationFactor = distanceToThreat / zSize; //Range(0-1)

                //if aggravation factor is smaller than sight and intelligence
                if(aggravationFactor < zombieStateMachine.Sight && aggravationFactor < zombieStateMachine.Intelligence)
                {
                    zombieStateMachine.visualThreat.Set(AITargetType.Visual_Light, _other, _other.transform.position, distanceToThreat);
                }
            }
            else if (_other.CompareTag("AI_Sound_Emitter"))
            {
                SphereCollider soundTrigger = (SphereCollider)_other;
                if (soundTrigger == null)
                    return;

                Vector3 agentSensorPosition = zombieStateMachine.SensorPosition;

                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out Vector3 soundPos, out float soundRadius);

                //Calculate distance from sound 
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;
                
                //Calculate a distance factor such that it is 1.0 when at sound radius, 0 when at center
                float distanceFactor = distanceToThreat / soundRadius; //Range(0-1)

                //Bias the factor based on hearing ability of agent
                distanceFactor += distanceFactor * (1.0f - zombieStateMachine.Hearing);

                if (distanceFactor > 1.0f) //Too far away
                    return;
                
                //if we can hear it and it is closer than what we previously have stored
                if(distanceToThreat < zombieStateMachine.audioThreat.Distance)
                {
                    zombieStateMachine.audioThreat.Set(AITargetType.Audio, _other, soundPos, distanceToThreat);
                }
            }
            else if (_other.CompareTag("AI_Food") && currentType != AITargetType.Visual_Player && currentType != AITargetType.Visual_Light &&
                zombieStateMachine.Satisfaction <= 0.9f && zombieStateMachine.audioThreat.Type == AITargetType.None)
            {
                //Calculate distance from sound 
                float distanceToThreat = Vector3.Distance(_other.transform.position, zombieStateMachine.SensorPosition);
                if(distanceToThreat < zombieStateMachine.visualThreat.Distance) //if closer than current visual threat
                {
                    //Check is within FOV and within sight
                    if(ColliderIsVisible(_other, out RaycastHit hitInfo, visualRaycastMask))
                    {
                        zombieStateMachine.visualThreat.Set(AITargetType.Visual_Food, _other, _other.transform.position, distanceToThreat);
                    }
                }
            }
        }
    }

    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1)
    {
        hitInfo = new RaycastHit();

        if (zombieStateMachine == null)
            return false;

        Vector3 head = zombieStateMachine.SensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        //Check if threat is within FOV (half, for obvious reasons)
        if(angle > zombieStateMachine.Fov * 0.5f)
            return false;


        //Return all things in the layerMask                                        (sight controls the radius portion)
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, zombieStateMachine.SensorRadius * zombieStateMachine.Sight, layerMask);

        //Find the closest collider that is NOT at the AIs own body part. If it's not the target then the target is obstructed 
        float       closestColliderDistance = float.MaxValue;
        Collider    closestCollider         = null;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance < closestColliderDistance) //if closer than current 
            {
                if(hit.transform.gameObject.layer == bodyPartLayerMask)
                {
                    //if it is not our own body part
                    if(stateMachine != GameSceneManager.Instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                    {
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else
                {
                    //It's not a body part so simply store this as the new closest hit
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        //Return true if the closest collider is the actual object that we are testing (there is line of sight)
        if (closestCollider && closestCollider.gameObject == other.gameObject)
            return true;

        return false;
    }
}
