using FPS.Utility;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInput_Gameplay : MonoBehaviour
{
	[SerializeField]
	private CharacterManager chrManager;
	
	[SerializeField]
	private FPS_Controller fpsController;

	[Header("Shared Variables - Input States")]
	[SerializeField] private SharedBool toggleFlashlightInputState = null;
	[SerializeField] private SharedBool interactInputState = null;


	void Awake()
	{
		Guard.AgainstNull(chrManager, nameof(chrManager));
		Guard.AgainstNull(fpsController, nameof(fpsController));
	}


	// Movement
	public void Movement_PlayerInput(InputAction.CallbackContext context)
		=> fpsController.Move(context.ReadValue<Vector2>());

	public void Run_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		fpsController.Run();
	}

	public void Jump_PlayerInput(InputAction.CallbackContext context)
		=> fpsController.Jump();

	public void Crouch_PlayerInput(InputAction.CallbackContext context)
		=> fpsController.Crouch();

	public void Crouch_Hold_PlayerInput(InputAction.CallbackContext context)
		=> fpsController.Crouch_Hold(context.ReadValue<float>());

	public void Look_PlayerInput(InputAction.CallbackContext context)
		=> fpsController.Look(context.ReadValue<Vector2>());


	// Weapons
	public void Weapon1_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		chrManager?.SwitchWeapon(0);
	}

	public void Weapon2_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		chrManager?.SwitchWeapon(1);
	}

	public void Fire_PlayerInput(InputAction.CallbackContext context)
		=> chrManager?.FireWeapon(context.performed, context.canceled);

	public void ADS_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		Debug.Log("ADS");

		chrManager?.ADS();
	}

	public void Reload_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		chrManager?.ReloadCurrentWeapon();
	}

	public void ToggleFlashlight(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		toggleFlashlightInputState.Value = true;

		_ = ResetInputStateNextFrame(toggleFlashlightInputState);
	}


	// Inventory
	public void Use_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		interactInputState.Value = true;

		_ = ResetInputStateNextFrame(interactInputState);
	}


	private async Task ResetInputStateNextFrame(SharedBool inputState)
	{
		await Task.Yield();

		inputState.Value = false;
	}
}
