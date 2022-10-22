using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentExample : MonoBehaviour
{
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;

    //
    public bool hasPath = false;
    public bool pathPending = false; 
    public bool pathStale = false; 
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid; 

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
        hasPath = navAgent.hasPath;
        pathPending = navAgent.pathPending;
        pathStale = navAgent.isPathStale;
        pathStatus = navAgent.pathStatus;

        if((!hasPath && !pathPending) || (pathStatus == NavMeshPathStatus.PathInvalid /*|| pathStatus == NavMeshPathStatus.PathPartial*/))
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
}
