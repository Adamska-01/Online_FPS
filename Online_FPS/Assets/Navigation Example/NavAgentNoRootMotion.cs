using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentNoRootMotion : MonoBehaviour
{
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;

    IEnumerator currentCoroutine = null;

    public bool hasPath = false;
    public bool pathPending = false;
    public bool pathStale = false;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve jumpCurve = new AnimationCurve();

    private NavMeshAgent navAgent = null;
    private Animator animator = null;
    private float originalMaxSpeed = 0.0f;
    //Animation parameters hash 
    int horizontalHash;
    int verticalHash;
    int turnOnSpotHash;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (navAgent != null)
            originalMaxSpeed = navAgent.speed;

        //Set animation parameters
        horizontalHash = Animator.StringToHash("Horizontal");
        verticalHash = Animator.StringToHash("Vertical");
        turnOnSpotHash = Animator.StringToHash("TurnOnSpot");

        //Turn off auto-update (rotation and position)
        //navAgent.updatePosition = false;
        //navAgent.updateRotation = false;

        if (waypointNetwork == null)
            return;

        SetNextDestination(false);
    }


    void Update()
    {
        //Nav agent state
        hasPath = navAgent.hasPath;
        pathPending = navAgent.pathPending;
        pathStale = navAgent.isPathStale;
        pathStatus = navAgent.pathStatus;

        int turnOnSpot;

        Vector3 cross = Vector3.Cross(transform.forward, navAgent.desiredVelocity.normalized);
        float horizontal = cross.y < 0.0f ? -cross.magnitude : cross.magnitude;
        horizontal = Mathf.Clamp(horizontal * 2.32f, -2.32f, 2.32f); //3.32 is the animations horizontal param range


        if(navAgent.desiredVelocity.magnitude < 3.0f && Vector3.Angle(transform.forward, navAgent.desiredVelocity) > 20.0f)
        {
            navAgent.speed = 0.1f; //Stop agent (but still have sterring info)
            turnOnSpot = (int)Mathf.Sign(horizontal);
        }
        else
        {
            navAgent.speed = originalMaxSpeed; //Set original speed
            turnOnSpot = 0;
        }


        //Set animation
        animator.SetFloat(horizontalHash, horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat(verticalHash, navAgent.desiredVelocity.magnitude, 0.1f, Time.deltaTime);
        animator.SetInteger(turnOnSpotHash, turnOnSpot);
        
        //Approachinng an off mesh link
        //if (navAgent.isOnOffMeshLink && currentCoroutine == null)
        //{
        //    currentCoroutine = Jump(1.0f);
        //    StartCoroutine(currentCoroutine);
        //    return;
        //}

        //If we do not have a path and one isn't pending then set the next
        //waypoint as the target, otherwise if the path is stale, regenerate path
        if ((navAgent.remainingDistance <= navAgent.stoppingDistance && !pathPending) || (pathStatus == NavMeshPathStatus.PathInvalid /*|| pathStatus == NavMeshPathStatus.PathPartial*/))
            SetNextDestination(true);
        else if (pathStale)
            SetNextDestination(false); //Recalculate
    }


    void SetNextDestination(bool _increment)
    {
        // If no network return
        if (!waypointNetwork) return;

        // Calculatehow much the current waypoint index needs to be incremented
        int incStep = _increment ? 1 : 0;

        // Calculate index of next waypoint factoring in the increment with wrap-around and fetch waypoint 
        int nextWaypoint = (currentIndex + incStep >= waypointNetwork.waypoints.Count) ? 0 : currentIndex + incStep;
        Transform nextWaypointTransform = waypointNetwork.waypoints[nextWaypoint];

        // Assuming we have a valid waypoint transform
        if (nextWaypointTransform != null)
        {
            // Update the current waypoint index, assign its position as the NavMeshAgents
            // Destination and then return
            currentIndex = nextWaypoint;
            navAgent.destination = nextWaypointTransform.position;
            return;
        }

        // We did not find a valid waypoint in the list for this iteration
        currentIndex++;
    }

    IEnumerator Jump(float _duration)
    {
        //Retrieve off mesh data
        OffMeshLinkData data = navAgent.currentOffMeshLinkData;

        //Easier to set startPos to nav agent current position
        Vector3 startPos = navAgent.transform.position;
        //take nav agent offset into account (only Y)
        Vector3 endPos = data.endPos + (navAgent.baseOffset * Vector3.up);

        float time = 0.0f;

        while (time <= _duration)
        {
            //Lerp from point A to point B
            float t = time / _duration;
            navAgent.transform.position = Vector3.Lerp(startPos, endPos, t) + jumpCurve.Evaluate(t) * Vector3.up;

            time += Time.deltaTime;

            yield return null;
        }

        navAgent.CompleteOffMeshLink();
        currentCoroutine = null;
    }
}
