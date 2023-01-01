using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] string animParameter = "";
    [SerializeField] int bloodParticlesBurstAmount = 10;

    //Private fields 
    private AIStateMachine stateMachine = null;
    private Animator anim = null;
    private int parameterHash = -1;


    private void Start()
    {
        stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();
        if(stateMachine != null)
        {
            anim = stateMachine.Anim;
        }

        parameterHash = Animator.StringToHash(animParameter);
    }


    private void OnTriggerStay(Collider other)
    {
        if (anim == null)
            return;


        if(other.gameObject.CompareTag("Player") && anim.GetFloat(parameterHash) > 0.9f)
        {
            if(GameSceneManager.Instance != null && GameSceneManager.Instance.BloodParticles != null)
            {
                ParticleSystem system = GameSceneManager.Instance.BloodParticles;

                system.transform.position = transform.position;
                system.transform.rotation = Camera.main.transform.rotation;

                var simSpace = system.main.simulationSpace;
                simSpace = ParticleSystemSimulationSpace.World;

                system.Emit(bloodParticlesBurstAmount);
            }
            Debug.Log("Player being damaged");
        }
    }
}
