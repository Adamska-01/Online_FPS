using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Delegate used for defining custom actions on a bob curve point.
/// </summary>
public delegate void CurveControlledBobCallback();


/// <summary>
/// Defines the types of bobbing callbacks (Horizontal and Vertical). This is used to trigger 
/// certain actions (eg. footstep sounds) whever the curve reaches a certain vertical (Y) or
/// horizontal (X) value.
/// </summary>
public enum CurveControlledBobCallbackType 
{
    Horizontal, 
    Vertical 
}


/// <summary>
/// Represents an event for controlling the bobbing animation of a GameObject using curves.
/// </summary>
[System.Serializable]
public class CurveControlledBobEvent
{
    [Tooltip("The time at which this event occurs in the animation.")]
    public float time = 0.0f;

    [Tooltip("The callback function to be executed when the event occurs.")]
    public CurveControlledBobCallback Function = null;

    [Tooltip("The type of bobbing callback, either Vertical or Horizontal.")]
    public CurveControlledBobCallbackType type = CurveControlledBobCallbackType.Vertical;
}


/// <summary>
/// Controls and manages the bobbing animation of a GameObject based on animation curves.
/// </summary>
[System.Serializable]
public class CurveControlledBob
{
    [Tooltip("The animation curve defining the bobbing motion. The curve represents the vertical movement over time.")]
    [SerializeField]
    AnimationCurve bobCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(0.5f, 1.0f),
                                                                  new Keyframe(1.0f, 0.0f), new Keyframe(1.5f, -1.0f),
                                                                  new Keyframe(2.0f, 0.0f));

    [Tooltip("The multiplier for horizontal bobbing.")]
    [SerializeField]
    private float horizontalMultiplayer = 0.01f;

    [Tooltip("The multiplier for vertical bobbing.")]
    [SerializeField]
    private float verticalMultiplayer = 0.02f;

    [Tooltip("The speed ratio between vertical and horizontal bobbing.")]
    [SerializeField] 
    private float verticalhorizontalSpeedRatio = 2.0f;

    [Tooltip("The base interval for bobbing.")]
    [SerializeField] 
    private float baseInterval = 1.0f;


    //Private internals 
    private float prevXPlayHead;
    private float prevYPlayHead;
    private float xPlayHead;
    private float yPlayHead;
    private float curveEndTime;
    private List<CurveControlledBobEvent> events = new List<CurveControlledBobEvent>();


    /// <summary>
    /// Initializes the CurveControlledBob with default values and calculates the curve end time.
    /// </summary>
    public void Initialize()
    {
        curveEndTime = bobCurve[bobCurve.length - 1].time;

        xPlayHead = 0.0f;
        yPlayHead = 0.0f;
        prevXPlayHead = 0.0f;
        prevYPlayHead = 0.0f;
    }

    /// <summary>
    /// Registers an event callback for controlling the bobbing animation at a specific time
    /// (used for footstep sounds).
    /// </summary>
    /// <param name="time">The time at which the event should occur.</param>
    /// <param name="function">The callback function to be executed when the event occurs.</param>
    /// <param name="type">The type of bobbing callback (Vertical or Horizontal).</param>

    public void RegisterEventCallback(float time, CurveControlledBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent newEvent = new CurveControlledBobEvent();
        newEvent.time = time;
        newEvent.Function = function;
        newEvent.type = type;

        events.Add(newEvent);
        //Sort by time 
        events.Sort(
            delegate (CurveControlledBobEvent e1, CurveControlledBobEvent e2)
            {
                return (e1.time.CompareTo(e2.time));
            }
        );
    }

    /// <summary>
    /// Calculates the offset vector for bobbing animation based on the given speed.
    /// </summary>
    /// <param name="speed">The speed of the GameObject.</param>
    /// <returns>The offset vector for the bobbing animation.</returns>
    public Vector3 GetVectorOffset(float speed)
    {
        xPlayHead += (speed * Time.deltaTime) / baseInterval;
        yPlayHead += ((speed * Time.deltaTime) / baseInterval) * verticalhorizontalSpeedRatio;

        if (xPlayHead > curveEndTime)
            xPlayHead -= curveEndTime;

        if (yPlayHead > curveEndTime)
            yPlayHead -= curveEndTime;

        // Process Events
        for (int i = 0; i < events.Count; i++)
        {
            CurveControlledBobEvent ev = events[i];
            if (ev != null)
            {
                if (ev.type == CurveControlledBobCallbackType.Vertical)
                {
                    if ((prevYPlayHead < ev.time && yPlayHead >= ev.time) ||
                        (prevYPlayHead > yPlayHead && (ev.time > prevYPlayHead || ev.time <= yPlayHead)))
                    {
                        ev.Function();
                    }
                }
                else
                {
                    if ((prevXPlayHead < ev.time && xPlayHead >= ev.time) ||
                        (prevXPlayHead > xPlayHead && (ev.time > prevXPlayHead || ev.time <= xPlayHead)))
                    {
                        ev.Function();
                    }
                }
            }
        }

        //Evaluate new curve pos
        float xPos = bobCurve.Evaluate(xPlayHead) * horizontalMultiplayer;
        float yPos = bobCurve.Evaluate(yPlayHead) * verticalMultiplayer;

        //Record current playHead
        prevXPlayHead = xPlayHead;
        prevYPlayHead = yPlayHead;

        return new Vector3(xPos, yPos, 0.0f);
    }
}
