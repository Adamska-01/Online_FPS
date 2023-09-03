using UnityEngine;
using TMPro;
using System;


public class TranscriptPanel : MonoBehaviour
{
    //Inspector-Assigned
    [Header("Text Components")]
    [SerializeField] private TMP_Text TranscriptText = null;
    [SerializeField] private TMP_Text notificationText = null;


    [Header("Shared Variables")]
    [SerializeField] private SharedString tranascript = null;
    [SerializeField] private SharedTimedStringQueue notificationQueue = null;

    //Internal
    private Action OnTranscriptChange = null;
    private Action OnNotificationChange = null;


    private void Start()
    {
        if (tranascript == null || notificationQueue == null)
            return;

        //Cache event listeners 
        OnTranscriptChange = () => { TranscriptText?.SetText(tranascript.Value); };
        OnNotificationChange = () => { notificationText?.SetText(notificationQueue.CurrentDequeuedText); };

        //Execute on start for the first time (the rest is handled by the events)
        OnTranscriptChange.Invoke();
        OnNotificationChange.Invoke();

        //Subscribe to the shared variables events 
        tranascript.OnVariableValueChanged += OnTranscriptChange;
        notificationQueue.OnVariableValueChanged += OnNotificationChange;
    }


    private void OnDestroy()
    {
        //Unsubscribe to the shared variables events 
        tranascript.OnVariableValueChanged -= OnTranscriptChange;
        notificationQueue.OnVariableValueChanged -= OnNotificationChange;
    }
}
