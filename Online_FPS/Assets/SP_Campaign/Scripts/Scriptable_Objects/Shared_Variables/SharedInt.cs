using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Shared Variables/Shared Int", fileName = "New Shared Int")]
public class SharedInt : ScriptableObject, ISerializationCallbackReceiver, ISharedVariableCallbackReceiver
{
    public event Action OnVariableValueChanged;

    //Inspector-Assigned
    [SerializeField] private int value = 0;
    
    //Internal
    private int runtimeValue = 0;

    //Setter
    public int Value { get { return runtimeValue; } set { runtimeValue = value; OnVariableValueChanged?.Invoke(); } }



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
