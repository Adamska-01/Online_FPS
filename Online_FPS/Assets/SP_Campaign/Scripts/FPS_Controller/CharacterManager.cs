using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    //Constants
    private const float MAX_HEALTH = 100.0f;

    //Inspector-Assigned
    [SerializeField] private CapsuleCollider meleeTrigger = null;
    [SerializeField] private CameraBloodEffect cameraBloodEffect = null;
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private AISoundEmitter soundEmitter = null;
    [SerializeField] private float health = MAX_HEALTH;
    [SerializeField] private float walkRadius = 0.0f;
    [SerializeField] private float runRadius = 7.0f;
    [SerializeField] private float landingRadius = 12.0f;
    [SerializeField] private float bloodRadiusScale = 6.0f;
    [SerializeField] private PlayerHUD playerHUD = null;
    //Pain/Damage Audio
    [SerializeField] private AudioCollection damageSounds = null;
    [SerializeField] private AudioCollection painSounds = null;
    [SerializeField] private float nextPainSoundTime = 0.0f;
    [SerializeField] private float painSoundOffset = 0.35f;

    //Private
    private Collider col= null;
    private FPS_Controller fpsController = null;
    private CharacterController characterController = null;
    private GameSceneManager gameSceneManger = null;
    private int AI_BodyPartLayer = -1;
    private int interactiveMask = 0;

    //Properties
    public float Health  { get { return health; } }
    public float Stamina { get { return fpsController != null ? fpsController.Stamina : 0.0f; } }
    public FPS_Controller FPSController { get { return fpsController; } }


    void Start()
    {
        col = GetComponent<Collider>();
        fpsController = GetComponent<FPS_Controller>();
        characterController = GetComponent<CharacterController>();
        gameSceneManger = GameSceneManager.Instance;
        AI_BodyPartLayer = LayerMask.NameToLayer("AI_BodyPart");
        interactiveMask = 1 << LayerMask.NameToLayer("Interactive");

        if(gameSceneManger != null)
        {
            Player_Info info = new Player_Info();
            info.camera = playerCamera;
            info.characterManager = this;
            info.collider = col;
            info.meleeTrigger = meleeTrigger;

            //Register this player
            gameSceneManger.RegisterPlayerInfo(col.GetInstanceID(), info);
        }

        //Hide Curesor and lock it to center
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //Start fading in
        if(playerHUD != null)
        {
        Debug.Log("fade internal");
            playerHUD.Fade(2.0f, ScreenFadeType.FadeIn);
        }
    }


    private void Update()
    {
        ProcessInteractableItems();

        if (Input.GetMouseButtonDown(0))
        {
            DoDamage();
        }

        //Set sound emitter radius (take damage value into account as well)
        if (fpsController != null || soundEmitter != null)
        {
            float newRadius = Mathf.Max(walkRadius, (MAX_HEALTH - health) / bloodRadiusScale);
            switch (fpsController.MovementStatus)
            {
                case PlayerMoveStatus.Running:
                    newRadius = Mathf.Max(newRadius, runRadius);
                    break;
                case PlayerMoveStatus.Landing:
                    newRadius = Mathf.Max(newRadius, landingRadius);
                    break;
            }

            soundEmitter.SetRadius(newRadius);

            fpsController.DragMultiplierLimit = Mathf.Max(health / MAX_HEALTH, 0.25f); //Set drag limit
        }

        if (playerHUD != null)
        {
            playerHUD.Invalidate(this);
        }
    }

    private void ProcessInteractableItems()
    {
        //Process interactive objects
        Ray ray;
        RaycastHit hit;
        RaycastHit[] hits;

        //Cast Ray
        ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f));
        //Calculate ray length based on where the player is looking (becomes longer if looking down, shortest is when looking forward)
        float rayLength = Mathf.Lerp(1.0f, 1.8f, Mathf.Abs(Vector3.Dot(playerCamera.transform.forward, Vector3.up)));

        //Collect hits
        hits = Physics.RaycastAll(ray, rayLength, interactiveMask);
        if (hits.Length > 0)
        {
            //Get the one with the highest priority
            int highestPriority = int.MinValue;
            InteractiveItem priorityObject = null;
            for (int i = 0; i < hits.Length; i++)
            {
                hit = hits[i];

                //Fetch interactive script from GameSceneManager and cache if it has an higher priority
                InteractiveItem interactiveObject = gameSceneManger.GetInteractiveItem(hit.collider.GetInstanceID());
                if (interactiveObject != null && interactiveObject.Priority > highestPriority)
                {
                    priorityObject = interactiveObject;
                    highestPriority = priorityObject.Priority;
                }
            }

            //Display Text if we found an interactive item
            if (priorityObject != null)
            {
                if (playerHUD != null)
                {
                    playerHUD.SetInteractionText(priorityObject.GetText());
                }

                //Use/Get Iteractable 
                if (Input.GetButtonDown("Use"))
                {
                    priorityObject.Activate(this);
                }
            }
        }
        else //No Item found
        {
            if (playerHUD != null)
            {
                playerHUD.SetInteractionText(null);
            }
        }
    }

    public void TakeDamage(float _dmg, bool _doDamage, bool _doPain)
    {
        health = Mathf.Max(health - (_dmg * Time.deltaTime), 0.0f);
        
        //Set player drag 
        if(fpsController != null)
        {
            fpsController.DragMultiplier = 0.0f;
        }

        if (cameraBloodEffect != null)
        {
            //Apply blood on screen (but 3 times less than what should be)
            cameraBloodEffect.MinBloodAmount = (1.0f - (health / MAX_HEALTH)) * 0.5f;
            cameraBloodEffect.BloodAmount = Mathf.Min(cameraBloodEffect.MinBloodAmount + 0.3f, 1.0f);
        }

        //Play Player Pain/Damage sounds
        if (AudioManager.Instance != null)
        {
            if(_doDamage && damageSounds != null)
            {
                AudioManager.Instance.PlayOneShotSound(damageSounds.AudioGroup,
                                                        damageSounds.RandomClip,
                                                        transform.position,
                                                        damageSounds.Volume,
                                                        damageSounds.SpatialBlend,
                                                        damageSounds.Priority);

                //Play Pain click after damage sound
                if(_doPain && painSounds != null && nextPainSoundTime < Time.time)
                {
                    AudioClip painClip = painSounds.RandomClip;
                    if(painClip != null)
                    {
                        nextPainSoundTime = Time.time + painClip.length + painSoundOffset;
                        StartCoroutine(AudioManager.Instance?.PlayOneShotSoundDelayed(painSounds.AudioGroup,
                                                                                      painClip,
                                                                                      transform.position,
                                                                                      painSounds.Volume,
                                                                                      painSounds.SpatialBlend,
                                                                                      painSounds.Priority));
                    }
                }
            }
        }
  
    }

    public void DoDamage(int _hitDir = 0)
    {
        if (playerCamera == null || gameSceneManger == null)
            return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        //Perform raycast from the crosshair (screen center)
        ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << AI_BodyPartLayer);

        if(isSomethingHit)
        {
            AIStateMachine stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if(stateMachine != null)
            {
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 50, hit.rigidbody, this, 0);
            }
        }
    }

    public void CompleteLevel()
    {
        if(playerHUD != null)
        {
            playerHUD.Fade(4.0f, ScreenFadeType.FadeOut);
            playerHUD.ShowMissionText("Mission Completed");
        }

        Invoke("GameOver", 4.0f);
    }
    public void GameOver()
    {
        //TODO: Go back to main menu

        //Show Cursor Again (for now)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
