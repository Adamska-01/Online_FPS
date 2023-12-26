using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable OBJ/Shared Variables/Shared Sprite", fileName = "New Shared Sprite")]
public class SharedSprite : ScriptableObject, ISerializationCallbackReceiver, ISharedVariableCallbackReceiver
{
    public event Action OnVariableValueChanged;

    // Inspector-Assigned
    [SerializeField] private Sprite value = null;

    // Internal
    private Sprite runtimeValue = null;

    // Setter
    public Sprite Value { get { return runtimeValue; } set { runtimeValue = value; OnVariableValueChanged?.Invoke(); } }


    // -------------------------------------------------------------------
    // -------------------------- Serialization --------------------------
    // -------------------------------------------------------------------
    public void OnAfterDeserialize()
    {
        runtimeValue = value;
    }

    public void OnBeforeSerialize()
    {
    }
}
