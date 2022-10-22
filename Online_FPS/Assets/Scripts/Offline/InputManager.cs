using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{ 
    //Movement Input
    protected bool forward;
    public bool Forward { get { return forward; } }
    protected bool left; 
    public bool Left { get { return left; } }
    protected bool right;
    public bool Right { get { return right; } }
    protected bool back;
    public bool Back { get { return back; } }
    protected bool run;
    public bool Run { get { return run; } }
    protected bool crouch;
    public bool Crouch { get { return crouch; } }
    private bool wasLastInputC;
    protected bool jump;
    public bool Jump { get { return jump; } }

    //Action Keys
    protected bool isAiming;
    public bool IsAiming { get { return isAiming; } }
    protected bool fireSingleShot;
    public bool FireSingleShot { get { return fireSingleShot; } }
    protected bool automaticShot;
    public bool AutomaticShot { get { return automaticShot; } }
    protected bool reload;
    public bool Reload { get { return reload; } }
    private string scrollWheelAxis = "Mouse ScrollWheel";
    protected bool switchWeaponUp;
    public bool SwitchWeaponUp { get { return switchWeaponUp; } } 
    protected bool switchWeaponDown;
    public bool SwitchWeaponDown { get { return switchWeaponDown; } }
    protected bool firstWeapon;
    public bool FirstWeapoon { get { return firstWeapon; } }
    protected bool secondWeapon;
    public bool SecondWeapoon { get { return secondWeapon; } }
    protected bool thirdWeapoon;
    public bool ThirdWeapoon { get { return thirdWeapoon; } }

    //UI keys 
    protected bool pause;
    public bool Pause { get { return pause; } }
    protected float horizontalUI;
    public float HorizontalUI { get { return horizontalUI; } }
    protected float verticalUI;
    public float VerticalUI { get { return verticalUI; } }

    //Camera Axis
    private string verticalLookAxis = "Mouse Y";
    private string horizontalLookAxis = "Mouse X";
    
    protected float xAxis;
    public float XLookAxis { get { return xAxis; } }
    protected float yAxis;
    public float YLookAxis { get { return yAxis; } }


    private void Update()
    {
        HandleInput();
    }
    

    protected void HandleInput()
    {
        //Movement
        forward = Input.GetKey(KeyCode.W);
        left = Input.GetKey(KeyCode.A);
        back = Input.GetKey(KeyCode.S);
        right = Input.GetKey(KeyCode.D);
        run = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.C))
        {
            crouch = !crouch;
            wasLastInputC = true;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            crouch = true;
            wasLastInputC = false;
        }
        else if (!Input.GetKey(KeyCode.LeftControl) && !wasLastInputC)
            crouch = false; 
        jump = Input.GetKeyDown(KeyCode.Space);
         
        //Actions
        isAiming = Input.GetKey(KeyCode.Mouse1);
        reload = Input.GetKeyDown(KeyCode.R);
        switchWeaponUp = Input.GetAxisRaw(scrollWheelAxis) > 0.0f;
        switchWeaponDown = Input.GetAxisRaw(scrollWheelAxis) < 0.0f;
        firstWeapon = Input.GetKeyDown(KeyCode.Alpha1);
        secondWeapon = Input.GetKeyDown(KeyCode.Alpha2);
        thirdWeapoon = Input.GetKeyDown(KeyCode.Alpha3);
        fireSingleShot = Input.GetMouseButtonDown(0);
        automaticShot = Input.GetMouseButton(0);

        //UI
        pause = Input.GetKeyDown(KeyCode.Escape);
        horizontalUI = Input.GetAxis("HorizontalUI");
        verticalUI = Input.GetAxis("VerticalUI");

        //Camera rotate
        xAxis = Input.GetAxisRaw(horizontalLookAxis);
        yAxis = Input.GetAxisRaw(verticalLookAxis);
    }
}
