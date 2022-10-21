using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWaypointNetworkEditor : Editor
{ 
    private void OnSceneGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target; 
        for (int i = 0; i < network.waypoints.Count; i++)
        { 
            //2) Add a label to each waypont
            if (network.waypoints[i] != null) 
                Handles.Label(network.waypoints[i].position, string.Format("Waypoint {0}", i)); 
        } 
        

        switch (network.displayMode)
        {
            case DisplayModes.None:
                break;
            case DisplayModes.Connections: //Connect all points with a line
                Vector3[] vectors = new Vector3[network.waypoints.Count + 1];
                for (int i = 0; i <= network.waypoints.Count; i++)
                {
                    int index = i != network.waypoints.Count ? i : 0;
                    if (network.waypoints[index] != null)
                        vectors[i] = network.waypoints[index].position;
                    else
                        vectors[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);  
                }
                //Display connecting points
                Handles.color = Color.cyan;
                Handles.DrawPolyLine(vectors);
                break;
            case DisplayModes.Paths:
                NavMeshPath path = new NavMeshPath();

                if(network.waypoints[network.UIStart] != null && network.waypoints[network.UIEnd] != null)
                {
                    Vector3 from = network.waypoints[network.UIStart].position;
                    Vector3 to = network.waypoints[network.UIEnd].position;

                    if(NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
                    {
                        //Display path
                        Handles.color = Color.yellow;
                        Handles.DrawPolyLine(path.corners); 
                    } 
                }
                break;
            default:
                break;
        }         
    }

    public override void OnInspectorGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;
        
        //Show modes 
        network.displayMode = (DisplayModes)EditorGUILayout.EnumPopup("Display Mode", network.displayMode);

        if(network.displayMode == DisplayModes.Paths)
        {
            network.UIStart = EditorGUILayout.IntSlider("UI Start", network.UIStart, 0, network.waypoints.Count - 1);
            network.UIEnd = EditorGUILayout.IntSlider("UI End", network.UIEnd, 0, network.waypoints.Count - 1);
        }

        //Draw rest of the inspector 
        DrawDefaultInspector();
    }
}
