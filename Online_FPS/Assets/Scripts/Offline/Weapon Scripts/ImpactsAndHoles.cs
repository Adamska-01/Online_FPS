using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactsAndHoles : MonoBehaviour
{
    public enum ImpactType
    {
        CONCRETE,
        DIRT,
        METAL,
        SAND,
        WOOD,
        BODY
    }

    [System.Serializable] public class ImpactAndHole
    { 
        public GameObject hole;
        public GameObject hit;
    }
     
    //Impact and holes 
    [SerializeField] private List<ImpactType> impactTypes = new List<ImpactType>();
    [SerializeField] private List<ImpactAndHole> impactObjects = new List<ImpactAndHole>();
    private Dictionary<ImpactType, ImpactAndHole> impactsAndHoles = new Dictionary<ImpactType, ImpactAndHole>();


    void Start()
    {
        for (int i = 0; i < impactTypes.Count; i++)
        {
            impactsAndHoles.Add(impactTypes[i], impactObjects[i]);
        }
    }


    public Dictionary<ImpactType, ImpactAndHole> GetBulletsAndImpacts()
    {
        return impactsAndHoles;
    }
}
