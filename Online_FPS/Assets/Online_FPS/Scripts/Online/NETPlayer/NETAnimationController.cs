using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NETAnimationController : MonoBehaviour
{
    [SerializeField] private NETPlayerController player;
    private Animator animator;
    private NETInputManager inputManager;
    private NETPlayerStats stats;
    private PhotonView pv;

    //Blend tree variables
    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;
    public float acceleration = 0.1f;
    public float deceleration = 0.5f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;
    //Private Hash
    private int XvelocityHash;
    private int ZvelocityHash;
    private int crouchHash;
    //Public Hash
    private int jumpHash;
    public int JumpHash { get { return jumpHash; } }
    private int reloadHash;
    public int ReloadHash { get { return reloadHash; } }
    private int fireHash;
    public int FireHash { get { return fireHash; } }
    private int deathHash;
    public int DeathHash { get { return deathHash; } }
    private int stabHash;
    public int StabHash { get { return stabHash; } }

    //Crouch lerp
    [SerializeField] private Transform cameraTansf;
    [SerializeField] private Transform cameraStandTarget;
    [SerializeField] private Transform cameraCrouchTarget;
    [SerializeField] private Transform HeadEffector;
    [SerializeField] private Transform headEffectorStandTarget;
    [SerializeField] private Transform headEffectorCrouchTarget;
    private float transitionCrouch = 0.0f;
    private float transitionCrouchTime = 5.0f;
    //Aim lerp 
    private float transitionADS = 0.0f;
    private float transitionADSTime = 10.0f;
    [SerializeField] private Transform ADSrightHandTarget;
    [SerializeField] private Transform NoADSrightHandTarget;
    [SerializeField] private Transform rightHandTarget; 
    [SerializeField] private Camera mainCamera;
    private float adsOffFov = 60.0f;
    private float adsOnFov = 40.0f;


    void Start()
    {
        stats = GetComponent<NETPlayerStats>();
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
        inputManager = FindObjectOfType<NETInputManager>(); 

        //Set Hash
        XvelocityHash = Animator.StringToHash("VelocityX");
        ZvelocityHash = Animator.StringToHash("VelocityZ");
        crouchHash = Animator.StringToHash("IsCrouched");
        jumpHash = Animator.StringToHash("Jump");
        reloadHash = Animator.StringToHash("Reloading");
        fireHash = Animator.StringToHash("Fire");
        deathHash = Animator.StringToHash("IsDead");
        stabHash = Animator.StringToHash("Stab");
    }


    void Update()
    {
        if (!pv.IsMine)
            return;

        if (stats.IsDead())
            return;

        //Stop animation when paused
        if(NETUIController.instance.isPaused || MatchManager.instance.state == MatchManager.GameStates.Ending)
        {
            ChangeVelocity(false, false, false, false, false, maximumWalkVelocity);
            LockOrResetVelocity(false, false, false, false, false, maximumWalkVelocity);
            animator.SetFloat(XvelocityHash, velocityX);
            animator.SetFloat(ZvelocityHash, velocityZ);
            return;
        }

        //Get key input from player
        bool forwardPressed = inputManager.Forward;
        bool leftPressed = inputManager.Left;
        bool rightPressed = inputManager.Right;
        bool backPressed = inputManager.Back;
        bool runPressed = inputManager.Run;

        //set current maxVelocity (running)
        float currentMaxVelocity = ((runPressed && !backPressed) && !inputManager.Crouch) ? maximumRunVelocity : maximumWalkVelocity;

        //Crouch lerp
        if (inputManager.Crouch)
        {
            transitionCrouch = Mathf.Lerp(transitionCrouch, 1, Time.smoothDeltaTime * transitionCrouchTime);
        }
        else
        {
            transitionCrouch = Mathf.Lerp(transitionCrouch, 0, Time.smoothDeltaTime * transitionCrouchTime);
        }
        cameraTansf.position = Vector3.Lerp(cameraStandTarget.position, cameraCrouchTarget.position, transitionCrouch);
        HeadEffector.position = Vector3.Lerp(headEffectorStandTarget.position, headEffectorCrouchTarget.position, transitionCrouch);

        //ADS lerp
        if (player.isAiming)
        {
            transitionADS = Mathf.Lerp(transitionADS, 1, Time.smoothDeltaTime * transitionADSTime);
        }
        else
        {
            transitionADS = Mathf.Lerp(transitionADS, 0, Time.smoothDeltaTime * transitionADSTime);
        }
        if (player.hasKnife)
            transitionADS = 0;
        //FOV
        mainCamera.fieldOfView = Mathf.Lerp(adsOffFov, adsOnFov, transitionADS);
        rightHandTarget.position = Vector3.Lerp(NoADSrightHandTarget.position, ADSrightHandTarget.position, transitionADS);
        rightHandTarget.rotation = Quaternion.Lerp(NoADSrightHandTarget.rotation, ADSrightHandTarget.rotation, transitionADS);

        ChangeVelocity(forwardPressed, leftPressed, rightPressed, backPressed, runPressed, currentMaxVelocity);
        LockOrResetVelocity(forwardPressed, leftPressed, rightPressed, backPressed, runPressed, currentMaxVelocity);

        //Update animation
        animator.SetFloat(XvelocityHash, velocityX);
        animator.SetFloat(ZvelocityHash, velocityZ);
        animator.SetBool(crouchHash, inputManager.Crouch); 
    }

    //Check for input and assign velocities 
    private void ChangeVelocity(bool _forwardPressed, bool _leftPressed, bool _rightPressed, bool _backPressed, bool _runPressed, float _currentMaxVelocity)
    {
        if (_forwardPressed && velocityZ < _currentMaxVelocity) //forward
        {
            velocityZ += Time.deltaTime * acceleration;
        }
        if (_leftPressed && velocityX > -_currentMaxVelocity) //left
        {
            velocityX -= Time.deltaTime * acceleration;
        }
        if (_rightPressed && velocityX < _currentMaxVelocity) //right
        {
            velocityX += Time.deltaTime * acceleration;
        }
        if (_backPressed && velocityZ > -_currentMaxVelocity) //Back
        {
            velocityZ -= Time.deltaTime * acceleration;
        }

        //decrese velocityZ
        if (!_forwardPressed && velocityZ > .0f)
        {
            velocityZ -= Time.deltaTime * deceleration;
        }
        //increase velocityX if left is not pressed and velocityX < 0
        if (!_leftPressed && velocityX < .0f)
        {
            velocityX += Time.deltaTime * deceleration;
        }
        //decrease velocityX if right is not pressed and velocityX > 0
        if (!_rightPressed && velocityX > .0f)
        {
            velocityX -= Time.deltaTime * deceleration;
        }
        //Decrease velocityZ if back is not pressed and velocityZ < 0
        if (!_backPressed && velocityZ < .0f)
        {
            velocityZ += Time.deltaTime * deceleration;
        }
    }

    private void LockOrResetVelocity(bool _forwardPressed, bool _leftPressed, bool _rightPressed, bool _backPressed, bool _runPressed, float currentMaxVelocity)
    {
        //reset velocityZ (safe check)
        if (!_forwardPressed && !_backPressed && velocityZ != .0f && (velocityZ > -.1f && velocityZ < .1f))
        {
            velocityZ = .0f;
        }
        //reset velocityX (safe check)
        if (!_leftPressed && !_rightPressed && velocityX != .0f && (velocityX > -.1f && velocityX < .1f))
        {
            velocityX = .0f;
        }

        //lock forward
        if (_forwardPressed && _runPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ = currentMaxVelocity;
        }
        //decelerate to the maximum walk veloxity
        else if (_forwardPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            //round to the currentMaxVel if within offset
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + .05f))
            {
                velocityZ = currentMaxVelocity;
            }
        }
        else if (_forwardPressed && velocityZ < currentMaxVelocity && velocityZ > currentMaxVelocity - .05f)
        {
            velocityZ = currentMaxVelocity;
        }

        //lock left
        if (_leftPressed && _runPressed && velocityX < -currentMaxVelocity)
        {
            velocityX = -currentMaxVelocity;
        }
        //decelerate to the maximum walk veloxity
        else if (_leftPressed && velocityX < -currentMaxVelocity)
        {
            velocityX += Time.deltaTime * deceleration;
            //round to the currentMaxVel if within offset
            if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity - .05f))
            {
                velocityX = -currentMaxVelocity;
            }
        }
        else if (_leftPressed && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity + .05f))
        {
            velocityX = -currentMaxVelocity;
        }

        //lock right
        if (_rightPressed && _runPressed && velocityX > currentMaxVelocity)
        {
            velocityX = currentMaxVelocity;
        }
        //decelerate to the maximum walk veloxity
        else if (_rightPressed && velocityX > currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * deceleration;
            //round to the currentMaxVel if within offset
            if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + .05f))
            {
                velocityX = currentMaxVelocity;
            }
        }
        else if (_rightPressed && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - .05f))
        {
            velocityX = currentMaxVelocity;
        }

        //lock back
        if (_backPressed && velocityZ < -currentMaxVelocity)
        {
            velocityZ += Time.deltaTime * deceleration;
            //round to the currentMaxVel if within offset
            if (velocityZ < -currentMaxVelocity && velocityZ > (-currentMaxVelocity - .05f))
            {
                velocityZ = -currentMaxVelocity;
            }
        }
        else if (_backPressed && velocityZ > -currentMaxVelocity && velocityZ < (-currentMaxVelocity + .05f))
        {
            velocityZ = -currentMaxVelocity;
        }
    }

    public bool IsReloadComplete()
    {
        return animator.GetCurrentAnimatorStateInfo(3).IsName("Reloading");
    }

    /// <summary>
    /// Footstep sounds
    /// </summary>
    
    //Normal footsteps
    public void PlayLeftFootstep()
    {
        SoundManager.instance.PlaySound(SoundManagerConstants.Clips.LEFT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
    }

    public void PlayRightFootstep()
    {
        SoundManager.instance.PlaySound(SoundManagerConstants.Clips.RIGHT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
    }

    public void PlayLeftFootstepRun()
    {
        SoundManager.instance.PlaySound(SoundManagerConstants.Clips.LEFT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
    }

    public void PlayRightFootstepRun()
    {
        SoundManager.instance.PlaySound(SoundManagerConstants.Clips.RIGHT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
    }

    //walk strafe
    public void PlayStrafeRightFootstepLeft()
    {
        if (velocityX >= 0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.LEFT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        }
    }
    public void PlayStrafeRightFootstepRight()
    {
        if (velocityX >= 0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIGHT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }

    public void PlayStrafeLeftFootstepLeft()
    {
        if (velocityX <= -0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.LEFT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        }
        
    } 
    public void PlayStrafeLeftFootstepRight()
    {
        if (velocityX <= -0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIGHT_FOOTSTEP, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }

    //run strafe
    public void PlayStrafeRightFootstepLeftRun()
    {
        if (velocityX >= 0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.LEFT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }
    public void PlayStrafeRightFootstepRightRun()
    {
        if (velocityX >= 0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIGHT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }

    public void PlayStrafeLeftFootstepLeftRun()
    {
        if (velocityX <= -0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.LEFT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }
    public void PlayStrafeLeftFootstepRightRun()
    {
        if (velocityX <= -0.49f && velocityZ == 0.0f)
        {
            AudioSource source = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIGHT_FOOTSTEP_RUN, SoundManagerConstants.AudioOutput.SFX, transform.position, 0.8f);
            source.maxDistance = 6.0f;
        } 
    }
}
