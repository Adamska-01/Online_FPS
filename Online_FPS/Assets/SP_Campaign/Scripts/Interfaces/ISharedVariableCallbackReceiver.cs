using System;


// --------------------------------------------------------------------------------
// INTERFACE :	ISharedVariableCallbackReceiver
// DESC		 :	Interface containing an action event that can be used in shared 
//              variables for update purposes.
// --------------------------------------------------------------------------------
public interface ISharedVariableCallbackReceiver
{
    public event Action OnVariableValueChanged;
}

