using UnityEngine;


public class ArmsBaseSMB : StateMachineBehaviour
{
    // Inspector-Assigned
    public ScriptableObject identifier = null;
    
    [HideInInspector] public CharacterManager characterMgr = null;
    [HideInInspector] public AnimatorStateCallback callbackHandler = null;
}
