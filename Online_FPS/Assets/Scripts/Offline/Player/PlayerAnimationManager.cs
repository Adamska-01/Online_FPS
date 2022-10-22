using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [SerializeField] private InputManager inputManager = default;
    private Vector3 movementDir;
    private float inputAmount;

    [SerializeField] private Rigidbody rb = default;
    [SerializeField] private Rigidbody headTargetRigidbody = default;
    [SerializeField] private CapsuleCollider col = default;
    [SerializeField] private float offsetFloorY = 0.4f;
    [SerializeField] private float movementSpeed = 3f;

    private Vector3 raycastFloorPos;
    private Vector3 combinedRaycast;
    private Vector3 gravity;
    private Vector3 floorMovement;
    private float groundRayLenght;

    [SerializeField] private Animator m_animator = default; 

    private void FixedUpdate()
    {
        UpdateMovementInput();
        UpdatePhysics();
        UpdateAnimation();
    }


    private void UpdateMovementInput()
    {
        //Vector3 forward = inputManager.Forward * transform.forward;
        //Vector3 sideway = inputManager.Sideway * transform.right;
        //Vector3 combinedInput = (forward + sideway).normalized;

        //movementDir = Vector3.zero;
        //movementDir = new Vector3(combinedInput.x, 0f, combinedInput.z);
        
        //float inputMagnitude = Mathf.Abs(inputManager.Forward) + Mathf.Abs(inputManager.Sideway);

        ////Clamp to keep between 0 and 1 (for blend tree)
        //inputAmount = Mathf.Clamp01(inputMagnitude);
    }
    
    private void UpdatePhysics()
    {
        //Set the raycast length to half collider + custom offset
        groundRayLenght = (col.height * 0.5f) + offsetFloorY; 
        if (FloorRaycasts(0, 0, groundRayLenght).transform == null)
        {
            gravity += (Vector3.up * Physics.gravity.y * Time.fixedDeltaTime);
        }

        //Update velocity
        rb.velocity = (movementDir * movementSpeed * inputAmount) + gravity;
        headTargetRigidbody.velocity = rb.velocity;

        //Adjust rigid body position so that the player is at the correct height
        floorMovement = new Vector3(rb.position.x, FindFloor().y, rb.position.z); 
        if (FloorRaycasts(0, 0, groundRayLenght).transform != null && floorMovement != rb.position)
        {
            rb.MovePosition(floorMovement);
            gravity.y = 0;
        }
    }

    private Vector3 FindFloor()
    {
        //Determine the average point of the floor between the 5 raycasts
        float raycastWidth = 0.25f;
        int floorAverage = 1;
        combinedRaycast = FloorRaycasts(0, 0, groundRayLenght).point;
        floorAverage += (GetFloorAverage(raycastWidth, 0) + GetFloorAverage(-raycastWidth, 0) + GetFloorAverage(0, raycastWidth) + GetFloorAverage(0, -raycastWidth));
        return combinedRaycast / floorAverage;
    }

    private RaycastHit FloorRaycasts(float t_offsetx, float t_offsetz, float t_raycastLength)
    {
        RaycastHit hit;

        raycastFloorPos = transform.TransformPoint(0.0f + t_offsetx, col.center.y, 0.0f + t_offsetz);
        
        Debug.DrawRay(raycastFloorPos, Vector3.down * groundRayLenght, Color.magenta);
        
        Physics.Raycast(raycastFloorPos, -Vector3.up, out hit, t_raycastLength);

        return hit;
    }

    private int GetFloorAverage(float t_offsetx, float t_offsetz)
    {
        if (FloorRaycasts(t_offsetx, t_offsetz, groundRayLenght).transform != null)
        {
            combinedRaycast += FloorRaycasts(t_offsetx, t_offsetz, groundRayLenght).point;
            return 1;
        }
        else 
            return 0; 
    }

    private void UpdateAnimation()
    {
        //m_animator.SetFloat("Forward", inputManager.Forward);
        //m_animator.SetFloat("Sideway", inputManager.Sideway);
    }
}
