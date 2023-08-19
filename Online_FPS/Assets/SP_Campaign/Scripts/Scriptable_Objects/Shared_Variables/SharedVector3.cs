using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Shared Variables/Shared Vector3", fileName = "New Shared Vector3")]
public class SharedVector3 : ScriptableObject, ISerializationCallbackReceiver, ISharedVariableCallbackReceiver
{
    public event Action OnVariableValueChanged; //ISharedVariableCallbackReceiver


    //Inspector-Assigned
    [SerializeField] private Vector3 value = Vector3.zero;
    private Vector3 runtimeValue = Vector3.zero;

    //Properties
    public Vector3 Value { get { return runtimeValue; } set { runtimeValue = value; OnVariableValueChanged?.Invoke(); } }
    public float x { get { return runtimeValue.x; } set { runtimeValue.x = value; OnVariableValueChanged?.Invoke(); } }
    public float y { get { return runtimeValue.y; } set { runtimeValue.y = value; OnVariableValueChanged?.Invoke(); } }
    public float z { get { return runtimeValue.z; } set { runtimeValue.z = value; OnVariableValueChanged?.Invoke(); } }


    //-------------------------------------------------------------------
    //-------------------------- Serialization --------------------------
    //-------------------------------------------------------------------
    public void OnAfterDeserialize()
    {
        //When pressing play, the scene is 'destroyed' (serialized) in the C++ layer
        //and then 'Rebuilt' (desirialized). At that point this function is called,
        //allowing to set the internal value to the value serialized in the inspector
        runtimeValue = value;
    }
    public void OnBeforeSerialize() { } //Not needed
}
