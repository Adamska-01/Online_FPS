using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] string animParameter = "";
    [SerializeField] int bloodParticlesBurstAmount = 10;
    [SerializeField] float damageAmount = 1.0f;
    [SerializeField] bool doDamageSound = true;
    [SerializeField] bool doPainSound = true;

    //Private fields 
    private AIStateMachine stateMachine = null;
    private Animator anim = null;
    private GameSceneManager gameSceneManager = null;
    private int parameterHash = -1;
    private bool firstContact = false;


    private void Start()
    {
        stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();
        if(stateMachine != null)
        {
            anim = stateMachine.Anim;
        }

        parameterHash = Animator.StringToHash(animParameter);

        gameSceneManager = GameSceneManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (anim == null)
            return;

        if (other.gameObject.CompareTag("Player") && anim.GetFloat(parameterHash) > 0.9f)
        {
            firstContact = true;
        }
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

                var settings = system.main;
                settings.simulationSpace = ParticleSystemSimulationSpace.World;

                system.Emit(bloodParticlesBurstAmount);
            }
            
            //Use character manager to apply damage
            if(gameSceneManager != null)
            {
                Player_Info info = gameSceneManager.GetPlayerInfo(other.GetInstanceID());
                if(info != null && info.characterManager != null)
                {
                    info.characterManager.TakeDamage(damageAmount, doDamageSound && firstContact, doPainSound);
                }
            }

            firstContact = false;
        }
    }
}
