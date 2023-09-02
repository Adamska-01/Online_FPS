using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------------
// CLASS	:	TimedStateKey
// DESC		:	Describes a Time at which a state should be set in the "Application
//              State Dictionary".
// --------------------------------------------------------------------------------
[System.Serializable]
public class TimedStateKey
{
    public float time = 0.0f;
    public string key = null;
    public string value = null;
    public string UIMessage = null;
}


// --------------------------------------------------------------------------------
// CLASS	:	TimedStateKey
// DESC		:	Describes a Caption and a time to display that caption.
// --------------------------------------------------------------------------------
[System.Serializable]
public class TimedCaptionKey
{
    public float time = 0.0f;
    [TextArea(3, 10)] public string text = null;
}


// --------------------------------------------------------------------------------
// CLASS	:	InventoryItemAudio
// DESC		:	Describes an Audio Inventory Item Scriptable Object 
// --------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable OBJ/Inventory System/Items/Audio", fileName = "New Audio Item")]
public class InventoryItemAudio : InventoryItem
{
    [Header("Audio Log Properties")]
    [Tooltip("The Author of the Audio Log")]
    [SerializeField] private string person = null;

    [Tooltip("The Subject of the Audio Log")]
    [SerializeField] private string subject = null;

    [Tooltip("The Image of this Audio Log")]
    [SerializeField] private Texture2D image = null;

    [Header("State Change Keys")]
    [Tooltip("A list of timed state changes to occur throughout the timeline of the Audio Log")]
    [SerializeField] private List<TimedStateKey> stateKeys= new List<TimedStateKey>();

    [Header("Caption Keys")]
    [Tooltip("A list of timed captions to display throughout the timeline of the Audio Log")]
    [SerializeField] private List<TimedCaptionKey> captions = new List<TimedCaptionKey>();


    //Public Properties
    public string Person { get { return person; } }
    public string Subject { get { return subject; } }
    public Texture2D Image { get { return image; } }
    public List<TimedStateKey> StateKeys { get { return stateKeys; } }
    public List<TimedCaptionKey> CaptionKeys { get { return captions; } }


    public override InventoryItem Use(Vector3 _position, bool _playAudio = true, Inventory _inventory = null)
    {
        return null; //Do not play audio
    }
}
