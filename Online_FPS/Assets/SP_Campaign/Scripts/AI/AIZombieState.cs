using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIZombieState : AIState
{
    protected int playerLayerMask = -1;
    protected int bodyPartLayerMask = -1;

    private void Awake()
    {
        playerLayerMask = LayerMask.GetMask("Player", "AI_Body_Part") + 1; // + Default layer
        bodyPartLayerMask = LayerMask.GetMask("AI_Body_Part");
    }

    public override void OnTriggerEvent(AITriggerEventType _eventType, Collider _other)
    {
        if (stateMachine == null)
            return;

        if(_eventType != AITriggerEventType.Exit)
        {
            AITargetType currentType = stateMachine.visualThreat.Type;

            if(_other.CompareTag("Player")) //Player has the highest priority
            {
                //Might be 2 players (?) - Future multiplayer support (?)
                float distance = Vector3.Distance(stateMachine.SensorPosition, _other.transform.position);
                if(currentType != AITargetType.Visual_Player || 
                  (currentType == AITargetType.Visual_Player && distance < stateMachine.visualThreat.Distance))
                {
                    if (ColliderIsVisible(_other, out RaycastHit raycastHit, playerLayerMask))
                    {
                        //Target is close and within FOV so store as the current most dangerous threat
                        stateMachine.visualThreat.Set(AITargetType.Visual_Player, _other, _other.transform.position, distance);
                    }
                }
            }
        }
    }

    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1)
    {
        hitInfo = new RaycastHit();

        if (stateMachine == null || stateMachine.GetType() != typeof(AIZombieStateMachine))
            return false;

        AIZombieStateMachine zombieMachine = ((AIZombieStateMachine)stateMachine);

        Vector3 head = stateMachine.SensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        //Check if threat is within FOV (half, for obvious reasons)
        if(angle > zombieMachine.Fov * 0.5f)
            return false;


        //Return all things in the layerMask                                        (sight controls the radius portion)
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, stateMachine.SensorRadius * zombieMachine.sight, layerMask);

        //Find the closest collider that is NOT at the AIs own body part. If it's not the target then the target is obstructed 
        float       closestColliderDistance = float.MaxValue;
        Collider    closestCollider         = null;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance < closestColliderDistance)
            {
                if(hit.transform.gameObject.layer == bodyPartLayerMask)
                {
                    if(stateMachine != GameSceneManager.Instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                    {
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else
                {
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
