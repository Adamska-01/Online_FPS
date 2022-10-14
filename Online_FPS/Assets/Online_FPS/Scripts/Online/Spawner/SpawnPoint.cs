using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject graphics;
    [SerializeField] private bool isOccupied;
    public bool IsOccupied { get { return isOccupied; } }
    public Collider pCol;

    private void Awake()
    {
        graphics.SetActive(false); 
    }


    public void Update()
    {
        if(isOccupied && !pCol)
        {
            isOccupied = false;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.root.tag.Contains("Player"))
        {
            isOccupied = true;
            pCol = other;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.root.tag.Contains("Player"))
        {
            isOccupied = true;
            pCol = other;
        } 
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.tag.Contains("Player"))
        {
            isOccupied = false; 
        }
    }
}
