using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim }
public enum AIScreamPosition  { Entity, Player }


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
    [SerializeField, Range(0.0f, 1.0f)]     private float   screamChance    = 1.0f;
    [SerializeField, Range(0.0f, 50.0f)]    private float   screamRadius    = 20.0f;

    [SerializeField] private AIScreamPosition screamPosition = AIScreamPosition.Entity;
    [SerializeField] private AISoundEmitter screamPrefab = null;
    
    [SerializeField] private float reanimationBlendTime = 1.0f;
    [SerializeField] private float reanimationWaitTime = 3.0f;
    [SerializeField] private LayerMask geometryLayer = 0;

    //Private (Managed by the state machine) 
    private int     seeking     = 0;
    private bool    feeding     = false;
    private bool    crawling    = false;
    private int     attackType  = 0;
    private float   speed       = 0.0f;
    private float   isScreaming = 0.0f;

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
    private int speedHash               = Animator.StringToHash("Speed");
    private int seekingHash             = Animator.StringToHash("Seeking");
    private int feedingHash             = Animator.StringToHash("Feeding");
    private int attackHash              = Animator.StringToHash("Attack");
    private int crawlingHash            = Animator.StringToHash("Crawling");
    private int screamHash              = Animator.StringToHash("Scream");
    private int screamingHash           = Animator.StringToHash("Screaming");
    private int hitTriggerHash          = Animator.StringToHash("Hit");
    private int hitTypeHash             = Animator.StringToHash("Hit_Type");
    private int reanimateFromFrontHash  = Animator.StringToHash("ReanimateFromFront");
    private int reanimateFromBackHash   = Animator.StringToHash("ReanimateFromBack");
    private int upperBodyDamageHash     = Animator.StringToHash("UpperBodyDamage");
    private int lowerBodyDamageHash     = Animator.StringToHash("LowerBodyDamage");
    private int stateHash               = Animator.StringToHash("State");

    private int upperBodyLayer = -1;
    private int lowerBodyLayer = -1;

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
    public bool IsScreaming     { get { return isScreaming > 0.1f; } }
    public float Speed          { get { return speed; } set { speed = value; } }
    public float ScreamChance   { get { return screamChance; } }
    public int Seeking          { get { return seeking; } set { seeking = value; } }



    protected override void Start()
    {
        base.Start();

        if (anim != null)
        {
            //Cache animation layer indexes
            upperBodyLayer = anim.GetLayerIndex("Upper_Body");
            lowerBodyLayer = anim.GetLayerIndex("Lower_Body");
        }

        //Get all the bones under the hip and add them to the list (Needed for transitioning from a ragdoll state to normal animations)
        if (rootBone != null)
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
            anim.SetInteger(stateHash, (int)currentStateType);

            isScreaming = IsLayerActive("Cinematic") ? 0.0f : anim.GetFloat(screamingHash);
        }

        satisfaction = MathF.Max(0, satisfaction - ((depletionRate * Time.deltaTime) / 100) * Mathf.Pow(speed, 3.0f));
    }

    protected void LateUpdate()
    {
        if(boneControlType == AIBoneControlType.RagdollToAnim)
        {
            //Position the root parent object back to the ragdolled zombie 
            if (Time.time <= ragdollEndTime + mechanimTransitionTime)
            {
                Vector3 animatedToRagdoll = ragdollHipPosition - rootBone.position;
                Vector3 newRootPosition = transform.position + animatedToRagdoll;

                //Get the closest hit point (floor)
                RaycastHit[] hits = Physics.RaycastAll(newRootPosition + Vector3.up * 0.25f, Vector3.down, float.MaxValue, geometryLayer);
                newRootPosition.y = float.MinValue;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.IsChildOf(transform))
                        continue;

                    newRootPosition.y = Math.Max(hit.point.y, newRootPosition.y);
                }

                //Get the closest valid nav mesh point and set it to the root object this zombie is contained in
                Vector3 baseOffset = Vector3.zero;
                if (NavAgent != null)
                {
                    baseOffset.y = NavAgent.baseOffset;
                }
                if(NavMesh.SamplePosition(newRootPosition, out NavMeshHit navMeshHit, 25.0f, NavMesh.AllAreas))
                {
                    transform.position = navMeshHit.position + baseOffset;
                }
                else
                {
                    transform.position = newRootPosition + baseOffset;
                }

                //Rotate the root object towards the direction the zombie is facing 
                Vector3 ragdollDirection = ragdollHeadPosition - ragdollFeetPosition;
                ragdollDirection.y = 0.0f; //rotate only on the y

                Vector3 meanFeetposition = 0.5f * (anim.GetBoneTransform(HumanBodyBones.LeftFoot).position + anim.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = anim.GetBoneTransform(HumanBodyBones.Head).position - meanFeetposition;
                animatedDirection.y = 0.0f; //rotate only on the y

                //Apply rotation (face ragdoll direction)
                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdollDirection.normalized);
            }

            //T lerp value
            float blendAmount = Math.Clamp(((Time.time - ragdollEndTime - mechanimTransitionTime) / reanimationBlendTime), 0.0f, 1.0f);
            //Perform the interpolation between the snapshot positions and the current animation positions
            foreach (BodyPartSnapshot snapShot in bodyPartSnapshots)
            {
                //lerp pos/rot of the hip 
                if(snapShot.transform == rootBone)
                {
                    snapShot.transform.position = Vector3.Lerp(snapShot.position, snapShot.transform.position, blendAmount);
                    snapShot.transform.rotation = Quaternion.Slerp(snapShot.rotation, snapShot.transform.rotation, blendAmount);
                }
                else //This might be mind blowing, but animations don't actually move the bones under the hip, it just rotates them 0.0
                {
                    snapShot.transform.localRotation = Quaternion.Slerp(snapShot.localtRotation, snapShot.transform.localRotation, blendAmount);
                }
            }

            //End reanimation
            if(blendAmount >= 1.0f)
            {
                boneControlType = AIBoneControlType.Animated;
             
                //Enable components back again
                if (navAgent) navAgent.enabled = true;
                if (col) col.enabled = true;

                //Set AI state to alerted
                if(states.TryGetValue(AIStateType.Alerted, out AIState newState))
                {
                    if(currentState != null) //Safe check (this should never be executed)
                    {
                        currentState.OnExitState();
                    }

                    newState.OnEnterState();
                    currentState = newState;
                    currentStateType = AIStateType.Alerted;
                }
            }
        }
    }


    protected void UpdateAnimatorDamage()
    {
        if (anim != null)
        {
            if(lowerBodyLayer != -1) //Set layer weights 
            {
                anim.SetLayerWeight(lowerBodyLayer, (lowerBodyDamage > limpThreshold && lowerBodyDamage < crawlThreshold) ? 1.0f : 0.0f);
            }
            if (upperBodyLayer != -1) //Set layer weights 
            {
                anim.SetLayerWeight(upperBodyLayer, (upperBodyDamage > upperBodyThreshold && lowerBodyDamage < crawlThreshold) ? 1.0f : 0.0f);
            }

            //Set animator parameters
            anim.SetBool(crawlingHash, IsCrawling);
            anim.SetInteger(upperBodyDamageHash, upperBodyDamage);
            anim.SetInteger(lowerBodyDamageHash, lowerBodyDamage);

            //Set lower/upper body animation layer active/unactive
            if(lowerBodyDamage > limpThreshold && lowerBodyDamage < crawlThreshold)
            {
                SetLayerActive("Lower_Body", true);
            }
            else
            {
                SetLayerActive("Lower_Body", false);
            }
            if (upperBodyDamage > upperBodyThreshold && lowerBodyDamage < crawlThreshold)
            {
                SetLayerActive("Upper_Body", true);
            }
            else
            {
                SetLayerActive("Upper_Body", false);
            }
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

        if(boneControlType != AIBoneControlType.Animated || IsCrawling || IsLayerActive("Cinematic") || attackerLocalPos.z < 0) //Attack from behind (z < 0)
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

            //Mute audio while ragdoll is happening
            if(layeredAudioSource != null)
            {
                layeredAudioSource.Mute(true);
            }

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

    protected IEnumerator EdoTensei()
    {
        if (boneControlType != AIBoneControlType.Ragdoll || anim == null)
            yield break;
        
        //Wait some time before reanimating 
        yield return new WaitForSeconds(reanimationWaitTime);

        //Record time at this point 
        ragdollEndTime = Time.time;

        //Give control to the animator (disable ragdoll)
        foreach (var item in bodyParts)
        {
            item.isKinematic = true;
        }

        boneControlType = AIBoneControlType.RagdollToAnim;

        foreach (BodyPartSnapshot snapShot in bodyPartSnapshots)
        {
            //Record a snapshot of the bone pos/rot
            snapShot.position = snapShot.transform.position;
            snapShot.rotation = snapShot.transform.rotation;
            snapShot.localtRotation = snapShot.transform.localRotation;
        }

        //Record Head - feet (mid-point) - 
        ragdollHeadPosition = anim.GetBoneTransform(HumanBodyBones.Head).position;
        ragdollFeetPosition = (anim.GetBoneTransform(HumanBodyBones.LeftFoot).position + anim.GetBoneTransform(HumanBodyBones.RightFoot).position) * 0.5f;
        ragdollHipPosition = rootBone.position;

        //Enable Animator
        anim.enabled = true;

        //Check the alignment of the hip bone (to determine the "forward")
        if(rootBone != null)
        {
            float forwardTest;
            switch (rootBoneAlignment)
            {
                case AIBoneAlignmentType.XAxis:
                    forwardTest = rootBone.right.y;
                    break;
                case AIBoneAlignmentType.YAxis:
                    forwardTest = rootBone.up.y;
                    break;
                case AIBoneAlignmentType.ZAxis:
                    forwardTest = rootBone.forward.y;
                    break;
                case AIBoneAlignmentType.XAxisInverted:
                    forwardTest = -rootBone.right.y;
                    break;
                case AIBoneAlignmentType.YAxisInverted:
                    forwardTest = -rootBone.up.y;
                    break;
                case AIBoneAlignmentType.ZAxisInverted:
                    forwardTest = -rootBone.forward.y;
                    break;
                default:
                    forwardTest = rootBone.forward.y;
                    break;
            }

            if (forwardTest >= 0)
            {
                anim.SetTrigger(reanimateFromBackHash);
            }
            else
            {
                anim.SetTrigger(reanimateFromFrontHash);
            }
        }
    }

    public bool Scream()
    {
        if (IsScreaming) //We are already screaming, no need to scream again 
            return true;

        if (anim == null || IsLayerActive("Cinematic") || screamPrefab == null)
            return false;

        anim.SetTrigger(screamHash); //Play scream animation

        //Instantiate scream emitter
        Vector3 spawnPos = screamPosition == AIScreamPosition.Entity ? transform.position : visualThreat.Position;
        AISoundEmitter screamEmitter = Instantiate(screamPrefab, spawnPos, Quaternion.identity) as AISoundEmitter;

        //Set scream emitter radius (decreases over time)
        if(screamEmitter != null)
        {
            screamEmitter.SetRadius(screamRadius);
        }

        return true;
    }

    
}      
