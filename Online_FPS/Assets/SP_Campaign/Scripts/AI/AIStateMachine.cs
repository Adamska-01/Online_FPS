using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


//Public enums of the AI System
public enum AIStateType         { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }
public enum AITargetType        { None, WayPoint, Visual_Player, Visual_Light, Visual_Food, Audio }
public enum AITriggerEventType  { Enter, Stay, Exit }
public enum AIBoneAlignmentType { XAxis, YAxis, ZAxis, XAxisInverted, YAxisInverted, ZAxisInverted }


public struct AITarget
{
    private AITargetType    type;       //The type of target
    private Collider        collider;   //Ref to collider 
    private Vector3         position;   //Current position in the world
    private float           distance;   //Distance from player
    private float           time;       //Time passed since last saw


    public AITargetType Type        { get { return type; } }
    public Collider     Collider    { get { return collider; } }
    public Vector3      Position    { get { return position; } }
    public float        Distance    { get { return distance; } set { distance = value; } }
    public float        Time        { get { return time; } }


    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        type        = t;
        collider    = c;
        position    = p;
        distance    = d;
        time        = UnityEngine.Time.time;
    }

    public void Clear()
    {
        type        = AITargetType .None;
        collider    = null;
        position    = Vector3.zero;
        distance    = Mathf.Infinity;
        time        = 0.0f;
    }
}


public abstract class AIStateMachine : MonoBehaviour
{
    public AITarget visualThreat    = new AITarget();
    public AITarget audioThreat     = new AITarget();

    protected AIState           currentState            = null;
    protected Dictionary<AIStateType, AIState> states   = new Dictionary<AIStateType, AIState>();
    protected List<Rigidbody>   bodyParts               = new List<Rigidbody>();
    protected AITarget          target                  = new AITarget();
    protected int               rootPositionRefCount    = 0;
    protected int               rootRotationRefCount    = 0;
    protected int               AI_BodyPartLayer        = -1;
    protected bool              isTargetReached         = false;
    protected bool              cinematicEnabled        = false;

    [SerializeField] protected AIStateType          currentStateType    = AIStateType.Idle;
    [SerializeField] protected AIBoneAlignmentType  rootBoneAlignment   = AIBoneAlignmentType.ZAxis;
    [SerializeField] protected Transform            rootBone            = null;
    [SerializeField] protected SphereCollider       targetTrigger       = null;
    [SerializeField] protected SphereCollider       sensorTrigger       = null;
    [SerializeField] protected AIWaypointNetwork    waypointNetwork     = null;
    [SerializeField] protected bool                 randomPatrol        = false;
    [SerializeField] protected int                  currentWaypoint     = -1;

    [SerializeField, Range(0.0f, 15.0f)] protected float stoppingDistance = 1.0f;

    protected Animator      anim        = null;
    protected NavMeshAgent  navAgent    = null;
    protected Collider      col         = null;

    //Public Properties
    public Animator      Anim       { get { return anim; } }
    public NavMeshAgent  NavAgent   { get { return navAgent; } }
    public bool UseRootPosition     { get { return rootPositionRefCount > 0; } }
    public bool UseRootRotation     { get { return rootRotationRefCount > 0; } }
    public bool InMeleeRange        { get; set; }
    public bool CinematicEnabled    { get { return cinematicEnabled; } set { cinematicEnabled = value; } }
    public bool IsTargetReached     { get { return isTargetReached; } }
    public AITargetType TargetType  { get { return target.Type; } }
    public Vector3 TargetPosition   { get { return target.Position; } }
    public Vector3 SensorPosition
    {
        get 
        { 
            if(sensorTrigger == null)
            {
                return Vector3.zero;
            }
            Vector3 point = sensorTrigger.transform.position;
            //Take parent (global) scale into account 
            point.x += sensorTrigger.center.x * sensorTrigger.transform.lossyScale.x;
            point.y += sensorTrigger.center.y * sensorTrigger.transform.lossyScale.y;
            point.z += sensorTrigger.center.z * sensorTrigger.transform.lossyScale.z;

            return point;
        }
    }
    public float SensorRadius
    {
        get
        {
            if (sensorTrigger == null)
            {
                return 0.0f;
            }
            //Take parent (global) scale into account (only the biggest scale seems affects the radius size)
            return Mathf.Max(sensorTrigger.radius * sensorTrigger.transform.lossyScale.x,
                             sensorTrigger.radius * sensorTrigger.transform.lossyScale.y,
                             sensorTrigger.radius * sensorTrigger.transform.lossyScale.z);
        }
    }
    public int TargetColliderID
    {
        get
        {
            if(target.Collider != null)
                return target.Collider.GetInstanceID();
            else
                return -1;
        }
    }

    protected virtual void Awake()
    {
        //Cache components
        anim = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        col = GetComponent<Collider>();

        //Get body part layer 
        AI_BodyPartLayer = LayerMask.NameToLayer("AI_BodyPart");

        //Register state machines with scene database
        if(GameSceneManager.Instance != null)
        { 
            if(col)
            {
                GameSceneManager.Instance.RegisterAIStateMachine(col.GetInstanceID(), this);
            }
            if (sensorTrigger)
            {
                GameSceneManager.Instance.RegisterAIStateMachine(sensorTrigger.GetInstanceID(), this);
            }
        }

        //Get ragdoll body parts
        if(rootBone != null)
        {
            Rigidbody[] bodies = rootBone.GetComponentsInChildren<Rigidbody>();
            foreach (var bodyPart in bodies)
            {
                if(bodyPart != null && bodyPart.gameObject.layer == AI_BodyPartLayer)
                {
                    bodyParts.Add(bodyPart);
                    //Also register this part in the dictionary (to retrieve state machine when dealing damage)
                    GameSceneManager.Instance.RegisterAIStateMachine(bodyPart.GetInstanceID(), this);
                }
            }

        }
    }

    protected virtual void Start()
    {
        //Store reference to this state machine in the AISensor
        if(sensorTrigger != null)
        {
            AISensor script = sensorTrigger.GetComponent<AISensor>();
            if(script != null)
            {
                script.ParentStateMachine = this;
            }
        }

        //Get all the AI states in this object
        AIState[] allStastes = GetComponents<AIState>();
        foreach (AIState state in allStastes)
        {
            //Add All the states acquired in the dictionary if not already (without duplicates)
            if(state != null && !states.ContainsKey(state.GetStateType()))
            {
                states[state.GetStateType()] = state;
                //Set state machine to each state
                state.SetStateMachine(this);

            }
        }
        
        //Set current state
        if(states.ContainsKey(currentStateType))
        {
            currentState = states[currentStateType];
            currentState.OnEnterState();
        }
        else
        {
            currentState = null;
        }

        //Get all the state machine behaviours, and set ref to this state machine on each one
        if(anim != null)
        {
            AIStateMachineLink[] sml = anim.GetBehaviours<AIStateMachineLink>();
            foreach (AIStateMachineLink item in sml)
            {
                item.StateMachine = this;
            }
        }
    }

    protected virtual void Update()
    {
        if (currentState == null)
            return;

        AIStateType newStateType = currentState.OnUpdate();
        if(newStateType != currentStateType)
        {
            AIState newState;

            //Transition to new state
            if (states.TryGetValue(newStateType, out newState))
            {
                currentState.OnExitState();
                newState.OnEnterState();

                currentState = newState;
                currentStateType = newStateType;
            }
            else if (states.TryGetValue(AIStateType.Idle, out newState))
            {
                currentState.OnExitState();
                newState.OnEnterState();

                currentState = newState;
                currentStateType = AIStateType.Idle;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        //Clear all targets
        visualThreat.Clear();
        audioThreat.Clear();

        //Set current target distance 
        if(target.Type != AITargetType.None)
        {
            target.Distance = Vector3.Distance(transform.position, target.Position);
        }

        //Handle the case where a target could get destroyed while this is true
        //and it does not get the OnTriggerExit notification
        isTargetReached = false;
    }


    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        target.Set(t, c, p, d);

        //Reposition and resize target trigger
        if(targetTrigger != null)
        {
            targetTrigger.radius = stoppingDistance;
            targetTrigger.transform.position = target.Position;
            targetTrigger.enabled = true;
        }
    }
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        target.Set(t, c, p, d);

        //Reposition and resize target trigger
        if (targetTrigger != null)
        {
            targetTrigger.radius = s;
            targetTrigger.transform.position = target.Position;
            targetTrigger.enabled = true;
        }
    }
    public void SetTarget(AITarget t)
    {
        target = t;

        //Reposition and resize target trigger
        if (targetTrigger != null)
        {
            targetTrigger.radius = stoppingDistance;
            targetTrigger.transform.position = target.Position;
            targetTrigger.enabled = true;
        }
    }

    public void ClearTarget()
    {
        target.Clear();

        //Set trigger collider to false
        if (targetTrigger != null)
        {
            targetTrigger.enabled = false;
        }
    }


    protected virtual void OnTriggerEnter(Collider other)
    {
        if (targetTrigger == null || other != targetTrigger)
            return; //Return if not our target trigger 

        isTargetReached = true;
    
        //Notify child state
        if(currentState != null)
        {
            currentState.OnDestinationReached(true);
        }
    }

    protected void OnTriggerStay(Collider other)
    {
        if (targetTrigger == null || other != targetTrigger)
            return; //Return if not our target trigger 

        isTargetReached = true;        
    }

    protected void OnTriggerExit(Collider other)
    {
        if (targetTrigger == null || other != targetTrigger)
            return; //Return if not our target trigger 

        isTargetReached = false;

        //Notify child state
        if (currentState != null)
        {
            currentState.OnDestinationReached(false);
        }
    }

    public virtual void OnTriggerEvent(AITriggerEventType _eventType, Collider _other) 
    {
        if (currentState != null)
        {
            currentState.OnTriggerEvent(_eventType, _other);
        }
    }

    protected virtual void OnAnimatorMove()
    {
        if (currentState != null)
        {
            currentState.OnAnimatorUpdated();
        }
    }

    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (currentState != null)
        {
            currentState.OnAnimatorIKUpdated();
        }
    }

    public void NavAgentControl(bool _positionUpdate, bool _rotationUpdate)
    {
        if (navAgent != null)
        {
            navAgent.updatePosition = _positionUpdate;
            navAgent.updateRotation = _rotationUpdate;
        }
    }

    public void AddRootMotionRequest(int _rootPosition, int _rootRotation)
    {
        rootPositionRefCount += _rootPosition;
        rootRotationRefCount += _rootRotation;
    }

    private void NextWaypoint()
    {
        //Increase current waypoint with wrap-around to zero (or choose a random waypoint)
        if (randomPatrol && waypointNetwork.waypoints.Count > 1)
        {
            int oldWaypoint = currentWaypoint;
            while (currentWaypoint == oldWaypoint)
            {
                currentWaypoint = Random.Range(0, waypointNetwork.waypoints.Count);
            }
        }
        else
        {
            currentWaypoint = currentWaypoint == waypointNetwork.waypoints.Count - 1 ? 0 : currentWaypoint + 1;
        }        
    }

    public Vector3 GetWaypointPosition(bool incrementWaypoint)
    {
        if (currentWaypoint == -1) //First time this gets called
        {
            if(randomPatrol)
            {
                currentWaypoint = Random.Range(0, waypointNetwork.waypoints.Count);
            }
            else
            {
                currentWaypoint = 0;
            }
        }
        else if(incrementWaypoint)
        {
            NextWaypoint();
        }

        //Fetch new waypoint from the waypoint list
        if (waypointNetwork.waypoints[currentWaypoint] != null)
        {
            Transform newWaypoint = waypointNetwork.waypoints[currentWaypoint];

            //Set new target position        
            SetTarget(AITargetType.WayPoint, null, newWaypoint.position, Vector3.Distance(transform.position, newWaypoint.position));
        
            return newWaypoint.position;
        }
        
        return Vector3.zero;
    }

    public void SetStateOverride(AIStateType state)
    {
        if(state != currentStateType && states.ContainsKey(state))
        {
            if(currentState != null)
            {
                currentState.OnExitState();
            }

            currentState = states[state];
            currentStateType = state;
            currentState.OnEnterState();
        }
    }

    public virtual void TakeDamage(Vector3 _position, Vector3 _force, int _damage, Rigidbody _bodyPart, CharacterManager _chrManager, int _hitDir = 0)
    {     
        //Implemeted in subclasses
    }
}
