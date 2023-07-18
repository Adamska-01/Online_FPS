using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------------
// CLASS	:	SharedTimedStringQueue
// DESC		:	Scriptable object that handles timed delivery of messages queued up.
//              Callers can enqueue new messages and get the currently focused 
//              message using the 'text' property.
// --------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable OBJ/Shared Variables/Shared Timed String Queue", fileName = "New Shared Timed String Queue")]
public class SharedTimedStringQueue : ScriptableObject, ISharedVariableCallbackReceiver
{
    public event Action OnVariableAssigned;

    [SerializeField, TextArea(2, 7)] private string noteToDeveloper = "An automated timed message delivery queue.\n\nUsage:\n1) Queue.Enqueue( 'My Message');\n2) Debug.Log(Queue.text);\n3) A SO_CoroutineRunner Instance must exist in the current scene.";

    [SerializeField] protected float dequeueDelay = 3.5f;

    //Internals
    protected float nextDequeueTime = 0.0f;
    protected IEnumerator coroutine = null;
    protected bool paused = false; //Alternative mechanism to pause the queue (without Time.timeScale)
    protected string currentText = null;
    //A string float queue
    private Queue<string> messageQueue = new Queue<string>();


    //Properties
    public string CurrentDequeuedText { get { return currentText; } }
    public bool Paused { get { return paused; } set { paused = value; } }
    

    //Add string to the message
    public void Enqueue(string _message)
    {
        if(SO_CoroutineRunner.Instance == null)
        {
            currentText = "Timed Text Queue Error: No CoroutineRunner object present in the scene!";
            return;
        }

        messageQueue.Enqueue(_message);
        if(coroutine == null)
        {
            coroutine = QueueProcessor();
            SO_CoroutineRunner.Instance.StartCoroutine(coroutine);
        }
    }

    protected IEnumerator QueueProcessor()
    {
        while(true) //While there are messages in the queue
        {
            if (!paused)
            {
                //Update Timer (unscaled time, as we don't use 'Time.timeScale' to pause)
                nextDequeueTime -= Time.unscaledDeltaTime;
                if(nextDequeueTime < 0.0f)
                {
                    //Nothing in the queue so break coroutine
                    if (messageQueue.Count <= 0)
                        break;

                    //Set next message as current text
                    currentText = messageQueue.Dequeue();

                    //Invoke event (PlayerHUD)
                    OnVariableAssigned?.Invoke();

                    //Set times so nothing happes again until the deque delay elapses
                    nextDequeueTime = dequeueDelay;
                }
            }

            yield return null;
        }

        //Nothing in queue so empty the current string and set coroutine to null as
        //the coroutine is about to end.
        currentText = null;
        coroutine = null;

        //Invoke event (PlayerHUD)
        OnVariableAssigned?.Invoke();
    }

    public int Count()
    {
        return messageQueue.Count;
    }
}