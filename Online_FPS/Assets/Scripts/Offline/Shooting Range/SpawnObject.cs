using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnObject : MonoBehaviour
{ 
    public Vector3 size;
    public GameObject objectToSpawn;
    public Transform player;
     

    public GameObject SpawnObjectFacingPlayer()
    {
        //Spawn point based on the x and z pos (y is equal to the transform)
        Vector3 randomPos = new Vector3(Random.Range((-size.x / 2), (size.x / 2)), 0.0f, Random.Range((-size.z / 2), (size.z / 2)));
        Vector3 pos = transform.position + randomPos;

        return Instantiate(objectToSpawn, pos, Quaternion.LookRotation(player.position - pos));
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.35f);
        Gizmos.DrawCube(transform.position, size);
    }
}
