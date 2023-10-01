using UnityEngine;


public class TimedStateTransitionSMB : StateMachineBehaviour
{
    //Inspector-Assigned 
    [Tooltip("How long before the forced transition occours.")]
    public float idleTimeout = 10.0f;

    [Tooltip("How long before the forced transition occours.")]
    public float transitionTime = 0.25f;

    [Tooltip("How long before the forced transition occours.")]
    public string stateName = "Empty State";


    //Internals 
    private float timer = 0.0f;
    private int stateHash = -1;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Reset timer and cache the state hash
        timer = 0.0f;
        stateHash = Animator.StringToHash(stateName);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;
        if(timer > idleTimeout)
        {
            timer = float.MinValue; //Safe-Assignment

            //Force-Transition to the state
            animator.CrossFade(stateHash, transitionTime);
        }
    }
}
