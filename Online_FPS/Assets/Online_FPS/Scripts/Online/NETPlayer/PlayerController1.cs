using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController1 : MonoBehaviourPunCallbacks
{
    private PhotonView pv;
    private Rigidbody rb;

    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    private float verticalLookRotation;
    private bool grounded;
    private Vector3 smoothMoveVelocity;
    private Vector3 moveAmount;

    //Guns
    [SerializeField] Item[] items;
    int itemIndex;
    int previousItemIndex = -1;

     
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();
         
        if (pv.IsMine)
        {
            EquipItem(0); //Equip first item in the array
        }
        else //Destroy the cameras and RBs (of the other players in the game) if the pv is not the local player
        { 
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
        }
    }
     

    void Update()
    {
        if (!pv.IsMine) //Return if this is not the local user 
            return;

        Look();
        Move();
        Jump();

        //Switch guns with numbers
        for (int i = 0; i < items.Length; i++)
        {
            if(Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }
        //Switch guns with scroll wheel
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0.0f)
        {
            if (itemIndex >= (items.Length - 1))
                EquipItem(0);
            else
                EquipItem(itemIndex + 1);
        }
        if (Input.GetAxisRaw("Mouse ScrollWheel") < 0.0f)
        {
            if (itemIndex <= 0)
                EquipItem(items.Length - 1);
            else
                EquipItem(itemIndex - 1);
        }
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine) //Return if this is not the local user 
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }


    private void Look()
    {
        //Y camera rotation
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        //X camera rotation
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90.0f, 90.0f);
        //Rotate camera on the X
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    private void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);
        }
    }

    private void EquipItem(int _index)
    {
        //if (_index == previousItemIndex) //safe check 
        //    return;

        ////Set current index
        //itemIndex = _index;

        ////Set current gun to true
        //items[itemIndex].itemObject.SetActive(true);

        ////Set the previous gun to false
        //if(previousItemIndex != -1)
        //{
        //    items[previousItemIndex].itemObject.SetActive(false);
        //}
        //previousItemIndex = itemIndex;

        //if (pv.IsMine)
        //{
        //    Hashtable hash = new Hashtable();
        //    hash.Add("itemIndex", itemIndex);
        //    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        //}
    }

    public void SetGrounded(bool _grnd)
    {
        grounded = _grnd;
    }

    //----------------------Photon Callbacks----------------------
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {//Called every time a custom property updates 
        if(pv && !pv.IsMine && targetPlayer == pv.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }
}
