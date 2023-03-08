using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	AIZombieState_Attack1
// DESC		:	A Zombie state that allows the zombie to attack
// --------------------------------------------------------------------------
public class AIZombieState_Attack1 : AIZombieState
{
    //Inspector-Assigned
    [SerializeField, Range(0.0f, 10.0f)] private float speed = 0.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float lookAtWeight = 0.7f;
    [SerializeField, Range(0.0f, 90.0f)] private float lookAtAngleThreshold = 15.0f;
    [SerializeField] private float slerpSpeed = 5.0f;
    [SerializeField] private float stoppingDistance = 1.0f;

    //Private variables 
    private float currentLookAtWeight = 0.0f;


    public override void OnEnterState()
    {
        Debug.Log("Entering Attack State");
        base.OnEnterState();

        if (zombieStateMachine == null)
            return;

        //Set zombie stats
        zombieStateMachine.Feeding = false;
        zombieStateMachine.Seeking = 0;
        zombieStateMachine.Speed = speed;
        zombieStateMachine.AttackType = Random.Range(1, 100); //Attack animation
        //Update position but not rotation
        zombieStateMachine.NavAgentControl(true, false);

        currentLookAtWeight = 0.0f; //IK (Head)
    }

    public override void OnExitState()
    {
        zombieStateMachine.AttackType = 0; //Go back to default state 
    }

    public override AIStateType GetStateType()
    {
        return AIStateType.Attack;
    }

    public override AIStateType OnUpdate()
    {
        Vector3 targetPos;
        Quaternion newRot;

        //Stop zombie from walking/running when too close to attack 
        if (Vector3.Distance(zombieStateMachine.transform.position, zombieStateMachine.TargetPosition) < stoppingDistance)
            zombieStateMachine.Speed = 0;
        else
            zombieStateMachine.Speed = speed;
        Debug.Log(stateMachine.visualThreat.Type);
        //Check if the player is still visible 
        if(zombieStateMachine.visualThreat.Type == AITargetType.Visual_Player)
        {
            //Set new target 
            zombieStateMachine.SetTarget(stateMachine.visualThreat);

            //if we are not in melee range then go back to pursuit mode 
            if(!zombieStateMachine.InMeleeRange)
            {
                return AIStateType.Pursuit;
            }

            if(!zombieStateMachine.UseRootRotation)
            {
                //keep zombie facing the player 
                targetPos = zombieStateMachine.TargetPosition;
                targetPos.y = zombieStateMachine.transform.position.y;
                newRot = Quaternion.LookRotation(targetPos - zombieStateMachine.transform.position);
                zombieStateMachine.transform.rotation = Quaternion.Slerp(zombieStateMachine.transform.rotation, newRot, Time.deltaTime * slerpSpeed);
            }

            zombieStateMachine.AttackType = Random.Range(1, 100);
            return AIStateType.Attack;
        }

        //Player is outside zombie's FOV
        if(!zombieStateMachine.UseRootRotation)
        {
            //Let zombie face the last known position
            targetPos = zombieStateMachine.TargetPosition;
            targetPos.y = zombieStateMachine.transform.position.y;
            newRot = Quaternion.LookRotation(targetPos - zombieStateMachine.transform.position);
            zombieStateMachine.transform.rotation = newRot;
        }

        return AIStateType.Alerted;
    }

    public override void OnAnimatorIKUpdated()
    {
        if (zombieStateMachine == null)
            return;

        //Make the head look at the player if less than the set threshold 
        if(Vector3.Angle(zombieStateMachine.transform.forward, zombieStateMachine.TargetPosition - zombieStateMachine.transform.position) < lookAtAngleThreshold)
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
