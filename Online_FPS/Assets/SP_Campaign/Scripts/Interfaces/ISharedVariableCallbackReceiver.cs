using System;

public interface ISharedVariableCallbackReceiver
{
    public event Action OnVariableAssigned;
}

