using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab = null;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();


    private void Awake()
    {
        if (spawnPoints.Count == 0 || prefab == null)
            return;

        //Instantiate at one of the points in the array
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}
