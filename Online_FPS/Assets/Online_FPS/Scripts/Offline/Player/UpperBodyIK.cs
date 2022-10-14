using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using Photon.Pun;

public class UpperBodyIK : MonoBehaviour
{ 
    [Header("Final IK Modules")] 
    [SerializeField] private ArmIK leftArmIK;
    [SerializeField] private ArmIK rightArmIK;
    [SerializeField] private FullBodyBipedIK fbbIK;
    [SerializeField] private Transform leftHandTarget;
      
    private bool isIKActive = true;
    private bool isFBBIKActive = true;
     

    void Start()
    {
        //Set the right UI controller
        if(PhotonNetwork.IsConnected)
        {
            var NETUIcontroller = NETUIController.instance; 
            if(NETUIcontroller != null && !NETUIcontroller.isPaused)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        else
        {
            var UIcontroller = HUDController.instance;
            if (UIcontroller != null && !UIcontroller.isPaused)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        //Disable by default, and update manually
        rightArmIK.enabled = false;
        leftArmIK.enabled = false;
        fbbIK.enabled = false; 
    }

    void Update()
    {
        if (isFBBIKActive) fbbIK.solver.FixTransforms();
        if(isIKActive)
        {
            rightArmIK.solver.FixTransforms();
            leftArmIK.solver.FixTransforms();  
        }
    }

    void LateUpdate()
    {
        if (isFBBIKActive) FBBIKUpdate();
        if(isIKActive)
        {
            ArmsIKUpdate();
        }
    } 


    private void ArmsIKUpdate()
    { 
        rightArmIK.solver.Update();
        leftArmIK.solver.Update();
    }

    private void FBBIKUpdate()
    {
        fbbIK.solver.Update(); 
    }

    public IEnumerator ChangeLeftArmTarget(Transform _t)
    {
        leftHandTarget.localPosition = new Vector3(_t.position.x, _t.position.y, _t.position.z);
        leftHandTarget.localRotation = new Quaternion(_t.rotation.x, _t.rotation.y, _t.rotation.z, _t.rotation.w);
        yield return null;
        //float elapsedTime = 0;
        //float waitTime = 0.7f;
        //Vector3 currentPos = leftHandTarget.position;
        //Quaternion currentRot = leftHandTarget.rotation;

        //while (elapsedTime < waitTime)
        //{
        //    leftHandTarget.localPosition = Vector3.Lerp(currentPos, new Vector3(_t.position.x, _t.position.y, _t.position.z), (elapsedTime / waitTime));
        //    leftHandTarget.localRotation = Quaternion.Lerp(currentRot, new Quaternion(_t.rotation.x, _t.rotation.y, _t.rotation.z, _t.rotation.w), (elapsedTime / waitTime));

        //    elapsedTime += Time.deltaTime;
        //    yield return null;
        //}
    }


    //-------------------------Getter and Setter----------------------------- 
    public void ActivateIK() => isIKActive = true;
    public void DeactivateIK() => isIKActive = false;
    public void SetIK(bool _state) => isIKActive = _state;
    public void SetFBBIK(bool _state) => isFBBIKActive = _state;
}
