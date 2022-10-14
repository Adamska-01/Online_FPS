using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NETSpawner : MonoBehaviour
{
    public static NETSpawner instance;
    private void Awake()
    {
        instance = this;
        spawnpoints = GetComponentsInChildren<SpawnPoint>();
    }

    private SpawnPoint[] spawnpoints;
    
    public Transform GetSpawnPoint()
    {
        //Try to spawn 10 times 
        int maximumTries = 10;
        for (int i = 0; i < maximumTries; i++)
        {
            SpawnPoint sp = spawnpoints[Random.Range(0, spawnpoints.Length)];
            if (!sp.IsOccupied)
            {
                Collider[] targetsInView = Physics.OverlapSphere(sp.transform.position, 28.0f);
                bool canSpawnHere = true;
                for (int j = 0; j < targetsInView.Length; j++)
                {
                    if (targetsInView[j].transform.root.tag.Contains("Player"))
                    {
                        Transform target = targetsInView[j].gameObject.transform;
                        Vector3 dirToTarget = (target.position - sp.transform.position).normalized;
                        if (Vector3.Angle(sp.transform.forward, dirToTarget) < (120.0f / 2.0f)) //60.0f == view angle (default FOV of the camera)
                        {
                            float dstToTarget = Vector3.Distance(sp.transform.position, target.position);
                            if (Physics.Raycast(sp.transform.position, dirToTarget, out RaycastHit hit, dstToTarget))
                            {
                                if (hit.collider.transform.root.tag.Contains("Player"))
                                {
                                    canSpawnHere = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (canSpawnHere) 
                    return sp.transform; 
            }
        }
         
        //Return random if no suitable spawn point is found 
        return spawnpoints[Random.Range(0, spawnpoints.Length)].transform; 
    }

    private void Shuffle(object[] arr)
    {
        System.Random rand = new System.Random();
        for (int i = arr.Length - 1; i >= 1; i--)
        {
            int j = rand.Next(i + 1);
            object tmp = arr[j];
            arr[j] = arr[i];
            arr[i] = tmp;
        }
    }
}
