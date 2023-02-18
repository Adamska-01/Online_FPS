using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim }


// --------------------------------------------------------------------------
// CLASS	:	BodyPartSnapshot
// DESC		:	Used to store information about the position of each body 
//              part when transitioning from a ragdoll
// --------------------------------------------------------------------------
public class BodyPartSnapshot
{
    public Transform transform;

    public Vector3      position;
    public Quaternion   rotation;
    public Quaternion   localtRotation;
}


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
    [SerializeField, Range(0, 100)]         private int     lowerBodyDamage = 0;
    [SerializeField, Range(0, 100)]         private int     upperBodyDamage = 0;
    [SerializeField, Range(0, 100)]         private int     upperBodyThreshold = 30;
    [SerializeField, Range(0, 100)]         private int     limpThreshold   = 30;
    [SerializeField, Range(0, 100)]         private int     crawlThreshold  = 90;
    [SerializeField, Range(0.0f, 1.0f)]     private float   intelligence    = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   satisfaction    = 1.0f;
    [SerializeField, Range(0.0f, 2.0f)]     private float   replenishRate   = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   depletionRate   = 0.1f;
    
    [SerializeField] private float reanimationBlendTime = 1.0f;
    [SerializeField] private float reanimationWaitTime = 3.0f;

    //Private (Managed by the state machine) 
    private int     seeking     = 0;
    private bool    feeding     = false;
    private bool    crawling    = false;
    private int     attackType  = 0;
    private float   speed       = 0;

    //Ragdoll stuff
    private AIBoneControlType boneControlType = AIBoneControlType.Animated;
    private List<BodyPartSnapshot> bodyPartSnapshots = new List<BodyPartSnapshot>();
    private float ragdollEndTime = float.MinValue;
    private Vector3 ragdollHipPosition;
    private Vector3 ragdollFeetPosition;
    private Vector3 ragdollHeadPosition;
    private IEnumerator reanimationCorutine = null;
    private float mechanimTransitionTime = 0.1f;

    // Hashes
    private int speedHash       = Animator.StringToHash("Speed");
    private int seekingHash     = Animator.StringToHash("Seeking");
    private int feedingHash     = Animator.StringToHash("Feeding");
    private int attackHash      = Animator.StringToHash("Attack");
    private int crawlingHash    = Animator.StringToHash("Crawling");
    private int hitTriggerHash  = Animator.StringToHash("Hit");
    private int hitTypeHash     = Animator.StringToHash("Hit_Type");

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
    public bool IsCrawling      { get { return lowerBodyDamage >= crawlThreshold; } }
    public int Seeking          { get { return seeking; } set { seeking = value; } }
    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }


    protected override void Start()
    {
        base.Start();

        //Get all the bones under the hip and add them to the list (Needed for transitioning from a ragdoll state to normal animations)
        if(rootBone != null)
        {
            Transform[] transforms = rootBone.GetComponentsInChildren<Transform>();
            foreach (Transform item in transforms)
            {
                BodyPartSnapshot snapShot = new BodyPartSnapshot();
                snapShot.transform = item;

                bodyPartSnapshots.Add(snapShot);
            }
        }

        UpdateAnimatorDamage();
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

        satisfaction = MathF.Max(0, satisfaction - ((depletionRate * Time.deltaTime) / 100) * Mathf.Pow(speed, 3.0f));
    }


    protected void UpdateAnimatorDamage()
    {
        if(anim != null)
        {
            anim.SetBool(crawlingHash, IsCrawling);
        }
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
            sys.Emit(55);
        }

        //Damage
        float hitStrength = _force.magnitude;
        if (boneControlType == AIBoneControlType.Ragdoll)
        {
            if(_bodyPart != null)
            {
                //Apply hit force 
                if(hitStrength > 1.0f)
                {
                    _bodyPart.AddForce(_force, ForceMode.Impulse);
                }

                //Apply damage on body parts (zombie can ONLY be killed by shooting at the head)
                switch(_bodyPart.tag)
                {
                    case "Head":
                        health = (int)MathF.Max(((float)(health - _damage)), 0.0f);
                        break;
                    case "Upper_Body":
                        upperBodyDamage += _damage;
                        break;
                    case "Lower_Body":
                        lowerBodyDamage += _damage;
                        break;
                }

                //Reflect the stats onto the animator
                UpdateAnimatorDamage();

                if(health > 0)
                {
                    //Only one EdoTensei per time
                    if (reanimationCorutine != null)
                    {
                        StopCoroutine(reanimationCorutine);
                    }

                    //Reanimate zombie
                    reanimationCorutine = EdoTensei();
                    StartCoroutine(reanimationCorutine);
                }
            }

            return;
        }

        //Get local space (relative to this zombie) position of the attacker (player)
        Vector3 attackerLocalPos = transform.InverseTransformPoint(_chrManager.transform.position);
        //Get local space (relative to this zombie) position of the hit
        Vector3 hitLocalPos = transform.InverseTransformPoint(_position);

        bool shouldRagdoll = (hitStrength > 1.0f); //TODO: set with a function
        if (_bodyPart != null)
        {
            //Apply damage on body parts (zombie can ONLY be killed by shooting at the head)
            switch (_bodyPart.tag)
            {
                case "Head":
                    health = (int)MathF.Max(((float)(health - _damage)), 0.0f);
                    if(health == 0)
                    {
                        shouldRagdoll = true;
                    }
                    break;
                case "Upper_Body":
                    upperBodyDamage += _damage;
                    UpdateAnimatorDamage();
                    break;
                case "Lower_Body":
                    lowerBodyDamage += _damage;
                    UpdateAnimatorDamage();
                    shouldRagdoll = true;
                    break;
            }
        }

        if(boneControlType != AIBoneControlType.Animated || IsCrawling || cinematicEnabled || attackerLocalPos.z < 0) //Attack from behind (z < 0)
        {
            shouldRagdoll = true;
        }

        if (!shouldRagdoll)
        {
            float angle = 0.0f;
            if(_hitDir == 0)
            {
                Vector3 vecToHit = (_position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }

            //Hit animation
            int hitType = 0;
            switch (_bodyPart.gameObject.tag)
            {
                case "Head":
                    if(angle < -10 || _hitDir == -1)
                    {
                        hitType = 1;
                    }
                    else if (angle > 10 || _hitDir == 1)
                    {
                        hitType = 3;
                    }
                    else
                    {
                        hitType = 2;
                    }
                    break;
                case "Upper_Body":
                    if (angle < -20 || _hitDir == -1)
                    {
                        hitType = 4;
                    }
                    else if (angle > 20 || _hitDir == 1)
                    {
                        hitType = 6;
                    }
                    else
                    {
                        hitType = 5;
                    }
                    break;
            }

            //Play animation
            if(anim != null)
            {
                anim.SetInteger(hitTypeHash, hitType);
                anim.SetTrigger(hitTriggerHash);
            }

            return;
        }
        else //Ragdoll
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

            //Enable ragdoll
            foreach (Rigidbody body in bodyParts)
            {
                if(body != null)
                {
                    //Enable ragdoll
                    body.isKinematic = false;
                }
            }

            //Add force to body part
            if(hitStrength > 1.0f)
            {
                if(_bodyPart != null)
                {
                    _bodyPart.AddForce(_force, ForceMode.Impulse);
                }
            }

            //Set ragdoll state
            boneControlType = AIBoneControlType.Ragdoll;

            if(health > 0)
            {
                //Only one EdoTensei per time
                if(reanimationCorutine != null)
                {
                    StopCoroutine(reanimationCorutine);
                }

                //Reanimate zombie
                reanimationCorutine = EdoTensei();
                StartCoroutine(reanimationCorutine);
            }
        }
    }

    private IEnumerator EdoTensei()
    {
        yield return null;
    }
}
