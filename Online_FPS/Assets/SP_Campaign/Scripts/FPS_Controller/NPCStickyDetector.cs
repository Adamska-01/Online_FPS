using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCStickyDetector : MonoBehaviour
{
    private CapsuleCollider col;
    private FPS_Controller fpsController;


    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        fpsController = GetComponentInParent<FPS_Controller>();
    }

    void Start()
    {
        if (col == null)
            return;

        col.radius = fpsController.CHRController.radius;
        col.height = fpsController.CHRController.height;
    }


    private void OnTriggerStay(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if (fpsController != null && machine != null)
        {
            fpsController.DoStickiness();

            Debug.Log("player");
            //Set visual threat on NPC (zombie)
            machine.visualThreat.Set(AITargetType.Visual_Player, fpsController.CHRController, fpsController.transform.position, Vector3.Distance(machine.transform.position, fpsController.transform.position));
            machine.SetStateOverride(AIStateType.Attack); //Attack player (avoid zombie going in alerted state)
        }
    }
}
