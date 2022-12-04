using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum DisplayModes
{
    None,
    Connections,
    Paths
}

public class AIWaypointNetwork : MonoBehaviour
{
    [HideInInspector] public DisplayModes displayMode = DisplayModes.None;
    [HideInInspector] public int UIStart = 0;
    [HideInInspector] public int UIEnd = 0;

    public List<Transform> waypoints = new List<Transform>();
}
