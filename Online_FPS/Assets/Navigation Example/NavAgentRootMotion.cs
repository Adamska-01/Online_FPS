using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentRootMotion : MonoBehaviour
{
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;

    IEnumerator currentCoroutine = null;

    public bool hasPath = false;
    public bool pathPending = false;
    public bool pathStale = false;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve jumpCurve = new AnimationCurve();
    public bool mixedMode = true;

    private NavMeshAgent navAgent = null;
    private Animator animator = null;
    private float smoothAngle = 0.0f;
    //Animation parameters hash 
    int angleHash;
    int speedHash;
    int turnOnSpotHash;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        //Set animation parameters
        angleHash = Animator.StringToHash("Angle");
        speedHash = Animator.StringToHash("Speed");
        turnOnSpotHash = Animator.StringToHash("TurnOnSpot");

        //Turn off auto-update (rotation and position)
        //navAgent.updatePosition = false;
        navAgent.updateRotation = false;

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

        Vector3 localDesiredVelocity = transform.InverseTransformVector(navAgent.desiredVelocity);
        float angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg;
        smoothAngle = Mathf.MoveTowardsAngle(smoothAngle, angle, 80.0f * Time.deltaTime); // no more than 80 degrees in a second

        float speed = localDesiredVelocity.z;

        if(navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon)
        {
            if(!mixedMode || (mixedMode && Mathf.Abs(angle) < 80.0f && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion")))
            {
                Quaternion lookRotation = Quaternion.LookRotation(navAgent.desiredVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5.0f * Time.deltaTime);
            }
        }    


        //Set animation
        animator.SetFloat(angleHash, smoothAngle);
        animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);

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

    private void OnAnimatorMove()
    {
        //Set agent rotation and velocity from the animation root
        if(mixedMode && !animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
        {
            transform.rotation = animator.rootRotation;
        }
        navAgent.velocity = animator.deltaPosition / Time.deltaTime;
    }
}

