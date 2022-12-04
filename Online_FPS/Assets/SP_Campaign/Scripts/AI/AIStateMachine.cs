using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType         { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }
public enum AITargetType        { None, WayPoint, Visual_Player, Visual_Light, Visual_Food, Audio }
public enum AITriggerEventType  { Enter, Stay, Exit }

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

    protected AIState currentState = null;
    protected Dictionary<AIStateType, AIState> states = new Dictionary<AIStateType, AIState>();
    protected AITarget target = new AITarget();

    [SerializeField] protected AIStateType      currentStateType    = AIStateType.Idle;
    [SerializeField] protected SphereCollider   targetTrigger       = null;
    [SerializeField] protected SphereCollider   sensorTrigger       = null;

    [SerializeField, Range(0.0f, 15.0f)] protected float stoppingDistance = 1.0f;

    protected Animator      anim        = null;
    protected NavMeshAgent  navAgent    = null;
    protected Collider      collider    = null;

    protected Animator      Anim        { get { return anim; } }
    protected NavMeshAgent  NavAgent    { get { return navAgent; } }


    protected virtual void Awake()
    {
        //Cache components
        anim = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        collider = GetComponent<Collider>();
    }

    protected virtual void Start()
    {
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
}
