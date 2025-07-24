using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player character. This is the conduit to the Arms Animator, the Inventory 
/// System, and the Weapons System as well as many other things such as managing the player 
/// damage.
/// </summary>
public class CharacterManager : MonoBehaviour
{
	//Constants
	public const float MAX_HEALTH = 100.0f;
	
	//Inspector-Assigned
	[SerializeField] private CapsuleCollider meleeTrigger = null;
	[SerializeField] private CameraBloodEffect cameraBloodEffect = null;
	[SerializeField] private Camera sceneCam = null;
	[SerializeField] private AISoundEmitter soundEmitter = null;
	[SerializeField] private float walkRadius = 0.0f;
	[SerializeField] private float runRadius = 7.0f;
	[SerializeField] private float landingRadius = 12.0f;
	[SerializeField] private float bloodRadiusScale = 6.0f;
	[SerializeField] private PlayerHUD playerHUD = null;
	
	[Header("Pain/Damage Audio & Settings")]
	[SerializeField] private AudioCollection damageSounds = null;
	[SerializeField] private AudioCollection painSounds = null;
	[SerializeField] private float nextPainSoundTime = 0.0f;
	[SerializeField] private float painSoundOffset = 0.35f;
   
	[Header("Inventory")]
	[SerializeField] private GameObject inventoryUI = null;
	[SerializeField] private Inventory inventory = null;
	[SerializeField] private InventoryItemWeapon defaultWeapon = null;
	[SerializeField] private Flashlight primaryFlashlight = new Flashlight();
	[SerializeField] private bool flashlightOnStart = false;

	[Header("Arms System")]
	[SerializeField] private Animator armsAnimator = null;
	[SerializeField] private List<ArmsObject> armsObjects = new List<ArmsObject>();
	[SerializeField] private LayerMask weaponRayLayerMask = new LayerMask();

	[Header("Shared Variables")]
	[SerializeField] private SharedFloat health    = null;
	[SerializeField] private SharedFloat stamina = null;
	[SerializeField] private SharedFloat infection = null;
	[SerializeField] private SharedFloat crosshairAlpha = null;
	[SerializeField] private SharedString interactionText = null;
	[SerializeField] private SharedVector3 crosshairPosition = null;
	[SerializeField] private SharedSprite crosshairSprite = null;
	[SerializeField] private VectorShaker cameraShaker = null;
	
	// Internal
	private Collider col= null;
	private FPS_Controller fpsController = null;
	private GameSceneManager gameSceneManger = null;
	private int interactiveMask = 0;

	//Arms & Weapons 
	private WeaponController weaponController = null;
	private Flashlight secondaryFlashlight = null; // TODO: make a 'flashlight controller' or something
	private bool canSwitchWeapons = false;

	private Dictionary<ScriptableObject, ArmsObject> armsObjectsDictionary = new Dictionary<ScriptableObject, ArmsObject>();

	// Animation Hashes
	private int flashlightHash          = Animator.StringToHash("Flashlight");          // Is flashlight on
	private int speedHash               = Animator.StringToHash("Speed");               // Speed setting of character (Idle, Walking or running)
	private int dualModeFOVHash         = Animator.StringToHash("Dual Mode FOV Weight");// Animation curve driven. used to set the FOV
	private int staminaHash             = Animator.StringToHash("Stamina");             // Stamina of the player
	private int playerSpeedOverrideHash = Animator.StringToHash("Player Speed Override"); // Allows animation to override max speed of player
	private int crosshairAlphaHash      = Animator.StringToHash("Crosshair Alpha");          // Transparency of the crosshair


    public event Action OnVariableValueChanged; 
	public Action OnPickUpAmmo;

    //Properties
    public FPS_Controller FPSController { get { return fpsController; } }
    public WeaponController WPNController { get { return weaponController; } }
	public Flashlight SecondaryFlashlight { get { return secondaryFlashlight; } set { secondaryFlashlight = value; } }

	void Awake()
	{
		col = GetComponent<Collider>();
		fpsController = GetComponent<FPS_Controller>();
		gameSceneManger = GameSceneManager.Instance;
	}

	void Start()
	{
		interactiveMask = 1 << LayerMask.NameToLayer("Interactive");

		if (gameSceneManger != null)
		{
			Player_Info info = new Player_Info();
			info.camera = sceneCam;
			info.characterManager = this;
			info.collider = col;
			info.meleeTrigger = meleeTrigger;

			//Register this player
			gameSceneManger.RegisterPlayerInfo(col.GetInstanceID(), info);
		}

		//Hide Curesor and lock it to center
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		ArmsBaseSMB[] stateMachineBehaviours = armsAnimator.GetBehaviours<ArmsBaseSMB>();
		Dictionary<ScriptableObject, List<ArmsBaseSMB>> stateMachineBehavioursByID = new Dictionary<ScriptableObject, List<ArmsBaseSMB>>();
		foreach (ArmsBaseSMB item in stateMachineBehaviours)
		{
			// Store this manager to each behaviour
			item.characterMgr = this;

			// Store a reference  each behaviour
			if(item.identifier != null)
			{
				if(stateMachineBehavioursByID.TryGetValue(item.identifier, out List<ArmsBaseSMB> behaviourList))
				{
					behaviourList.Add(item);
				}
				else
				{
					List<ArmsBaseSMB> newBehaviourList = new List<ArmsBaseSMB>() { item };
					stateMachineBehavioursByID.Add(item.identifier, newBehaviourList);
				}
			}
		}

		// Copy over the fps weapon prefabs in the scene (attached to our
		// arms) stored in a list into a dictionary for quick runtime access
		for (int i = 0; i < armsObjects.Count; i++)
		{
			ArmsObject armsObject = armsObjects[i];
			if (armsObject != null && armsObject.identifier != null) 
			{
				// Store the gameobject list in the dictionary by ID
				armsObjectsDictionary.Add(armsObject.identifier, armsObject);

				// See if this weapon has an AnimatorStateCallback so that the animator can call it
				if (armsObject.callback != null && stateMachineBehavioursByID.TryGetValue(armsObject.identifier, out List<ArmsBaseSMB> behaviourList))
				{
					foreach (ArmsBaseSMB item in behaviourList)
					{
						if (item != null)
						{
							item.callbackHandler = armsObject.callback;
						}
					}
				}
			}
		}

		// Start off with the primary flashlight and mesh disabled
		primaryFlashlight.ActivateLight(false);
		primaryFlashlight.ActivateMesh(false);

		// Set the starting state of the flashlight
		ActivateFlashlight(flashlightOnStart);
    }

	private void OnEnable()
	{
		if (weaponController == null)
		{
            weaponController = new WeaponController(this,
                                                sceneCam,
                                                armsAnimator,
                                                defaultWeapon,
                                                inventory,
                                                soundEmitter,
                                                inventoryUI,
                                                crosshairPosition,
                                                crosshairSprite,
                                                cameraShaker,
                                                weaponRayLayerMask);
        }

		weaponController.RegisterWeaponEvents();
    }
	
	private void OnDisable()
	{
		weaponController.DeregisterWeaponEveents();
	}

	private void Update()
	{
		if(armsAnimator != null)
		{
			// Set player speed override from the animator in the controller
			if(fpsController != null)
			{
				fpsController.SpeedOverride = armsAnimator.GetFloat(playerSpeedOverrideHash);
			}

			if (crosshairAlpha != null)
			{
				crosshairAlpha.Value = armsAnimator.GetFloat(crosshairAlphaHash);
			}

			// Set stamina in the animator
			float normalizedStamina = 1.0f;
			if(stamina != null && fpsController != null)
			{
				normalizedStamina = (stamina.Value / 100.0f) * fpsController.DragMultiplier;
			}
			armsAnimator.SetFloat(staminaHash, Mathf.Min(normalizedStamina + 0.1f, 1.0f));
		}

		weaponController.UpdateWeaponControllerState();

        ProcessInteractableItems(); // (Interactable Raycast)
				
		if(fpsController != null)
		{
			// Set animator to current speed
			if(armsAnimator)
			{
				switch (fpsController.MovementStatus)
				{
					case PlayerMoveStatus.Walking:
						armsAnimator.SetInteger(speedHash, 1);
						break;
					case PlayerMoveStatus.Running:
					case PlayerMoveStatus.Landing:
						armsAnimator.SetInteger(speedHash, 2);
						break;
					default:
						armsAnimator.SetInteger(speedHash, 0);
						break;
				}
			}

			//Set sound emitter radius (take damage value into account as well)
			if (soundEmitter != null)
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
		}

		// Process flashlight input when inventory is not active
		if((inventoryUI != null && !inventoryUI.activeSelf) || inventoryUI == null)
		{
			// Set crosshair position 
			if(crosshairPosition != null && sceneCam != null) 
			{
				crosshairPosition.Value = sceneCam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0.0f));

				if(weaponController.CurrentWeapon != null)
				{
					ArmsObject armsObject = armsObjectsDictionary[weaponController.CurrentWeapon];
					if(armsObject != null)
					{
						if(weaponController.CurrentWeapon.DualMode)
						{
							if(armsObject.crosshairPositionDualMode && armsObject.crosshairPosition)
							{
								crosshairPosition.Value = Vector3.Lerp(sceneCam.WorldToScreenPoint(armsObject.crosshairPosition.position),
																	   sceneCam.WorldToScreenPoint(armsObject.crosshairPositionDualMode.position),
																	   armsAnimator.GetFloat(dualModeFOVHash));
							}
							else
							{
								if(armsObject.crosshairPosition != null)
								{
									crosshairPosition.Value = sceneCam.WorldToScreenPoint(armsObject.crosshairPosition.position);
								}
							}
						}
					}
				}
			}

			if(armsAnimator != null)
			{
				if(Input.GetButtonDown("Flashlight"))
				{
					ActivateFlashlight(!armsAnimator.GetBool(flashlightHash));
				}
			}
		}
	}

	public void ActivateFlashlight(bool _activate)
	{
		// Trigger flashlight animation
		armsAnimator?.SetBool(flashlightHash, _activate);

		// Enable/Disable the secondary flashlight without going through the animator.
		// This will always be null if we don't currently have a weapon with a flashlight on it.
		secondaryFlashlight?.ActivateLight(_activate);
	}

	public void ActivateFlashlightMesh_AnimatorCallback(bool _enableMesh, FlashlightType _type)
	{
		if(_type == FlashlightType.Primary)
		{
			primaryFlashlight?.ActivateMesh(_enableMesh);
		}
	}

	public void ActivateFlashlightLight_AnimatorCallback(bool _enableLight, FlashlightType _type)
	{
		if (_type == FlashlightType.Primary)
		{
			primaryFlashlight?.ActivateLight(_enableLight);
		}
		else if(secondaryFlashlight != null)
		{
			secondaryFlashlight.ActivateLight(_enableLight);
		}
	}

	private void ProcessInteractableItems()
	{
		//Process interactive objects
		Ray ray;
		RaycastHit hit;
		RaycastHit[] hits;

		//Cast Ray
		ray = sceneCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f));
		//Calculate ray length based on where the player is looking (becomes longer if looking down, shortest is when looking forward)
		float rayLength = Mathf.Lerp(1.0f, 1.8f, Mathf.Abs(Vector3.Dot(sceneCam.transform.forward, Vector3.up)));

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
				if(!(priorityObject.GetType() == typeof(CollectableWeapon) && !canSwitchWeapons))
				{
					if (Input.GetButtonDown("Use"))
					{
						priorityObject.Activate(this);

						// Should set up an event ("OnPickUpAmmo") and subscribe to it, but ehi, give me a break
						if (weaponController.CurrentWeapon != null)
						{
							OnPickUpAmmo?.Invoke();
						}
					}
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

	public ArmsObject GetArmObjectFromDictionary(InventoryItemWeapon _weaponKey)
	{
		armsObjectsDictionary.TryGetValue(_weaponKey, out ArmsObject armsObject);

		return armsObject;
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


	#region Player Input - Weapons
    public void SwitchWeapon(int weaponIndex)
		=> weaponController?.SwitchWeapon(weaponIndex);

	public void FireWeapon(bool isPerformed, bool isCanceled)
		=> weaponController?.FireWeapon(isPerformed, isCanceled);

	public void ADS()
		=> weaponController?.ADS();

	public void ReloadCurrentWeapon()
		=> weaponController?.ReloadCurrentWeapon();
	#endregion
}
