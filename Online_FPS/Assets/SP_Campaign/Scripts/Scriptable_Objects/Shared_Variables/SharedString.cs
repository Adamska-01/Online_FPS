using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Shared Variables/Shared String", fileName = "New Shared String")]
public class SharedString : ScriptableObject, ISerializationCallbackReceiver, ISharedVariableCallbackReceiver
{
    public event Action OnVariableAssigned;

    //Inspector-Assigned
    [SerializeField, TextArea(3, 10)] private string value = null;

    //Internal
    private string runtimeValue = null;

    //Setter
    public string Value { get { return runtimeValue; } set { runtimeValue = value; OnVariableAssigned?.Invoke(); } }


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