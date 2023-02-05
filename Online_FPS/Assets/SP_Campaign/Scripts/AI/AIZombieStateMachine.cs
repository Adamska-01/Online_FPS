using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --------------------------------------------------------------------------
// CLASS	:	AIZombieStateMachine
// DESC		:	State Machine used by zombie characters
// --------------------------------------------------------------------------
public class AIZombieStateMachine : AIStateMachine
{
    //Inspector Assigned 
    [Header("Zombie Characteristics")]
    [SerializeField, Range(10.0f, 360.0f)]  private float   fov             = 50.0f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   sight           = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   hearing         = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   aggression      = 0.5f;
    [SerializeField, Range(0, 100)]         private int     health          = 100;
    [SerializeField, Range(0.0f, 1.0f)]     private float   intelligence    = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   satisfaction    = 1.0f;
    [SerializeField, Range(0.0f, 2.0f)]     private float   replenishRate   = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   depletionhRate  = 0.1f;

    //Private (Managed by the state machine) 
    private int     seeking     = 0;
    private bool    feeding     = false;
    private bool    crawling    = false;
    private int     attackType  = 0;
    private float   speed       = 0;

    // Hashes
    private int speedHash   = Animator.StringToHash("Speed");
    private int seekingHash = Animator.StringToHash("Seeking");
    private int feedingHash = Animator.StringToHash("Feeding");
    private int attackHash  = Animator.StringToHash("Attack");

    // Public Properties
    public float ReplenishRate  { get { return replenishRate; } }
    public float Fov            { get { return fov; } }
    public float Hearing        { get { return hearing; } }
    public float Sight          { get { return sight; } }
    public bool Crawling        { get { return crawling; } }
    public float Intelligence   { get { return intelligence; } }
    public float Satisfaction   { get { return satisfaction; } set { satisfaction = value; } }
    public float Aggression     { get { return aggression; } set { aggression = value; } }
    public int Health           { get { return health; } set { health = value; } }
    public int AttackType       { get { return attackType; } set { attackType = value; } }
    public bool Feeding         { get { return feeding; } set { feeding = value; } }
    public int Seeking          { get { return seeking; } set { seeking = value; } }
    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }

    protected override void Update()
    {
        base.Update();

        //Set animator parameters
        if (anim != null)
        {
            anim.SetFloat(speedHash, speed);
            anim.SetInteger(seekingHash, seeking);
            anim.SetBool(feedingHash, feeding);
            anim.SetInteger(attackHash, attackType);
        }

        satisfaction = MathF.Max(0, satisfaction - ((depletionhRate * Time.deltaTime) / 100) * Mathf.Pow(speed, 3.0f));
    }


    public override void TakeDamage(Vector3 _position, Vector3 _force, int _damage, Rigidbody _bodyPart, CharacterManager _chrManager, int _hitDir = 0)
    {
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.BloodParticles != null)
        {
            //Add blood particles 
            ParticleSystem sys = GameSceneManager.Instance.BloodParticles;
            sys.transform.position = _position;
            //Set simulation World space
            var settings = sys.main;
            settings.simulationSpace = ParticleSystemSimulationSpace.World;
            //Emission
            sys.Emit(60);
        }

        health -= _damage;

        float hitStrength = _force.magnitude;
        bool shouldRagdoll = (hitStrength > 1.0f); //TODO: set with a function
        if(health <= 0)
        {
            shouldRagdoll = true;
        }

        if(shouldRagdoll)
        {
            //"Clear" the state the AI is currently in 
            if(currentState)
            {
                currentState.OnExitState();
                currentState = null;
                currentStateType = AIStateType.None;
            }

            //Disable anything that conflicts with the ragdoll
            if(navAgent)    navAgent.enabled = false;
            if(anim)        anim.enabled = false;
            if(col)         col.enabled = false;

            InMeleeRange = false;

            foreach (Rigidbody body in bodyParts)
            {
                if(body != null)
                {
                    //Enable ragdoll
                    body.isKinematic = false;
                }
            }

            _bodyPart.AddForce(_force, ForceMode.Impulse);
        }
    }
}
