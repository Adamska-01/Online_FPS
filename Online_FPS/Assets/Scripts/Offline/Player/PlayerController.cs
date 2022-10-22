using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Componens
    [HideInInspector] public CharacterController characterController;
    private InputManager inputManager;
    private SkinnedMeshRenderer chMesh;
    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationController animController; 
    [SerializeField] private Transform gunTarget;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private UpperBodyIK ik;
    public Transform groundCheck;
    public LayerMask groundMask;
     
    //Guns
    [SerializeField] Item[] items;
    private int itemIndex;
    private int previousItemIndex = -1;
    [HideInInspector] public bool hasKnife = false;

    //Movement vector
    private Vector3 movementDir;
     
    //Movement values
    private float movementSpeed = 3.8f;
    private float speedMultiplier = 1.8f;
    private float crouchMultiplier = 0.6f;
    private float aimSensitivity = 0.4f; 
    public float sensitivityMultiplier = 1.0f; //Main sensitivity variable
    private float ADSsensitivityMultiplier = 0.3f;
    private float jumpForce = 0.35f; 
    private float ySpeed;
    private float verticalLookRotation = 0.0f;
    private float groundDistance = 0.4f;

    //States 
    [HideInInspector] public bool isFiring;
    [HideInInspector] public bool isFiringSingleShot;
    [HideInInspector] public bool isReloading;
    [HideInInspector] public bool isAiming;
    [HideInInspector] public bool canReload;
    private bool isGrounded;
    public bool IsRunning { get { return (!inputManager.Crouch && !inputManager.Back && inputManager.Run); } }
   
    private bool previousCrouch;
    private bool currentCrouch;


    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputManager = FindObjectOfType<InputManager>();
        chMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        //Equip first weapon
        isReloading = false;
        EquipItem(0); 

        //Set Skin color
        float r = PlayerPrefs.HasKey("SkinRed") ? PlayerPrefs.GetFloat("SkinRed") / 255.0f : chMesh.material.color.r;
        float g = PlayerPrefs.HasKey("SkinGreen") ? PlayerPrefs.GetFloat("SkinGreen") / 255.0f : chMesh.material.color.g;
        float b = PlayerPrefs.HasKey("SkinBlue") ? PlayerPrefs.GetFloat("SkinBlue") / 255.0f : chMesh.material.color.b;
        chMesh.material.color = new Color(r, g, b); 

        //Set sensiivity
        if (PlayerPrefs.HasKey("Settings->General->Sensitivity"))
            sensitivityMultiplier = PlayerPrefs.GetFloat("Settings->General->Sensitivity");

        //crouchMultiplier state
        currentCrouch = previousCrouch = inputManager.Crouch;
        HUDController.instance.CrouchStand(currentCrouch);
    }

    void Update()
    {
        if(!HUDController.instance.isPaused)
        {
            UpdateMovementInput();
            UpdatePhysics();        
            UpdateWeapon(); 
        }
    } 

    private void UpdateMovementInput()
    {
        //Movement
        Vector3 forward = inputManager.Forward ? transform.forward : inputManager.Back ? -1 * transform.forward : Vector3.zero;
        Vector3 sideway = inputManager.Left ? -1 * transform.right : inputManager.Right ? transform.right : Vector3.zero;
        Vector3 combinedInput = (forward + sideway).normalized;
        movementDir = new Vector3(combinedInput.x, 0f, combinedInput.z);

        Look();

        //Jump
        ySpeed += Physics.gravity.y * Time.deltaTime * 2.5f; //Gravity 
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask) || characterController.isGrounded;
        if (isGrounded && inputManager.Jump && !inputManager.Crouch)
        {
            ySpeed = jumpForce * -2.0f * Physics.gravity.y;
            animator.SetTrigger(animController.JumpHash);
        }
        movementDir.y = ySpeed;

        //Crouch/Stand UI
        currentCrouch = inputManager.Crouch;
        if (currentCrouch != previousCrouch)
        {
            HUDController.instance.CrouchStand(currentCrouch);
            previousCrouch = currentCrouch;
        }
    }

    private void UpdateWeapon()
    {
        isAiming = (isGrounded && !IsRunning) ? inputManager.IsAiming : false;
        if (!isReloading)
        {
            //Switch guns with numbers
            for (int i = 0; i < items.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    EquipItem(i); 
                    break;
                }
            }
            //Switch guns with scroll wheel
            if (inputManager.SwitchWeaponUp)
            {
                if (itemIndex >= (items.Length - 1))
                    EquipItem(0);
                else
                    EquipItem(itemIndex + 1);
            }
            if (inputManager.SwitchWeaponDown)
            {
                if (itemIndex <= 0)
                    EquipItem(items.Length - 1);
                else
                    EquipItem(itemIndex - 1);
            }
        }
          
        //Fire
        FireWeapon();
        TryToReloadWeapon();

        //Update UI
        Gun gun = ((Gun)items[itemIndex]);
        string ammoInMagazine = gun.currentAmmoInMagazine.ToString();
        string ammoAvailable = gun.ammoAvailable > 999 ? "\u221E" : gun.ammoAvailable.ToString();
        if(itemIndex == 2)
            HUDController.instance.ammunitionText.text = string.Empty;
        else
            HUDController.instance.ammunitionText.text = ammoInMagazine + "/" + ammoAvailable;
    }


    private void EquipItem(int _index)
    {
        if (_index == previousItemIndex) //safe check 
            return;

        //Set current index
        itemIndex = _index;

        //Set current gun to true
        foreach (var item in items[itemIndex].itemObject)
        {
            item.SetActive(true); 
        }

        //Set the previous gun to false
        if (previousItemIndex != -1)
        {
            foreach (var item in items[previousItemIndex].itemObject)
            {
                item.SetActive(false);
            } 
        }
        previousItemIndex = itemIndex;

        if (items[itemIndex].TryGetComponent<MeleeWeapon>(out var melee))
            hasKnife = true;
        else
            hasKnife = false;

        //Change left arm target
        ik.StartCoroutine(ik.ChangeLeftArmTarget(((GunInfo)items[itemIndex].itemInfo).leftHandTarget));

        //Update UI
        HUDController.instance.SelectWeapon(itemIndex); 
    }

    private void FireWeapon()
    {
        //Check rifle, handgun and knife
        if(inputManager.FireSingleShot && !isReloading && (items[itemIndex].TryGetComponent<SingleShotGun>(out var ssg) ))
        {
            if (items[itemIndex].Use())  
                animator.SetTrigger(animController.FireHash);
        }
        else if (inputManager.AutomaticShot && !isReloading && items[itemIndex].TryGetComponent<AutomaticGun>(out var ag))
        {
            if(items[itemIndex].Use())
                animator.SetTrigger(animController.FireHash);
        }
        else if(inputManager.FireSingleShot && items[itemIndex].TryGetComponent<MeleeWeapon>(out var melee))
        {
            if (items[itemIndex].Use())
                animator.SetTrigger(animController.StabHash); 
        }
        else
        {
            isFiring = false;
            isFiringSingleShot = false; 
        }
    }

    private void TryToReloadWeapon()
    {
        if (!isReloading && inputManager.Reload && items[itemIndex].CanReload())
        {
            isReloading = true;
            animator.SetTrigger(animController.ReloadHash);

            if (items[itemIndex].TryGetComponent<SingleShotGun>(out var ssg))
                SoundManager.instance.PlaySound(SoundManagerConstants.Clips.RELOAD_HANDGUN, SoundManagerConstants.AudioOutput.SFX, gameObject);
            else
                SoundManager.instance.PlaySound(SoundManagerConstants.Clips.RELOAD_RIFLE, SoundManagerConstants.AudioOutput.SFX, gameObject);
        }
    }

    public void ReloadWeapon()
    {
        isReloading = false;
        items[itemIndex].Reload();
    }

    private void UpdatePhysics()
    {
        float multiplier = (IsRunning ? movementSpeed * speedMultiplier : currentCrouch ? movementSpeed * crouchMultiplier : movementSpeed);
        Vector3 movement = new Vector3(movementDir.x * multiplier, movementDir.y, movementDir.z * multiplier);

        //Update velocity
        characterController.Move(movement * Time.deltaTime);
    }

    private void Look()
    {
        float sensitivity = isAiming ? (aimSensitivity * sensitivityMultiplier * ADSsensitivityMultiplier) : (aimSensitivity * sensitivityMultiplier);

        //Rotate player 
        transform.Rotate(Vector3.up * inputManager.XLookAxis * sensitivity);

        //Rotate camera
        verticalLookRotation += inputManager.YLookAxis * sensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -70.0f, 70f);
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }
}
