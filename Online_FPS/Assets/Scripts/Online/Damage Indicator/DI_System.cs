using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DI_System : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject indicatorPrefab = null;
    [SerializeField] private RectTransform holder = null;
    [HideInInspector] public Camera cam = null;
    [HideInInspector] public Transform player = null;

    private Dictionary<Transform, DamageIndicator> indicators = new Dictionary<Transform, DamageIndicator>();

    #region delegates
    public static Action<Transform> CreateIndicator = delegate { };
    public static Func<Transform, bool> CheckIfObjectInSight = null;
    #endregion

    private void OnEnable()
    {
        CreateIndicator += Create;
        CheckIfObjectInSight += InSight;
    }

    private void OnDisable()
    {
        CreateIndicator -= Create;
        CheckIfObjectInSight -= InSight;
    }

    private void Create(Transform _target)
    {
        //Reset timer if indicator is already instantiated
        if(indicators.ContainsKey(_target))
        {
            indicators[_target].Restart();
            return;
        }

        //Instantiate indicator and unregister after timer
        DamageIndicator newIndicator = Instantiate(indicatorPrefab, holder).GetComponent<DamageIndicator>();
        newIndicator.Register(_target, player, new Action(() => { indicators.Remove(_target); }));

        indicators.Add(_target, newIndicator);
    }

    bool InSight(Transform _t)
    { 
        Vector3 screenPoint = cam.WorldToViewportPoint(_t.position);
        return screenPoint.z > 0.0f && screenPoint.x > 0.0f && screenPoint.x < 1.0f && screenPoint.y > 0.0f && screenPoint.y < 1.0f; 
    }
}
