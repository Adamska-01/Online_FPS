using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragCollider : MonoBehaviour
{
    private CapsuleCollider col;
    private FPS_Controller fpsController;
    private CharacterController chrController;

    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        fpsController = transform.root.GetComponentInChildren<FPS_Controller>();
        chrController = transform.root.GetComponentInChildren<CharacterController>();
    }

    void Start()
    {
        if (col == null || chrController == null)
            return;

        col.radius = chrController.radius;
        col.height = chrController.height;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fpsController == null)
            return;

        if (GameSceneManager.Instance.GetAIStateMachine(other.GetComponent<Collider>().GetInstanceID()) != null)
        {
            fpsController.DragMultiplier = 1.0f - fpsController.NPCStickines;
            Debug.Log(fpsController.DragMultiplier);
        }
    }
}
