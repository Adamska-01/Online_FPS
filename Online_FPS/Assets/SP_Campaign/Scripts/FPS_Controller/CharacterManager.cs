using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    //Constants
    public const float MAX_HEALTH = 100.0f;

    //Inspector-Assigned
    [SerializeField] private CapsuleCollider meleeTrigger = null;
    [SerializeField] private CameraBloodEffect cameraBloodEffect = null;
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private AISoundEmitter soundEmitter = null;
    [SerializeField] private float walkRadius = 0.0f;
    [SerializeField] private float runRadius = 7.0f;
    [SerializeField] private float landingRadius = 12.0f;
    [SerializeField] private float bloodRadiusScale = 6.0f;
    [SerializeField] private PlayerHUD playerHUD = null;
    
    [Header("Pain/Damage Audio & Settings")]
    [SerializeField] private AudioCollection damageSounds = null;
    [SerializeField] private AudioCollection painSounds = null;
    [SerializeField] private AudioCollection tauntSounds = null;
    [SerializeField] private float nextPainSoundTime = 0.0f;
    [SerializeField] private float painSoundOffset = 0.35f;
    [SerializeField] private float tauntRadius = 10.0f;
   
    [Header("Inventory")]
    [SerializeField] private GameObject inventoryUI = null;
    [SerializeField] private Inventory inventory = null;

    [Header("Shared Variables")]
    [SerializeField] private SharedFloat health    = null;
    [SerializeField] private SharedFloat infection = null;
    [SerializeField] private SharedString interactionText = null;
    
    //Private
    private Collider col= null;
    private FPS_Controller fpsController = null;
    private CharacterController characterController = null;
    private GameSceneManager gameSceneManger = null;
    private int AI_BodyPartLayer = -1;
    private int interactiveMask = 0;
    private float nextAttackTime = 0.0f;
    private float nextTauntTime = 0.0f;

    //Properties
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
            playerHUD.Fade(2.0f, ScreenFadeType.FadeIn);
        }

        //Disable Inventory UI at start up
        inventoryUI.SetActive(false);
    }

    private void OnEnable()
    {
        //Register Inventory Listeners
        if(inventory != null)
        {
            inventory.OnWeaponChange.AddListener(OnSwitchWeapon);
            inventory.OnWeaponDropped.AddListener(OnDrophWeapon);
        }
    }

    

    private void OnDisable()
    {
        //Unregister Inventory Listeners
        if (inventory != null)
        {
            inventory.OnWeaponChange.AddListener(OnSwitchWeapon);
            inventory.OnWeaponDropped.AddListener(OnDrophWeapon);
        }
    }

    private void Update()
    {
        //Inventory Key Toggle 
        if(Input.GetButtonDown("Inventory") && inventoryUI != null) 
        {
            //Toggle Inventory UI
            inventoryUI.SetActive(!inventoryUI.activeSelf);       
            if(playerHUD != null)
            {
                //Toggle HUD
                playerHUD.gameObject.SetActive(!inventoryUI.activeSelf);

                //Toggle Cursor visibility.lockstate
                Cursor.visible = inventoryUI.activeSelf;
                Cursor.lockState = inventoryUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        ProcessInteractableItems();

        //Push (Attack)
        if (Input.GetMouseButtonDown(0) && Time.time > nextAttackTime)
        {
            DoDamage();
        }
        
        //Set sound emitter radius (take damage value into account as well)
        if (fpsController != null || soundEmitter != null)
        {
            float newRadius = Mathf.Max(walkRadius, (MAX_HEALTH - health.Value) / bloodRadiusScale);
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

            fpsController.DragMultiplierLimit = Mathf.Max(health.Value / MAX_HEALTH, 0.25f); //Set drag limit
        }

        //Taunt 
        if (Input.GetMouseButtonDown(1))
        {
            DoTaunt();
        }
    }

    private void DoTaunt()
    {
        if (tauntSounds == null || Time.time < nextTauntTime)
            return;

        //Play tount sound
        AudioClip tauntClip = tauntSounds[0];
        AudioManager.Instance.PlayOneShotSound(tauntSounds.AudioGroup,
                                               tauntClip,
                                               transform.position,
                                               tauntSounds.Volume,
                                               tauntSounds.SpatialBlend,
                                               tauntSounds.Priority);

        //Set sound emitter radius
        if(soundEmitter != null)
        {
            soundEmitter.SetRadius(tauntRadius);
        }

        nextTauntTime = Time.time + tauntClip.length;
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
                if (interactionText != null)
                {
                    interactionText.Value = priorityObject.GetText();
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
            if (interactionText != null)
            {
                interactionText.Value = null;
            }
        }
    }

    public void TakeDamage(float _dmg, bool _doDamage, bool _doPain)
    {
        health.Value = Mathf.Max(health.Value - (_dmg * Time.deltaTime), 0.0f);
        
        //Set player drag 
        if(fpsController != null)
        {
            fpsController.DragMultiplier = 0.0f;
        }

        //Apply blood on screen (but 3 times less than what should be)
        if (cameraBloodEffect != null)
        {
            cameraBloodEffect.MinBloodAmount = (1.0f - (health.Value / MAX_HEALTH)) * 0.5f;
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
        
        //Die
        if(health.Value <= 0)
        {
            DoDeath();
        }
    }


    private void DoDeath()
    {
        if (fpsController != null)
        {
            fpsController.FreezeMovement = true;
        }

        //GameOver Screen
        if (playerHUD != null)
        {
            playerHUD.Fade(2.5f, ScreenFadeType.FadeOut);
            //playerHUD.ShowMissionText("Mission Failed");
            //playerHUD.RefreshUI(this);
        }

        //Back to main menu
        Invoke("GameOver", 3.2f);
    }

    private void OnSwitchWeapon(InventoryWeaponMountInfo _weaponMount)
    {
        if(inventory != null)
        {
            //Calculate mount index
            int mountIndex = (_weaponMount.weapon.WeaponType == InventoryWeaponType.SingleHanded) ? 0 : 1;
            
            //If there is any weapon on that index, drop it
            inventory.DropWeaponItem(mountIndex);

            inventory.AssignWeapon(mountIndex, _weaponMount);
        }
    }
    
    private void OnDrophWeapon(InventoryItemWeapon arg0)
    {
        Debug.Log("Dropping Weapon");
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
        isSomethingHit = Physics.Raycast(ray, out hit, 1.0f, 1 << AI_BodyPartLayer);

        if(isSomethingHit)
        {
            AIStateMachine stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if(stateMachine != null)
            {
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 1, hit.rigidbody, this, 0);
                nextAttackTime = Time.time + 0.5f;
            }
        }
    }

    public void CompleteLevel()
    {
        if(playerHUD != null)
        {
            playerHUD.Fade(4.0f, ScreenFadeType.FadeOut);
            //playerHUD.ShowMissionText("Mission Completed");
        }

        Invoke("GameOver", 4.5f);
    }

    public void GameOver()
    {
        //Show Cursor Again (for now)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //Open Main Menu
        ApplicationManager.Instance.LoadMainMenu();
    }
}
