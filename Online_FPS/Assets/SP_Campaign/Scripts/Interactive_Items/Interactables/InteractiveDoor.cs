using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum InteractiveDoorAxisAlignment { XAxis, YAxis, ZAxis }


// -----------------------------------------------------------------------------
// CLASS	:	InteractiveDoorInfo
// DESC		:	Describes the animation properties of a single door in an
//              InteractiveDoor
// -----------------------------------------------------------------------------
[System.Serializable]
public class InteractiveDoorInfo
{
    //Animation properties
    public Transform transf = null;
    public Vector3 rotation = Vector3.zero;
    public Vector3 movement = Vector3.zero;

    //The following are used to cache the open and closed position and rotations 
    //of the door at startup for easy Lerping/Slerping
    [HideInInspector] public Quaternion closedRotation = Quaternion.identity;
    [HideInInspector] public Quaternion openedRotation = Quaternion.identity;
    [HideInInspector] public Vector3 openedPosition = Vector3.zero;
    [HideInInspector] public Vector3 closedPosition = Vector3.zero;
}


// -----------------------------------------------------------------------------
// CLASS	:	InteractiveDoor
// DESC		:	Control mechanism for all doors (normal, sliding, double doors,
//              etc...)
// -----------------------------------------------------------------------------
[RequireComponent(typeof(BoxCollider))]
public class InteractiveDoor : InteractiveItem
{
    //Inspector-Assigned
    [Header("Activation Properties")]
    [Tooltip("Does the door start open or closed")]
    [SerializeField] protected bool isClosed = true;

    [Tooltip("Does the door start open in both directions")]
    [SerializeField] protected bool isTwoWay = true;

    [Tooltip("Does the door open automatically when the player walks into its trigger")]
    [SerializeField] protected bool autoOpen = false;

    [Tooltip("Does the door close automatically after a certain period of time")]
    [SerializeField] protected bool autoClose = false;

    [Tooltip("The Random time range for the autoclose delay")]
    [SerializeField] protected Vector2 autoCloseDelay = new Vector2(5.0f, 5.0f);
    
    [Tooltip("Disable Manual Activation")]
    [SerializeField] protected bool disableManualActivation = false;

    [Tooltip("How should the size of the box collider grow when the door is open")]
    [SerializeField] protected float colliderLengthOpenScale = 3.0f;

    [Tooltip("Should we offset the center of the collider when open")]
    [SerializeField] protected bool offsetCollider = true;

    [Tooltip("A container object used as the parent for any objects the open door should reveal/contain (eg. drawer)")]
    [SerializeField] protected Transform contentsMount = null;

    //Axis setup
    [SerializeField] protected InteractiveDoorAxisAlignment localFoewardAxis = InteractiveDoorAxisAlignment.ZAxis;

    [Header("Game State Management")] //Requirements for the door to open
    [SerializeField] protected List<GameState> requiredStates = new List<GameState>(); //Game states
    [SerializeField] protected List<GameState> requiredItems = new List<GameState>(); //Player inventory 

    [Header("Message")]
    [TextArea(3, 10), SerializeField] protected string openedHintText = "Door: Press 'Use' to close";
    [TextArea(3, 10), SerializeField] protected string closedHintText = "Door: Press 'Use' to open";
    [TextArea(3, 10), SerializeField] protected string cantActivateText = "Door: It's Locked";

    [Header("Door Transforms")]
    [Tooltip("A list of child transforms to animate")]
    [SerializeField] protected List<InteractiveDoorInfo> doors = new List<InteractiveDoorInfo>(); //Player inventory 

    [Header("Sounds")]
    [Tooltip("The AudioCollection to use for the door opening and closing sounds")]
    [SerializeField] protected AudioCollection doorSounds = null;
    [Tooltip("Optional assignment of a AudioPunchInPunchOut Database")]
    [SerializeField] protected AudioPunchInPunchOutDatabase audioPunchInPunchOutDatabase = null;

    //Private
    protected IEnumerator coroutine = null; //Allow to stop executing animation 
    protected Vector3 closedColliderSize = Vector3.zero;
    protected Vector3 closedColliderCenter = Vector3.zero;
    protected Vector3 openedColliderSize = Vector3.zero;
    protected Vector3 openedColliderCenter = Vector3.zero;
    protected BoxCollider boxCollider = null;
    protected Plane plane;
    protected bool openedFrontSide = true;
    //Used to store info about the door progress during a coroutine
    protected float normalizedTime = 0.0f;
    protected float startAnimTime = 0.0f;
    protected ulong oneShotSoundID = 0;


    protected override void Start()
    {
        base.Start();

        //Cache components
        boxCollider = col as BoxCollider;
        if(boxCollider != null)
        {
            closedColliderSize = openedColliderSize = boxCollider.size;
            closedColliderCenter = openedColliderCenter = boxCollider.center;

            float offset = 0.0f;
            //Make sure we offset the collider and grow it in the dimension specified as
            // what 'we' perceive to be its forward axis
            switch (localFoewardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    plane = new Plane(transform.right, transform.position);
                    openedColliderSize.x *= colliderLengthOpenScale;
                    offset = closedColliderCenter.x - (openedColliderSize.x / 2); //Move forward by half the scale amount
                    openedColliderCenter = new Vector3(offset, closedColliderCenter.y, closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    plane = new Plane(transform.up, transform.position);
                    openedColliderSize.y *= colliderLengthOpenScale;
                    offset = closedColliderCenter.y - (openedColliderSize.y / 2); //Move forward by half the scale amount
                    openedColliderCenter = new Vector3(closedColliderCenter.x, offset, closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    plane = new Plane(transform.forward, transform.position);
                    openedColliderSize.z *= colliderLengthOpenScale;
                    offset = closedColliderCenter.z - (openedColliderSize.z / 2); //Move forward by half the scale amount
                    openedColliderCenter = new Vector3(closedColliderCenter.x, closedColliderCenter.y, offset);
                    break;
                default:
                    break;
            }
        }

        //Start closed or opened? apply the scale and center accordingly 
        if(!isClosed)
        {
            boxCollider.size = openedColliderSize;
            if(offsetCollider)
            {
                boxCollider.center = openedColliderCenter;
            }
            openedFrontSide = true;
        }

        //Set all of the doors this object manages to their staarting orientations 
        foreach (InteractiveDoorInfo info in doors)
        {
            if(info != null && info.transf != null)
            {
                //It is assumed that all doors are set in the closed position at startup
                //so grab the current rotation quaternion and store it as the closed rotation
                info.closedRotation = info.transf.localRotation;
                info.closedPosition = info.transf.position;
                info.openedPosition = info.transf.position - info.transf.TransformDirection(info.movement);

                //Calculate a rotation to take it into the open position
                Quaternion rotationToOpen = Quaternion.Euler(info.rotation);
                if(!isClosed)
                {
                    info.transf.localRotation = info.closedRotation * rotationToOpen;
                    info.transf.position = info.openedPosition;
                }
            }
        }

        //Disable colliders of any contents if in the closed position
        if(contentsMount != null)
        {
            Collider[] colliders = contentsMount.GetComponentsInChildren<Collider>();
            foreach (Collider item in colliders)
            {
                if(isClosed)
                {
                    item.enabled = false;
                }
                else
                {
                    item.enabled = true;
                }
            }
        }

        //Animation is not currently in progress
        coroutine = null;
    }


    public override string GetText()
    {
        if (disableManualActivation)
            return null;

        //Check required items in player's inventory 
        bool haveInventoryItems = HasRequiredInventoryItems();
        
        //Check required game states are set
        bool haveRequiredStates = true;
        if(requiredStates.Count> 0)
        {
            if(ApplicationManager.Instance == null)
            {
                haveRequiredStates = false;
            }
            else
            {
                haveRequiredStates = ApplicationManager.Instance.AreRequiredStatesSet(requiredStates);
            }
        }

        if(isClosed)
        {
            if(!haveRequiredStates || !haveInventoryItems)
            {
                return cantActivateText;
            }
            else
            {
                return closedHintText;
            }
        }
        else
        {
            return openedHintText;
        }
    }

    public override void Activate(CharacterManager _chrManager)
    {
        if (disableManualActivation)
            return;

        //Check required game states are set
        bool haveRequiredStates = true;
        if (requiredStates.Count > 0)
        {
            if (ApplicationManager.Instance == null)
            {
                haveRequiredStates = false;
            }
            else
            {
                haveRequiredStates = ApplicationManager.Instance.AreRequiredStatesSet(requiredStates);
            }
        }

        //Area all requirements met?
        if(haveRequiredStates && HasRequiredInventoryItems())
        {
            if(startAnimTime <= 0.0f) //The sound still needs to reach the starting time for the animation (fixes the snap)
            {
                if(coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                coroutine = Activate(plane.GetSide(_chrManager.transform.position));
                StartCoroutine(coroutine);
            }
        }
        else
        {
            //Lock sound
            if (doorSounds != null && AudioManager.Instance != null)
            {
                AudioClip clip = doorSounds[2];
                if (clip != null)
                {
                    oneShotSoundID = AudioManager.Instance.PlayOneShotSound(doorSounds.AudioGroup,
                                                                            clip,
                                                                            transform.position,
                                                                            doorSounds.Volume,
                                                                            doorSounds.SpatialBlend,
                                                                            doorSounds.Priority);
                }
            }
        }
    }

    protected virtual IEnumerator Activate(bool frontSide, bool autoClosing = false, float delay = 0.0f)
    {
        //Wait for delay
        yield return new WaitForSeconds(delay);
        
        AudioClip clip = null;

        //Used to sync animation with sound 
        float duration = 1.5f;
        float time = 0.0f;
        startAnimTime = 0.0f;

        if(!isTwoWay) //Always front side
        {
            frontSide = true;
        }

        //Ping pong normalized time
        if(normalizedTime > 0.0f)
        {
            normalizedTime = 1 - normalizedTime;
        }

        //If the door is closed then we need to open it 
        if(isClosed)
        {
            //Consider it open from this point on
            isClosed = false;

            //Fix the 2 way snap bug while the door is opening. Now the door opens 
            //like is not 2 ways, to avoid the snap in the other direction
            if(normalizedTime > 0)
            {
                frontSide = openedFrontSide;
            }

            //Record side we opened from
            openedFrontSide = frontSide;

            //Find a sound to play
            if(doorSounds != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSound(oneShotSoundID);
                
                clip = doorSounds[0];
                if(clip != null)
                {
                    duration = clip.length; //Make the duration equal to the sound length (by default)
                    
                    //Optional Punchin/PunchOut database check
                    if(audioPunchInPunchOutDatabase != null)
                    {
                        AudioPunchInPunchOutInfo info = audioPunchInPunchOutDatabase.GetClipInfo(clip);
                        if(info != null)
                        {
                            //Get start time regiestered with this clip
                            startAnimTime = Mathf.Min(info.startTime, clip.length);

                            //If end time is larger than the start time, the duration is simple the
                            //time between the 2 markers
                            if(info.endTime >= startAnimTime)
                            {
                                duration = info.endTime - startAnimTime;
                            }
                            else //Otherwise it is assumed that the clip will be played to the end (allows to put 0 to the end time instead of the actual clip length)
                            {
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }

                    //If already part-way into the animation then we need to start the sound some way from the beginning
                    float playbackOffset = 0.0f;
                    if(normalizedTime > 0.0f)
                    {
                        playbackOffset = startAnimTime + (duration * normalizedTime); //How far we are into the animation 
                        startAnimTime = 0.0f;
                    }

                    oneShotSoundID = AudioManager.Instance.PlayOneShotSound(doorSounds.AudioGroup,
                                                                            clip,
                                                                            transform.position,
                                                                            doorSounds.Volume,
                                                                            doorSounds.SpatialBlend,
                                                                            doorSounds.Priority,
                                                                            playbackOffset);
                }
            }

            //Determine perceived forward axis, offset and scale the collider in that dimension
            float offset = 0.0f;
            switch (localFoewardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    offset = openedColliderSize.x / 2.0f;
                    if(!frontSide)
                    {
                        offset = -offset;
                    }
                    openedColliderCenter = new Vector3(closedColliderCenter.x - offset, closedColliderCenter.y, closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    offset = openedColliderSize.y / 2.0f;
                    if (!frontSide)
                    {
                        offset = -offset;
                    }
                    openedColliderCenter = new Vector3(closedColliderCenter.x, closedColliderCenter.y - offset , closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    offset = openedColliderSize.z / 2.0f;
                    if (!frontSide)
                    {
                        offset = -offset;
                    }
                    openedColliderCenter = new Vector3(closedColliderCenter.x, closedColliderCenter.y, closedColliderCenter.z - offset);
                    break;
                default:
                    break;
            }

            //Apply offset and size
            if(offsetCollider)
            {
                boxCollider.center = openedColliderCenter;
            }
            boxCollider.size = openedColliderSize;

            //if startAnimTime != 0.0f we need to let some of the sound play before we start animating the door 
            if(startAnimTime > 0.0f)
            {
                yield return new WaitForSeconds(startAnimTime);
                startAnimTime = 0.0f;
            }

            //Set the starting time of the animation
            time = duration * normalizedTime;
            //Now complete the animation for each door 
            while(time <= duration)
            {
                //Calculate new normalized time 
                normalizedTime = time / duration;
                
                foreach (InteractiveDoorInfo info in doors)
                {
                    if(info != null && info.transf != null)
                    {
                        info.transf.position = Vector3.Lerp(info.closedPosition, info.openedPosition, normalizedTime);
                        info.transf.localRotation = info.closedRotation * Quaternion.Euler(frontSide ? info.rotation * normalizedTime : -info.rotation * normalizedTime);
                    }
                }

                yield return null;
                time += Time.deltaTime;
            }

            //Enable colliders of any contents if in the opened position
            if(contentsMount != null)
            {
                Collider[] colliders = contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider item in colliders)
                {
                    item.enabled = true;
                }
            }

            //Reset time to zero
            normalizedTime = 0.0f;
            
            //if autoclose is active then swap a new coroutine to close it again
            if(autoClose)
            {
                coroutine = Activate(frontSide, true, Random.Range(autoCloseDelay.x, autoCloseDelay.y));
                StartCoroutine(coroutine);
            }
            yield break;
        }
        else //Close
        {
            //Consider it closed from this point on
            isClosed = true;

            //Cache the door in its open position 
            foreach (InteractiveDoorInfo info in doors)
            {
                if (info != null && info.transf != null)
                {
                    Quaternion rotationToOpen = Quaternion.Euler(openedFrontSide ? info.rotation : -info.rotation);
                    info.openedRotation = info.closedRotation * rotationToOpen;
                }
            }

            //Disable colliders of any contents if in the closed position
            if (contentsMount != null)
            {
                Collider[] colliders = contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider item in colliders)
                {
                    item.enabled = false;
                }
            }

            //Find a sound to play
            if (doorSounds != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSound(oneShotSoundID);

                clip = doorSounds[autoClose ? 3 : 1];
                if (clip != null)
                {
                    duration = clip.length; //Make the duration equal to the sound length

                    if(audioPunchInPunchOutDatabase != null)
                    {
                        AudioPunchInPunchOutInfo info = audioPunchInPunchOutDatabase.GetClipInfo(clip);
                        if (info != null)
                        {
                            //Get start time regiestered with this clip
                            startAnimTime = Mathf.Min(info.startTime, clip.length);

                            //If end time is larger than the start time, the duration is simple the
                            //time between the 2 markers
                            if (info.endTime >= startAnimTime)
                            {
                                duration = info.endTime - startAnimTime;
                            }
                            else //Otherwise it is assumed that the clip will be played to the end (allows to put 0 to the end time instead of the actual clip length)
                            {
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }

                    //If already part-way into the animation then we need to start the sound some way from the beginning
                    float playbackOffset = 0.0f;
                    if (normalizedTime > 0.0f)
                    {
                        playbackOffset = startAnimTime + (duration * normalizedTime); //How far we are into the animation 
                        startAnimTime = 0.0f;
                    }

                    oneShotSoundID = AudioManager.Instance.PlayOneShotSound(doorSounds.AudioGroup,
                                                                            clip,
                                                                            transform.position,
                                                                            doorSounds.Volume,
                                                                            doorSounds.SpatialBlend,
                                                                            doorSounds.Priority,
                                                                            playbackOffset);
                }
            }

            //if startAnimTime != 0.0f we need to let some of the sound play before we start animating the door 
            if (startAnimTime > 0.0f)
            {
                yield return new WaitForSeconds(startAnimTime);
                startAnimTime = 0.0f;
            }

            //Set the starting time of the animation
            time = duration * normalizedTime;
            //Now complete the animation for each door 
            while (time <= duration)
            {
                //Calculate new normalized time 
                normalizedTime = time / duration;

                foreach (InteractiveDoorInfo info in doors)
                {
                    if (info != null && info.transf != null)
                    {
                        info.transf.position = Vector3.Lerp(info.openedPosition, info.closedPosition, normalizedTime);
                        info.transf.localRotation = Quaternion.Lerp(info.openedRotation, info.closedRotation, normalizedTime);
                    }
                }

                yield return null;
                time += Time.deltaTime;
            }

            //Set rotation and position to the exact amount (to avoid any rounding error)
            foreach (InteractiveDoorInfo info in doors)
            {
                if (info != null && info.transf != null)
                {
                    info.transf.localRotation = info.closedRotation;
                    info.transf.position = info.closedPosition;
                }
            }

            boxCollider.size = closedColliderSize;
            boxCollider.center = closedColliderCenter;
        }

        normalizedTime = 0.0f;
        coroutine = null;
        yield break;
    }

    //TODO:
    //Once inventory system is implemented, implement this function 
    //that checks if the player has the required items to open the door
    protected bool HasRequiredInventoryItems()
    {
        return true;
    }


    //For Auto-open doors
    private void OnTriggerEnter(Collider other)
    {
        if (!autoOpen || !isClosed)
            return;

        bool haveRequiredStates = true;
        if (requiredStates.Count > 0)
        {
            if (ApplicationManager.Instance == null)
            {
                haveRequiredStates = false;
            }
            else
            {
                haveRequiredStates = ApplicationManager.Instance.AreRequiredStatesSet(requiredStates);
            }
        }

        //Only activate the door if we meet all requirements
        if (haveRequiredStates && HasRequiredInventoryItems())
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = Activate(plane.GetSide(other.transform.position));
            StartCoroutine(coroutine);
        }
        else
        {
            //Lock sound
            if (doorSounds != null && AudioManager.Instance != null)
            {
                AudioClip clip = doorSounds[2];
                if (clip != null)
                {
                    oneShotSoundID = AudioManager.Instance.PlayOneShotSound(doorSounds.AudioGroup,
                                                                            clip,
                                                                            transform.position,
                                                                            doorSounds.Volume,
                                                                            doorSounds.SpatialBlend,
                                                                            doorSounds.Priority);
                }
            }
        }
    }
}
