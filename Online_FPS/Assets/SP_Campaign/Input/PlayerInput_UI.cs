using FPS.Utility;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInput_UI : MonoBehaviour
{
	[SerializeField]
	private PlayerInventoryUI inventoryUI;


	private PlayerInput playerInput;


	void Awake()
	{
		playerInput = GetComponent<PlayerInput>();

		Guard.AgainstNull(playerInput, nameof(playerInput));
		Guard.AgainstNull(inventoryUI, nameof(inventoryUI));
	}


	public void Toggle_Inventory_PlayerInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		var InventoryUIVisibility = inventoryUI.ToggleInventory();

		var currentActionMap = InventoryUIVisibility ? InputActionMaps.UI_ACTION_MAP : InputActionMaps.GAMEPLAY_ACTION_MAP;

		playerInput.SwitchCurrentActionMap(currentActionMap);
	}
}
