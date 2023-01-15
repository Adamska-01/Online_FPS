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

    void Start()
    {
        col = GetComponent<Collider>();
        fpsController = GetComponent<FPS_Controller>();
        characterController = GetComponent<CharacterController>();
        gameSceneManger = GameSceneManager.Instance;

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
}
