using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMoveStatus { NotMoving, Walking, Running, NotGrounded, Landing }

[RequireComponent(typeof(CharacterController))]
public class FPS_Controller : MonoBehaviour
{
    [Header("Locomotion Settings")]
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private float stickToGroundForce = 5.0f;
    [SerializeField] private float gravityMultiplier = 2.5f;

    //Takes care of mouse look
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;

    //Private internals
    private Camera  cam              = null;
    private bool    jumpButtonPressed   = false;
    private Vector2 inputVector         = Vector2.zero;
    private Vector3 moveDirection       = Vector3.zero;
    private bool    previouslyGrounded  = false;
    private bool    isWalking           = true;
    private bool    isJumping           = false;

    //Timers
    private float fallingTimer = 0.0f;

    private CharacterController characterController = null;
    private PlayerMoveStatus movementStatus = PlayerMoveStatus.NotMoving;

    //Public Properties 
    public PlayerMoveStatus MovementStatus { get { return movementStatus; } }
    public float WalkSpeed { get { return walkSpeed; } }
    public float RunSpeed { get { return runSpeed; } }


    protected void Start()
    {
        //Cache components references
        characterController = GetComponent<CharacterController>();

        cam = Camera.main;

        movementStatus = PlayerMoveStatus.NotMoving;

        fallingTimer = 0.0f;

        //init mouse look
        mouseLook.Init(transform, cam.transform);
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
        if(Time.deltaTime > Mathf.Epsilon)
        {
            mouseLook.LookRotation(transform, cam.transform);
        }

        //Process jump
        if(!jumpButtonPressed)
        {
            jumpButtonPressed = Input.GetButtonDown("Jump");
        }

        //Calculate character status 
        if(!previouslyGrounded && characterController.isGrounded)
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
            movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (characterController.velocity.sqrMagnitude < 0.01f)
        {
            movementStatus = PlayerMoveStatus.NotMoving;
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
    }

    protected void FixedUpdate()
    {
        //Read input from axis
        float horizontal = Input.GetAxis("Horizontal");
        float veritical = Input.GetAxis("Vertical");

        bool wasWalking = isWalking;
        isWalking = !Input.GetKey(KeyCode.LeftShift);

        //Set the desired speed to be either our walking speed or our running speed
        float speed = isWalking ? walkSpeed : runSpeed;

        inputVector = new Vector2(horizontal, veritical);
        if (inputVector.sqrMagnitude > 1)
            inputVector.Normalize();

        //Always move along the camera forward as it is the direction that it is being aimed at
        Vector3 desiredMove = transform.forward * inputVector.y + transform.right * inputVector.x;

        //Get a normal of the surface that is being touched to move along it 
        //RaycastHit hitInfo;
        //if(Physics.SphereCast(transform.position, characterController.radius, Vector3.down, out hitInfo, characterController.height / 2.0f, 1)) //All layers
        //{// We are standing on a surface
        //    desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        //}

        //Scale movement by speed 
        moveDirection.x = desiredMove.x * speed;
        moveDirection.z = desiredMove.z * speed;
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
        }
        else //Apply standard gravity (not in the air)
        {
            moveDirection += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
        }

        //Move
        characterController.Move(moveDirection * Time.fixedDeltaTime);
    }
}
