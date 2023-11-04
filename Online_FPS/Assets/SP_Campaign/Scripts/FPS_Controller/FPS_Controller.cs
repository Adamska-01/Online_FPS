using UnityEngine;


public enum PlayerMoveStatus 
{ 
    NotMoving, 
    Crouching, 
    Walking, 
    Running,
    NotGrounded,
    Landing 
}


[RequireComponent(typeof(CharacterController))]
public class FPS_Controller : MonoBehaviour
{
    //Constants
    public const float MAX_STAMINA = 100.0f;

    [Header("Sound Settings")]
    [SerializeField] private AudioCollection footsteps = null;
    [SerializeField] private float crouchVolumeAttenuation = 0.2f;

    [Header("Locomotion Settings")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 1.0f;
    [SerializeField] private float staminaDepletion = 5.0f;
    [SerializeField] private float staminaRecovery = 10.0f;
    [SerializeField] private float stickToGroundForce = 5.0f;
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private float runStepLengthen = 0.75f;
    [SerializeField] private CurveControlledBob headBob = new CurveControlledBob();
    
    [Header("Shared Variables")]
    [SerializeField] private SharedFloat stamina = null;
    [Header("Shared Variables - Broadcasters")]
    [SerializeField] private SharedVector3 broadcastPosition = null;
    [SerializeField] private SharedVector3 broadcastDirection = null;

    [Header("Others")]
    // Takes care of mouse look
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;
    [SerializeField, Range(0.0f, 1.0f)] private float npcStickiness = 0.5f;

    // Private internals
    private Camera  cam                   = null;
    private Vector2 inputVector           = Vector2.zero;
    private Vector3 moveDirection         = Vector3.zero;
    private Vector3 localSpaceCameraPos   = Vector3.zero;
    private bool    jumpButtonPressed     = false;
    private bool    previouslyGrounded    = false;
    private bool    isWalking             = true;
    private bool    isJumping             = false;
    private bool    isCrouching           = false;
    private bool    freezeMovement        = false;
    private float   controllerHeight      = 0.0f;
    private float   inAirTime             = 0.1f;
    private float   inAirCounter          = 0.0f;
    private float   dragMultiplier        = 1.0f;
    private float   dragMultiplierLimit   = 1.0f;
    private float   speedOverride         = 0.0f;

    // Timers
    private float fallingTimer = 0.0f;

    private CharacterController characterController = null;
    private PlayerMoveStatus movementStatus = PlayerMoveStatus.NotMoving;

    //Public Properties 
    public PlayerMoveStatus MovementStatus { get { return movementStatus; } }
    public CharacterController CHRController { get { return characterController; } }
    public float WalkSpeed           { get { return walkSpeed; } }
    public float RunSpeed            { get { return runSpeed; } }
    public float DragMultiplierLimit { get { return dragMultiplierLimit; } set { dragMultiplierLimit = Mathf.Clamp01(value); } }
    public float DragMultiplier      { get { return dragMultiplier; } set { dragMultiplier = Mathf.Min(value, dragMultiplierLimit); } }
    public float SpeedOverride       { get { return speedOverride; } set { speedOverride = value; } }
    public bool IsJumping            { get { return isJumping; } }
    public bool FreezeMovement       { get { return freezeMovement; } set { freezeMovement = value; } }



    protected void Awake()
    {
        //Cache components references
        characterController = GetComponent<CharacterController>();
        controllerHeight = characterController.height;
        
        cam = Camera.main;
    }

    protected void Start()
    {
        if (cam != null)
        {
            // Init headbob
            localSpaceCameraPos = cam.transform.localPosition;

            // Setup Mouse Look Script
            mouseLook.Init(transform, cam.transform);
        }

        movementStatus = PlayerMoveStatus.NotMoving;

        fallingTimer = 0.0f;

        // Initiate Head Bob Object
        headBob.Initialize();
        headBob.RegisterEventCallback(1.5f, PlayFootStepSound, CurveControlledBobCallbackType.Vertical);
    }

    protected void Update()
    {
        //If we are falling increment timer 
        if(characterController.isGrounded)
        {
            fallingTimer = 0.0f;
        }
        else
        {
            fallingTimer += Time.deltaTime;
        }
        
        //Process mouse look
        if(Time.deltaTime > Mathf.Epsilon && cam != null)
        {
            mouseLook.LookRotation(transform, cam.transform);
        }

        //Process jump
        if(!jumpButtonPressed && !isCrouching && characterController.isGrounded)
        {
            jumpButtonPressed = Input.GetButtonDown("Jump");
        }

        if(Input.GetButtonDown("Crouch"))
        {
            isCrouching = !isCrouching;
            characterController.height = isCrouching == true ? controllerHeight / 2.0f : controllerHeight;
        }

        //Calculate character status 
        if(!previouslyGrounded && characterController.isGrounded && inAirCounter > inAirTime)
        {
            if(fallingTimer > 0.5f)
            {
                //TODO: Play Landing Sound 
            }

            moveDirection.y = 0.0f;
            isJumping = false;
            movementStatus = PlayerMoveStatus.Landing;
        }
        else if(!characterController.isGrounded)
        {
            inAirCounter += Time.deltaTime;
            movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (characterController.velocity.sqrMagnitude < 0.01f)
        {
            movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if(isCrouching)
        {
            movementStatus = PlayerMoveStatus.Crouching;
        }
        else if (isWalking)
        {
            movementStatus = PlayerMoveStatus.Walking;
        }
        else 
        {
            movementStatus = PlayerMoveStatus.Running;
        }

        previouslyGrounded = characterController.isGrounded;

        //Calculate Stamina
        if(movementStatus == PlayerMoveStatus.Running) //Deplete
        {
            stamina.Value = Mathf.Max(stamina.Value - staminaDepletion * Time.deltaTime, 0.0f);
        }
        else //Recovery
        {
            stamina.Value = Mathf.Min(stamina.Value + staminaRecovery * Time.deltaTime, MAX_STAMINA);
        }

        dragMultiplier = Mathf.Min(dragMultiplier + Time.deltaTime, dragMultiplierLimit);
    }

    protected void FixedUpdate()
    {
        //Read input from axis
        float horizontal = Input.GetAxisRaw("Horizontal");
        float veritical = Input.GetAxisRaw("Vertical");

        //Is Running?
        isWalking = !Input.GetKey(KeyCode.LeftShift);

        //Set the desired speed to be either our walking speed or our running speed
        float speed = isCrouching ? crouchSpeed : isWalking ? walkSpeed : Mathf.Lerp(walkSpeed, RunSpeed, stamina.Value / MAX_STAMINA);
        
        if (speedOverride > 0.0f) // Apply speed override (if there is)
            speed = speedOverride;

        //Normalize input if less than 1
        inputVector = new Vector2(horizontal, veritical);
        if (inputVector.sqrMagnitude > 1)
            inputVector.Normalize();

        //Always move along the camera forward as it is the direction that it is being aimed at
        Vector3 desiredMove = transform.forward * inputVector.y + transform.right * inputVector.x;

        //Get a normal of the surface that is being touched to move along it 
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, characterController.radius, Vector3.down, out hitInfo, characterController.height / 2.0f, 1)) //All layers
        {// We are standing on a surface
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

        //Scale movement by speed 
        moveDirection.x = !freezeMovement ? desiredMove.x * speed * dragMultiplier : 0.0f;
        moveDirection.z = !freezeMovement ? desiredMove.z * speed * dragMultiplier : 0.0f;
        if(characterController.isGrounded)
        {
            //Apply severe down force to keep it stuck to the floor
            moveDirection.y = -stickToGroundForce;
            
            //Apply jump force if jumping 
            if(jumpButtonPressed)
            {
                moveDirection.y = jumpSpeed;
                jumpButtonPressed = false;
                isJumping = true;

                //TODO: Play Jump Sound
            }

            inAirCounter = 0.0f;
        }
        else //Apply standard gravity (not in the air)
        {
            moveDirection += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
            inAirCounter += Time.fixedDeltaTime;
        }

        //Move
        characterController.Move(moveDirection * Time.fixedDeltaTime);
        
        //Head bob
        Vector3 speedXZ = new Vector3(characterController.velocity.x, 0.0f, characterController.velocity.z);
        if(speedXZ.magnitude > 0.01f)
        {
            cam.transform.localPosition = localSpaceCameraPos + headBob.GetVectorOffset(speedXZ.magnitude * ((isCrouching || isWalking) ? 1.0f : runStepLengthen));
        }
        else //Not moving 
        {
            cam.transform.localPosition = localSpaceCameraPos;
        }

        //Update broadcasters
        broadcastPosition.Value = transform.position;
        broadcastDirection.Value = transform.forward;
    }

    private void PlayFootStepSound()
    {
        if (AudioManager.Instance != null && footsteps != null)
        {
            AudioClip soundToPlay;
            if(isCrouching)
            {
                soundToPlay = footsteps[1];
            }
            else
            {
                soundToPlay = footsteps[0];
            }

            //Play sound
            AudioManager.Instance.PlayOneShotSound(footsteps.AudioGroup, 
                                                    soundToPlay, 
                                                    transform.position, 
                                                    isCrouching ? crouchVolumeAttenuation * footsteps.Volume : footsteps.Volume, 
                                                    footsteps.SpatialBlend, 
                                                    footsteps.Priority);
        }
    }

    public void DoStickiness()
    {
        dragMultiplier = 1.0f - npcStickiness;
    }
}
