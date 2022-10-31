using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentExample : MonoBehaviour
{
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;

    IEnumerator currentCoroutine = null;

    //
    public bool hasPath = false;
    public bool pathPending = false; 
    public bool pathStale = false; 
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve jumpCurve = new AnimationCurve();

    private NavMeshAgent navAgent = null;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
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

        //Approachinng an off mesh link
        if (navAgent.isOnOffMeshLink && currentCoroutine == null)
        {
            currentCoroutine = Jump(1.0f);
            StartCoroutine(currentCoroutine);
            return;
        }

        //If we do not have a path and one isn't oebdubg then set the next
        //waypoint as the target, otherwise if the path is stale, regenerate path
        if((navAgent.remainingDistance <= navAgent.stoppingDistance && !pathPending) || (pathStatus == NavMeshPathStatus.PathInvalid /*|| pathStatus == NavMeshPathStatus.PathPartial*/))
            SetNextDestination(true); 
        else if(pathStale)
            SetNextDestination(false);
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

        while(time <= _duration)
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
