using FPS.Utility;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInput_PlayerActions : MonoBehaviour
{
	[SerializeField]
	private CharacterManager chrManager;
	
	
	private FPS_Controller fpsController;


	void Awake()
    {
		Guard.AgainstNull(chrManager, nameof(chrManager));
		Guard.AgainstNull(chrManager.FPSController, nameof(chrManager.FPSController));
		
		fpsController = chrManager.FPSController;
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

		chrManager?.ADS();
	}

	public void Reload_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		chrManager?.ReloadCurrentWeapon();
	}
}
