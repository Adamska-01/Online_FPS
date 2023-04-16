using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Custom Animation Curve")]
public class CustomCurve : ScriptableObject
{
    [SerializeField] AnimationCurve curve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 0.0f));

    
    public float Evaluate(float t)
    {
        return curve.Evaluate(t);
    }
}
