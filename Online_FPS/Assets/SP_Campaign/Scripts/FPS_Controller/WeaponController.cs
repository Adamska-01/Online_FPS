using System;
using System.Collections;
using UnityEngine;


public class WeaponController
{
	// Constructor-Assigned
	private CharacterManager characterManager = null;
	private Camera sceneCam = null;
	private Animator armsAnimator = null;
	private InventoryItemWeapon defaultWeapon = null;
	private Inventory inventory = null;
	private AISoundEmitter soundEmitter = null;
	private GameObject inventoryUI = null;
	private SharedVector3 crosshairPosition = null;
	private SharedSprite crosshairSprite = null;
	private VectorShaker cameraShaker = null;
	private LayerMask weaponRayLayerMask = new LayerMask();

	// Internal
	private GameSceneManager gameSceneManger = null;
	private InventoryItemWeapon currentWeapon = null;
	private InventoryItemWeapon nextWeapon = null;
	private InventoryWeaponMountInfo nextWeaponMountInfo = null;
	private bool canSwitchWeapons = false;
	private IEnumerator switchWeaponCoroutine = null;
	private int availableAmmo = 0;
	private float initialFOV = 60.0f;

	// Animator Hashes
	private int weaponAnimHash          = Animator.StringToHash("Weapon Anim");         // The current sub-state machine to play for the selected weapon
	private int clearWeaponHash         = Animator.StringToHash("Clear Weapon");        // Hash of Clear Weapon Trigger in animator
	private int canSwitchWeaponsHash    = Animator.StringToHash("Can Switch Weapons");  // Can we switch to a different weapon at the moment
	private int attackAnimHash          = Animator.StringToHash("Attack Anim");         // Used by machines that have several random attack states
	private int attackTriggerHash       = Animator.StringToHash("Attack");              // Used to trigger a transition into an attack state
	private int weaponArmedHash         = Animator.StringToHash("Weapon Armed");        // Is the current weapon armed
	private int switchingWeaponHash     = Animator.StringToHash("Switching Weapon");    // Can we switch to a different weapon at the moment
	private int dualModeActiveHash      = Animator.StringToHash("Dual Mode Active");    // Does the current weapon have a dual firing mode that is active
	private int dualModeFOVHash         = Animator.StringToHash("Dual Mode FOV Weight");// Animation curve driven. used to set the FOV
	private int dualHandedWeaponHash    = Animator.StringToHash("Dual Handed Weapon");  // Is the current weapon two handed
	private int reloadHash              = Animator.StringToHash("Reload");              // Do we require a reload
	private int autoFireHash            = Animator.StringToHash("Auto Fire");           // Does the weapon support auto fire
	private int reloadRepeatHash        = Animator.StringToHash("Reload Repeat");       // How many times should the reload animation loop (used for partial reload types)

	// Properties 
	public InventoryItemWeapon CurrentWeapon { get { return currentWeapon; } }


	public WeaponController(CharacterManager characterManager,
							Camera sceneCam,
							Animator armsAnimator,
							InventoryItemWeapon defaultWeapon,
							Inventory inventory,
							AISoundEmitter soundEmitter,
							GameObject inventoryUI,
							SharedVector3 crosshairPosition,
							SharedSprite crosshairSprite,
							VectorShaker cameraShaker,
							LayerMask weaponRayLayerMask)
	{
		this.characterManager = characterManager;
		this.sceneCam = sceneCam;
		this.armsAnimator = armsAnimator;
		this.defaultWeapon = defaultWeapon;
		this.inventory = inventory;
		this.soundEmitter = soundEmitter;
		this.inventoryUI = inventoryUI;
		this.crosshairPosition = crosshairPosition;
		this.crosshairSprite = crosshairSprite;
		this.cameraShaker = cameraShaker;
		this.weaponRayLayerMask = weaponRayLayerMask;
		
		if (this.sceneCam != null)
		{
			this.initialFOV = this.sceneCam.fieldOfView;
		}

		gameSceneManger = GameSceneManager.Instance;
	}

	public void RegisterWeaponEvents()
	{
		if (inventory != null)
		{
			inventory.OnWeaponChange.AddListener(OnSwitchWeapon);
			inventory.OnWeaponDropped.AddListener(OnDrophWeapon);
		}

		if (characterManager != null)
		{
			characterManager.OnPickUpAmmo += UpdateAvailableAmmo;
		}
	}

	public void DeregisterWeaponEveents()
	{
		if (inventory != null)
		{
			inventory.OnWeaponChange.RemoveListener(OnSwitchWeapon);
			inventory.OnWeaponDropped.RemoveListener(OnDrophWeapon);
		}

		if (characterManager != null)
		{
			characterManager.OnPickUpAmmo -= UpdateAvailableAmmo;
		}
	}

	public void UpdateWeaponControllerState()
	{
		if (armsAnimator == null)
			return;

		// Grab the current state of the animator and set the 'canSwitchWeapon' bool
		canSwitchWeapons = (armsAnimator.GetFloat(canSwitchWeaponsHash) > 0.75f) ? true : false;

		// Set FOV based (fetched by the animator parameter)
		var zoomFOVWeight = armsAnimator.GetFloat(dualModeFOVHash);
		if (sceneCam != null && !zoomFOVWeight.Equals(0.0f) && currentWeapon != null && currentWeapon.DualMode)
		{
			sceneCam.fieldOfView = Mathf.Lerp(initialFOV, currentWeapon.DualModeFOV, zoomFOVWeight);
		}
		else
		{
			sceneCam.fieldOfView = initialFOV;
		}
	}

	public void ShootAndDoDamage(int _hitDir = 0)
	{
		// Has fire request been successful?
		if (inventory != null &&
			currentWeapon != null &&
			currentWeapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition &&
			inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.WeaponAmmoOnly) > 0)
		{
			inventory.DecreaseAmmoInWeapon(currentWeapon.WeaponType == InventoryWeaponType.TwoHanded ? 1 : 0);

			if (inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.WeaponAmmoOnly) < 1)
			{
				armsAnimator?.SetBool(autoFireHash, false);
			}
		}

		// Set the rest
		var weapon = currentWeapon == null ? defaultWeapon : currentWeapon;

		if (weapon == null)
			return;

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
			ray.origin = ray.origin - characterManager.transform.forward * weapon.RayRadius;

			hits = Physics.SphereCastAll(ray, weapon.RayRadius, weapon.Range, weaponRayLayerMask.value, QueryTriggerInteraction.Ignore);

			foreach (var potentialHit in hits)
			{
				if (potentialHit.transform.gameObject.layer != LayerMask.NameToLayer("AI_BodyPart"))
					continue;

				var sightTestRay = ray;
				ray.origin += characterManager.transform.forward * weapon.RayRadius; // <Move forward again for the normal raycast
				sightTestRay.direction = potentialHit.point - sightTestRay.origin;

				if (!Physics.Raycast(sightTestRay, out hit, 1000, weaponRayLayerMask.value, QueryTriggerInteraction.Ignore))
					continue;

				if (potentialHit.transform != hit.transform)
					continue;

				if (hit.rigidbody == null)
					continue;

				var stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());

				if (stateMachine == null)
					continue;

				var damage = weapon.GetAttentuatedDamage(hit.rigidbody.tag, hit.distance) * (wmi == null ? 1 : wmi.condition / 100.0f);

				stateMachine.TakeDamage(hit.point,
										ray.direction * weapon.GetAttentuatedForce(hit.distance),
										(int)damage,
										hit.rigidbody,
										characterManager,
										_hitDir);

				isSomethingHit = true;
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
												characterManager,
												_hitDir);

						isSomethingHit = true;
					}
				}
			}
		}

		if (isSomethingHit && cameraShaker != null && weapon.ShakeType == InventoryItemWeaponShakeType.OnHit)
		{
			cameraShaker.ShakeVector(weapon.ShakeDuration, weapon.ShakeMagnitude, weapon.ShakeDamping);
		}
	}

	public void SwitchMount(InventoryItemWeapon _nextWeapon)
	{
		if (!canSwitchWeapons || switchWeaponCoroutine != null)
			return;

		switchWeaponCoroutine = SwitchWeaponInternal(_nextWeapon, null);
		SO_CoroutineRunner.Instance?.StartCoroutine(switchWeaponCoroutine);
	}

	#region Player Input
	public void SwitchWeapon(int index)
	{
		if (index < 0 || index > 1)
			throw new ArgumentException($"The weapon index ({index}) is out of bounds");

		SwitchMountInternal(index);
	}

	public void FireWeapon(bool isPerformed, bool isCanceled)
	{
		if (isCanceled)
			armsAnimator.SetBool(autoFireHash, false);

		if (!isPerformed)
			return;
		
		int attackAnim = 1;
		bool autofireEnabled = false;
		bool canFire = true;

		if (defaultWeapon != null)
		{
			attackAnim = UnityEngine.Random.Range(1, defaultWeapon.AttackAnimCount + 1);
		}

		if (currentWeapon != null)
		{
			int mountIndex = currentWeapon.WeaponType == InventoryWeaponType.TwoHanded ? 1 : 0;

			InventoryWeaponMountInfo currWMI = inventory.GetWeapon(mountIndex);

			if ((currentWeapon.WeaponFeedType == InventoryWeaponFeedType.Ammunition && currWMI.inGunRounds > 0 && currWMI.condition > 0) ||
					currentWeapon.WeaponFeedType != InventoryWeaponFeedType.Ammunition)
			{
				autofireEnabled = currentWeapon.AutoFire;

				attackAnim = UnityEngine.Random.Range(1, currentWeapon.AttackAnimCount + 1);
			}
			else
			{
				canFire = false;
			}
		}

		if (canFire) // Fire
		{
			armsAnimator.SetTrigger(attackTriggerHash);
			armsAnimator.SetInteger(attackAnimHash, attackAnim);
			armsAnimator.SetBool(autoFireHash, autofireEnabled);
		}
	}

	public void ADS()
	{
		if (currentWeapon == null || !currentWeapon.DualMode)
			return;

		armsAnimator.SetBool(dualModeActiveHash, !armsAnimator.GetBool(dualModeActiveHash));
	}

	public void ReloadCurrentWeapon()
	{
		int mountIndex = currentWeapon.WeaponType == InventoryWeaponType.TwoHanded ? 1 : 0;

		InventoryWeaponMountInfo currWMI = inventory.GetWeapon(mountIndex);

		if (currWMI != null && currWMI.inGunRounds < currentWeapon.AmmoCapacity)
		{
			// Only reload if we are changing to a more plentiful clip
			if (inventory.IsReloadAvailable(mountIndex))
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
	#endregion

	private void OnSwitchWeapon(InventoryWeaponMountInfo _weaponMount)
	{
		if (canSwitchWeapons && _weaponMount != null && _weaponMount.weapon && switchWeaponCoroutine == null)
		{
			switchWeaponCoroutine = SwitchWeaponInternal(_weaponMount.weapon, _weaponMount);
			SO_CoroutineRunner.Instance?.StartCoroutine(switchWeaponCoroutine);
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
		if (currentWeapon != _weapon || currentWeapon == null)
			return;

		// Deactivate the corresponding arms object
		characterManager.GetArmObjectFromDictionary(currentWeapon)?.SetAllSceneObjectsActiveState(false);

		// TODO: Check if this is fine to remove
		// We have processed this mouse action so clear it
		//Input.ResetInputAxes();

		// Force the animator to an immediate disarmed state
		armsAnimator?.SetTrigger(clearWeaponHash);
		armsAnimator?.SetBool(weaponArmedHash, false);
		armsAnimator?.SetInteger(weaponAnimHash, 0);

		currentWeapon = null;
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
			var armsObj = characterManager.GetArmObjectFromDictionary(nextWeapon);
			if (armsObj != null)
			{
				armsObj.SetAllSceneObjectsActiveState(true);

				// Also assign weapon's secondary flashlight (if available on the gun)
				characterManager.SecondaryFlashlight = armsObj.light;
			}

			// This is our new current weapon
			currentWeapon = nextWeapon;

			// Assign weapon-specific crosshair sprite  
			if (crosshairSprite != null)
			{
				crosshairSprite.Value = currentWeapon.Crosshair;
			}

			// Also get available ammo for that weapon
			if (inventory != null)
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
		if (currentWeapon != null) // There is no weapon to deactivate
		{
			// Get current weapon and switch it off
			var armsObj = characterManager.GetArmObjectFromDictionary(currentWeapon);
			if (armsObj != null)
			{
				armsObj.SetAllSceneObjectsActiveState(false);
			}
		}

		// Drop the current weapon (if the weapon switch is instigated by the inventory system)
		if (nextWeapon != null && nextWeaponMountInfo != null && nextWeaponMountInfo.weapon != null)
		{
			var droppedWeapon = inventory?.DropWeaponItem((nextWeaponMountInfo.weapon.WeaponType == InventoryWeaponType.TwoHanded) ? 1 : 0);
			
			if (droppedWeapon != null)
			{
				droppedWeapon.transform.position = nextWeaponMountInfo.PickUpPosition;
				droppedWeapon.transform.rotation = nextWeaponMountInfo.PickUpRotation;
			}
		}

		// Since we dropped, we currently have no weapon
		currentWeapon = null;

		// Clear weapon-specific crosshair sprite  
		if (crosshairSprite != null)
		{
			crosshairSprite.Value = null;
		}

		// Clear secondary flashlight
		characterManager.SecondaryFlashlight = null;
	}

	public void ReloadWeapon_AnimatorCallback(InventoryWeaponType _type)
	{
		if (inventory == null || currentWeapon == null || currentWeapon.WeaponType == InventoryWeaponType.None)
			return;

		// Reload (Inventory-Wise)
		inventory.ReloadWeaponItem(currentWeapon.WeaponType == InventoryWeaponType.SingleHanded ? 0 : 1, false);

		availableAmmo = inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.NoWeaponAmmo);
	}


	private IEnumerator SwitchWeaponInternal(InventoryItemWeapon _nextWeapon, InventoryWeaponMountInfo _weaponMount)
	{
		if (armsAnimator == null) // We need an animator to switch weapons 
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

		if (_nextWeapon != null)
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
	
	private void SwitchMountInternal(int _mountIndex)
	{
		if (armsAnimator == null)
			return;

		var weaponMountInfo = inventory?.GetWeapon(_mountIndex);

		// Only process this keypress if we have something at the mount 
		if (weaponMountInfo == null || weaponMountInfo.weapon == null)
			return;

		// Just toggle its armed state if the weapon we have is the one current one 
		if (currentWeapon == weaponMountInfo.weapon)
		{
			//Get current armed status
			var weaponArmed = armsAnimator.GetBool(weaponArmedHash);
			
			weaponArmed = !weaponArmed;

			nextWeapon = weaponArmed ? weaponMountInfo.weapon : null;

			// Instruct animator to arm/disarm weapon 
			armsAnimator.SetBool(weaponArmedHash, weaponArmed);
		}
		else // Switch to the other weapon
		{
			SwitchMount(weaponMountInfo.weapon);
		}

		armsAnimator.SetBool(dualModeActiveHash, false);
	}

	private void UpdateAvailableAmmo()
	{
		availableAmmo = inventory.GetAvailableAmmo(currentWeapon.Ammo, AmmoAmountRequestType.NoWeaponAmmo);
	}
}