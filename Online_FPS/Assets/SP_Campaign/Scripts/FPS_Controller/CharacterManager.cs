using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] private CapsuleCollider meleeTrigger = null;
    [SerializeField] private CameraBloodEffect cameraBloodEffect = null;
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private float health = 100.0f;

    //Private
    private Collider col= null;
    private FPS_Controller fpsController = null;
    private CharacterController characterController = null;
    private GameSceneManager gameSceneManger = null;
    private int AI_BodyPartLayer = -1;

    void Start()
    {
        col = GetComponent<Collider>();
        fpsController = GetComponent<FPS_Controller>();
        characterController = GetComponent<CharacterController>();
        gameSceneManger = GameSceneManager.Instance;
        AI_BodyPartLayer = LayerMask.NameToLayer("AI_BodyPart");

        if(gameSceneManger != null)
        {
            Player_Info info = new Player_Info();
            info.camera = playerCamera;
            info.characterManager = this;
            info.collider = col;
            info.meleeTrigger = meleeTrigger;

            //Register this player
            gameSceneManger.RegisterPlayerInfo(col.GetInstanceID(), info);
        }
    }


    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            DoDamage();
        }
    }


    public void TakeDamage(float dmg)
    {
        health = Mathf.Max(health - (dmg * Time.deltaTime), 0.0f);
        if (cameraBloodEffect != null)
        {
            //Apply blood on screen (but 3 times less than what should be)
            cameraBloodEffect.MinBloodAmount = 1.0f - (health / 100.0f);
            cameraBloodEffect.BloodAmount = Mathf.Min(cameraBloodEffect.MinBloodAmount + 0.3f, 1.0f);
        }
    }

    public void DoDamage(int _hitDir = 0)
    {
        if (playerCamera == null || gameSceneManger == null)
            return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        //Perform raycast from the crosshair (screen center)
        ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << AI_BodyPartLayer);

        if(isSomethingHit)
        {
            AIStateMachine stateMachine = gameSceneManger.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if(stateMachine != null)
            {
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 50, hit.rigidbody, this, 0);
            }
        }
    }
}
