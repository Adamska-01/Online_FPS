using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;


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
	[SerializeField] private AudioCollection tauntSounds = null;
	[SerializeField] private float nextPainSoundTime = 0.0f;
	[SerializeField] private float painSoundOffset = 0.35f;
	[SerializeField] private float tauntRadius = 10.0f;
   
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
	

	//Private
	private Collider col= null;
	private FPS_Controller fpsController = null;
	private CharacterController characterController = null;
	private GameSceneManager gameSceneManger = null;
	private int AI_BodyPartLayer = -1;
	private int interactiveMask = 0;
	private float nextTauntTime = 0.0f;

	//Arms & Weapons 
	private Flashlight secondaryFlashlight = null;
	private InventoryItemWeapon currentWeapon = null;
	private InventoryItemWeapon nextWeapon = null;
	private InventoryWeaponMountInfo nextWeaponMountInfo = null;
	private bool canSwitchWeapons = false;
	private IEnumerator switchWeaponCoroutine = null;
	private int availableAmmo = 0;
	private float initialFOV = 60.0f;


	private Dictionary<ScriptableObject, ArmsObject> armsObjectsDictionary = new Dictionary<ScriptableObject, ArmsObject>();

	// Animation Hashes
	private int weaponAnimHash          = Animator.StringToHash("Weapon Anim");         // The current sub-state machine to play for the selected weapon
	private int weaponArmedHash         = Animator.StringToHash("Weapon Armed");        // Is the current weapon armed
	private int flashlightHash          = Animator.StringToHash("Flashlight");          // Is flashlight on
	private int speedHash               = Animator.StringToHash("Speed");               // Speed setting of character (Idle, Walking or running)
	private int attackAnimHash          = Animator.StringToHash("Attack Anim");         // Used by machines that have several random attack states
	private int attackTriggerHash       = Animator.StringToHash("Attack");              // Used to trigger a transition into an attack state
	private int canSwitchWeaponsHash    = Animator.StringToHash("Can Switch Weapons");  // Can we switch to a different weapon at the moment
	private int switchingWeaponHash     = Animator.StringToHash("Switching Weapon");    // Can we switch to a different weapon at the moment
	private int dualHandedWeaponHash    = Animator.StringToHash("Dual Handed Weapon");  // Is the current weapon two handed
	private int dualModeActiveHash      = Animator.StringToHash("Dual Mode Active");    // Does the current weapon have a dual firing mode that is active
	private int dualModeFOVHash         = Animator.StringToHash("Dual Mode FOV Weight");// Animation curve driven. used to set the FOV
	private int reloadHash              = Animator.StringToHash("Reload");              // Do we require a reload
	private int reloadRepeatHash        = Animator.StringToHash("Reload Repeat");       // How many times should the reload animation loop (used for partial reload types)
	private int staminaHash             = Animator.StringToHash("Stamina");             // Stamina of the player
	private int autoFireHash            = Animator.StringToHash("Auto Fire");           // Does the weapon support auto fire
	private int playerSpeedOverrideHash = Animator.StringToHash("Player Speed Override"); // Allows animation to override max speed of player
	private int clearWeaponHash         = Animator.StringToHash("Clear Weapon");          // Hash of Clear Weapon Trigger in animator
	private int crosshairAlphaHash      = Animator.StringToHash("Crosshair Alpha");          // Transparency of the crosshair

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
		
		if(sceneCam != null)
		{
			initialFOV = sceneCam.fieldOfView;
		}

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

		//Start fading in
		if(playerHUD != null)
		{
			playerHUD.Fade(2.0f, ScreenFadeType.FadeIn);
		}

		//Disable Inventory UI at start up
		inventoryUI.SetActive(false);

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

		if(armsAnimator != null)
		{
			// Grab the current state of the animator and set the 'canSwitchWeapon' bool
			canSwitchWeapons = (armsAnimator.GetFloat(canSwitchWeaponsHash) > 0.75f) ? true : false;

			// Set player speed override from the animator in the controller
			if(fpsController != null)
			{
				fpsController.SpeedOverride = armsAnimator.GetFloat(playerSpeedOverrideHash);
			}

			if (crosshairAlpha != null)
			{
				crosshairAlpha.Value = armsAnimator.GetFloat(crosshairAlphaHash);
			}

			// Set FOV based (fetched by the animator parameter)
			float zoomFOVWeight = armsAnimator.GetFloat(dualModeFOVHash);
			if(sceneCam != null && !zoomFOVWeight.Equals(0.0f) && currentWeapon != null && currentWeapon.DualMode)
			{
				sceneCam.fieldOfView = Mathf.Lerp(initialFOV, currentWeapon.DualModeFOV, zoomFOVWeight);
			}
			else
			{
				sceneCam.fieldOfView = initialFOV;
			}

			// Set stamina in the animator
			float normalizedStamina = 1.0f;
			if(stamina != null && fpsController != null)
			{
				normalizedStamina = (stamina.Value / 100.0f) * fpsController.DragMultiplier;
			}
			armsAnimator.SetFloat(staminaHash, Mathf.Min(normalizedStamina + 0.1f, 1.0f));
		}

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

				if(currentWeapon != null)
				{
					ArmsObject armsObject = armsObjectsDictionary[currentWeapon];
					if(armsObject != null)
					{
						if(currentWeapon.DualMode)
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

				// Process Mount/Weapon switching 
				int mountIndex = -1;
				if(canSwitchWeapons)
				{
					if(Input.GetButtonDown("Single Handed Mount"))
					{
						mountIndex = 0;
					}
					else if (Input.GetButtonDown("Dual Handed Mount"))
					{
						mountIndex = 1;
					}
				}

				if(mountIndex != -1)
				{
					InventoryWeaponMountInfo weaponMountInfo = null;
					if(inventory)
					{
						weaponMountInfo = inventory.GetWeapon(mountIndex);
					}

					// Only process this keypress if we have something at the mount 
					if(weaponMountInfo != null && weaponMountInfo.weapon != null)
					{
						// Just toggle its armed state if the weapon we have is the one current one 
						if(currentWeapon == weaponMountInfo.weapon)
						{
							//Get current armed status
							bool weaponArmed = armsAnimator.GetBool(weaponArmedHash);
							weaponArmed = !weaponArmed;

							if(weaponArmed)
							{
								nextWeapon = weaponMountInfo.weapon;
							}
							else
							{
								nextWeapon = null;
							}

							// Instruct animator to arm/disarm weapon 
							armsAnimator.SetBool(weaponArmedHash, weaponArmed);
						}
						else // Switch to the other weapon
						{
							SwitchMount(weaponMountInfo.weapon);
						}
					}
				}

				// Process fire button
				if(Input.GetButtonDown("Fire1"))
				{
					int attackAnim = 1;
					bool autofireEnabled = false;
					bool canFire = true;

					if(defaultWeapon)
					{
						attackAnim = Random.Range(1, defaultWeapon.AttackAnimCount + 1);
					}

					if(currentWeapon != null)
					{
						mountIndex = currentWeapon.WeaponType ==InventoryWeaponType.TwoHanded ? 1 : 0;

						InventoryWeaponMountInfo currWMI = inventory.GetWeapon(mountIndex);

						if ((currentWeapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition && currWMI.inGunRounds > 0 && currWMI.condition > 0) || 
							 currentWeapon.WeaponFeedType != InventoryWeaponFeedType.Ammunition)
						{
							autofireEnabled = currentWeapon.AutoFire;

							attackAnim = Random.Range(1, currentWeapon.AttackAnimCount + 1);
						}
						else
						{
							canFire = false;
						}
					}

					if(canFire) // Fire
					{
						armsAnimator.SetTrigger(attackTriggerHash);
						armsAnimator.SetInteger(attackAnimHash, attackAnim);
						armsAnimator.SetBool(autoFireHash, autofireEnabled);
					}
				}
				else if(Input.GetButtonUp("Fire1"))
				{
					armsAnimator.SetBool(autoFireHash, false);
				}

				// ADS -> Used by any weapon that support it
				if (Input.GetButtonDown("Fire2") && currentWeapon != null && currentWeapon.DualMode)
				{
					armsAnimator.SetBool(dualModeActiveHash, !armsAnimator.GetBool(dualModeActiveHash));
				}

				if(Input.GetButtonDown("Reload") && currentWeapon != null && currentWeapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition && availableAmmo > 0)
				{
					mountIndex = currentWeapon.WeaponType == InventoryWeaponType.TwoHanded ? 1 : 0;

					InventoryWeaponMountInfo currWMI = inventory.GetWeapon(mountIndex);

					if(currWMI != null && currWMI.inGunRounds < currentWeapon.AmmoCapacity) 
					{
						// Only reload if we are changing to a more plentiful clip
						if(inventory.IsReloadAvailable(mountIndex))
						{
							// How many times do we need to reload? (if partial)
							if (currentWeapon.ReloadType == InventoryWeaponReloadType.Partial)
							{ 
								armsAnimator.SetInteger(reloadRepeatHash, Mathf.Min(availableAmmo, currentWeapon.AmmoCapacity - currWMI.inGunRounds));
							}
							else
							{
								armsAnimator.SetInteger(reloadRepeatHash, 0);
							}

							armsAnimator.SetBool(reloadHash, true); // Reload
						}
					}
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

	public void EnableWeapon_AnimatorCallback()
	{
		// Assign next weapon to the correct mount
		if (nextWeapon != null) 
		{
			if (nextWeapon != currentWeapon && nextWeaponMountInfo != null && inventory)
			{
				inventory.AssignWeapon((nextWeapon.WeaponType == InventoryWeaponType.SingleHanded) ? 0 : 1, nextWeaponMountInfo);
			}

			// Get the weapon we switched to and set it on (active)
			if (armsObjectsDictionary.TryGetValue(nextWeapon, out ArmsObject armsObj))
			{
				armsObj.SetAllSceneObjectsActiveState(true);

				// Also assign weapon's secondary flashlight (if available on the gun)
				secondaryFlashlight = armsObj.light;
			}

			// This is our new current weapon
			currentWeapon = nextWeapon;

			// Assign weapon-specific crosshair sprite  
			if(crosshairSprite != null)
			{
				crosshairSprite.Value = currentWeapon.Crosshair;
			}

			// Also get available ammo for that weapon
			if(inventory != null)
			{
				availableAmmo = inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.NoWeaponAmmo);
			}
		}

		// Switching weapon process complete
		armsAnimator?.SetBool(switchingWeaponHash, false);

		// Weapon has been switched so there is no weapon waiting any longer
		nextWeaponMountInfo = null;
		nextWeapon = null;
	}

	public void DisableWeapon_AnimatorCallback()
	{
		if(currentWeapon != null) // There is no weapon to deactivate
		{
			// Get current weapon and switch it off
			if(armsObjectsDictionary.TryGetValue(currentWeapon, out ArmsObject armsObj))
			{
				armsObj.SetAllSceneObjectsActiveState(false);
			}
		}

		// Drop the current weapon (if the weapon switch is instigated by the inventory system)
		if(nextWeapon != null && nextWeaponMountInfo != null && nextWeaponMountInfo.weapon != null)
		{
			inventory?.DropWeaponItem((nextWeaponMountInfo.weapon.WeaponType == InventoryWeaponType.TwoHanded) ? 1 :  0);
		}

		// Since we dropped, we currently have no weapon
		currentWeapon = null;

		// Clear weapon-specific crosshair sprite  
		if (crosshairSprite != null)
		{
			crosshairSprite.Value = null;
		}

		// Clear secondary flashlight
		secondaryFlashlight = null;
	}

	public void ReloadWeapon_AnimatorCallback(InventoryWeaponType _type)
	{
		if (inventory == null || currentWeapon == null || currentWeapon.WeaponType == InventoryWeaponType.None)
			return;

		// Reload (Inventory-Wise)
		inventory.ReloadWeaponItem(currentWeapon.WeaponType == InventoryWeaponType.SingleHanded ? 0 : 1, false);

		availableAmmo = inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.NoWeaponAmmo);
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
						if (currentWeapon != null)
						{
							availableAmmo = inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.NoWeaponAmmo);
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

	private void OnSwitchWeapon(InventoryWeaponMountInfo _weaponMount)
	{
		if(canSwitchWeapons && _weaponMount != null && _weaponMount.weapon && switchWeaponCoroutine == null)
		{
			switchWeaponCoroutine = SwitchWeaponInternal(_weaponMount.weapon, _weaponMount);
			StartCoroutine(switchWeaponCoroutine);
		}
	}

	// Player-Instigated weapon switch
	public void SwitchMount(InventoryItemWeapon _nextWeapon)
	{
		if (canSwitchWeapons && switchWeaponCoroutine == null)
		{
			switchWeaponCoroutine = SwitchWeaponInternal(_nextWeapon, null);
			StartCoroutine(switchWeaponCoroutine);
		}
	}

	private void OnDrophWeapon(InventoryItemWeapon _weapon)
	{
		// We only want to process this event when the UI is active. This is out way of responding to
		// a DropWeapon event within the UI so that our Arms and Weapons hierarchy stays synced.
		if ((inventoryUI && !inventoryUI.activeSelf) || !inventoryUI) 
			return;

		// Is the weapon we are dropping the current weapon we are using
		// because if so we need to remove if from our arms
		if (currentWeapon == _weapon && currentWeapon != null)
		{
			// Deactivate the corresponding arms object
			if (armsObjectsDictionary.TryGetValue(currentWeapon, out ArmsObject armsObject))
			{
				armsObject.SetAllSceneObjectsActiveState(false);
			}

			// We have processed this mouse action so clear it
			Input.ResetInputAxes();

			// Force the animator to an immediate disarmed state
			armsAnimator?.SetTrigger(clearWeaponHash);
			armsAnimator?.SetBool(weaponArmedHash, false);
			armsAnimator?.SetInteger(weaponAnimHash, 0);

			currentWeapon = null;
		}
	}
	
	private IEnumerator SwitchWeaponInternal(InventoryItemWeapon _nextWeapon, InventoryWeaponMountInfo _weaponMount)
	{
		if(armsAnimator == null) // We need an animator to switch weapons 
		{
			switchWeaponCoroutine = null;
			yield break;
		}

		// Cancel the reload (if there is a pending reload) 
		armsAnimator.SetBool(reloadHash, false);

		// Disarm current weapon 
		armsAnimator.SetBool(weaponArmedHash, false);

		// The weapon we wish to transition next
		nextWeapon = _nextWeapon;

		// Pick up info - If null then the weapon is assumed already be mount and will not be added in the inventory
		nextWeaponMountInfo = _weaponMount;

		if(_nextWeapon != null)
		{
			// Let animator know we are transitioning to a single/dual handed weapon
			// In case of single handed weapon, we allow to bring up the flash light in the left hand. 
			armsAnimator.SetBool(dualHandedWeaponHash, _nextWeapon.WeaponType == InventoryWeaponType.TwoHanded);
			armsAnimator.SetBool(switchingWeaponHash, true);

			// Force a wait state so the animator can pick up on a switch between two weapons of the same type
			yield return new WaitForSecondsRealtime(0.2f);

			// Arm next weapon
			armsAnimator.SetBool(weaponArmedHash, true);
			armsAnimator.SetInteger(weaponAnimHash, _nextWeapon.WeaponAnim);
		}

		// Free this coroutine
		switchWeaponCoroutine = null;
	}

	public void DoDamage(int _hitDir = 0)
	{
		// Has fire request been successful?
		if (inventory != null && 
			currentWeapon != null &&
			currentWeapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition &&
			inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.WeaponAmmoOnly) > 0)
		{
			inventory.DecreaseAmmoInWeapon(currentWeapon.WeaponType == InventoryWeaponType.TwoHanded ? 1 : 0);

			if(inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.WeaponAmmoOnly) < 1)
			{
				armsAnimator?.SetBool(autoFireHash, false);
			}
		}

		// Set the rest
		InventoryItemWeapon weapon = currentWeapon == null ? defaultWeapon : currentWeapon;
		if(weapon != null)
		{
			soundEmitter.SetRadius(weapon.SoundRadius);

            InventoryWeaponMountInfo wmi = null;

			if (weapon != defaultWeapon && inventory != null && weapon.WeaponType != InventoryWeaponType.None)
			{
				// Weapon condition
				wmi = inventory.GetWeapon(weapon.WeaponType == InventoryWeaponType.SingleHanded ? 0 : 1);
				if (wmi != null)
				{
					wmi.condition = Mathf.Max(0.0f, wmi.condition - weapon.ConditionDepletion);
				}
			}

			// Camera shake
			if (cameraShaker != null && weapon.ShakeType == InventoryItemWeaponShakeType.OnFire)
			{
				cameraShaker.ShakeVector(weapon.ShakeDuration, weapon.ShakeMagnitude, weapon.ShakeDamping);
			}

			// Raycast
			if (sceneCam == null || gameSceneManger == null)
				return;

			Ray ray;
			RaycastHit hit = new RaycastHit();
			RaycastHit[] hits;
			bool isSomethingHit = false;

			ray = sceneCam.ScreenPointToRay(crosshairPosition.Value);

			if (weapon.RayRadius > 0.0f) // eg. shotgun, rpg, etc..
			{
				// Move the origin back a bit as the sphere cast only works from half sphere onward
				ray.origin = ray.origin - transform.forward * weapon.RayRadius;

				hits = Physics.SphereCastAll(ray, weapon.RayRadius, weapon.Range, weaponRayLayerMask.value, QueryTriggerInteraction.Ignore);

				foreach (var potentialHit in hits)
				{
					if (potentialHit.transform.gameObject.layer == LayerMask.NameToLayer("AI_BodyPart"))
					{
						Ray sightTestRay = ray;
						ray.origin += transform.forward * weapon.RayRadius; // <Move forward again for the normal raycast

						sightTestRay.direction = potentialHit.point - sightTestRay.origin;

                        if (Physics.Raycast(sightTestRay, out hit, 1000, weaponRayLayerMask.value, QueryTriggerInteraction.Ignore))
						{
							if (potentialHit.transform == hit.transform)
							{
								if (hit.rigidbody != null)
								{
                                    AIStateMachine stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());
                                    if (stateMachine != null)
                                    {
                                        float damage = weapon.GetAttentuatedDamage(hit.rigidbody.tag, hit.distance) * (wmi == null ? 1 : wmi.condition / 100.0f);

                                        stateMachine.TakeDamage(hit.point,
                                                                ray.direction * weapon.GetAttentuatedForce(hit.distance),
                                                                (int)damage,
                                                                hit.rigidbody,
                                                                this,
                                                                _hitDir);

                                        isSomethingHit = true;
                                    }
                                }
							}
						}
                    }
                }
			}
			else
            {
                if (Physics.Raycast(ray, out hit, weapon.Range, weaponRayLayerMask.value, QueryTriggerInteraction.Ignore))
				{
                UnityEngine.Debug.Log("Fist hit");	
					if (hit.rigidbody != null)
					{
						AIStateMachine stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());
						if (stateMachine != null)
						{
							float damage = weapon.GetAttentuatedDamage(hit.rigidbody.tag, hit.distance) * (wmi == null ? 1 : wmi.condition / 100.0f);

							stateMachine.TakeDamage(hit.point,
													ray.direction * weapon.GetAttentuatedForce(hit.distance),
													(int)damage,
													hit.rigidbody,
													this,
													_hitDir);

							isSomethingHit = true;
						}
					}
				}
			}

			if(isSomethingHit && cameraShaker != null && weapon.ShakeType == InventoryItemWeaponShakeType.OnHit)
			{
                cameraShaker.ShakeVector(weapon.ShakeDuration, weapon.ShakeMagnitude, weapon.ShakeDamping);
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
