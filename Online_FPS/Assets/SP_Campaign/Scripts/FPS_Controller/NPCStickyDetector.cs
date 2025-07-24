using FPS.Utility;
using UnityEngine;


public class NPCStickyDetector : MonoBehaviour
{
    private CapsuleCollider col;

    private FPS_Controller fpsController;

	private CharacterController chrController;


    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        fpsController = GetComponentInParent<FPS_Controller>();
		chrController = GetComponentInParent<CharacterController>();

        Guard.AgainstNull(col, nameof(col));
        Guard.AgainstNull(fpsController, nameof(fpsController));
        Guard.AgainstNull(chrController, nameof(chrController));
    }

    void Start()
    {
        if (col == null)
            return;

        col.radius = chrController.radius;
        col.height = chrController.height;
    }


    private void OnTriggerStay(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if (fpsController != null && machine != null)
        {
            fpsController.DoStickiness();

            //Set visual threat on NPC (zombie)
            machine.visualThreat.Set(AITargetType.Visual_Player, chrController, fpsController.transform.position, Vector3.Distance(machine.transform.position, fpsController.transform.position));
            machine.SetStateOverride(AIStateType.Attack); //Attack player (avoid zombie going in alerted state)
        }
    }
}
