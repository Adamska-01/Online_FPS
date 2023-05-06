using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractiveItem : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] protected int priority = 0;

    //Properties
    public int Priority { get { return priority; } }

    //Private/Protected
    protected GameSceneManager gameSceneManager = null;
    protected Collider col = null;


    //Methods
    public virtual string GetText()
    {
        return null;
    }
    public virtual void Activate(CharacterManager _chrManager) 
    {
    }


    protected virtual void Start()
    {
        gameSceneManager = GameSceneManager.Instance;
        col = GetComponent<Collider>();

        if(gameSceneManager != null && col != null)
        {
            gameSceneManager.RegisterInteractiveItem(col.GetInstanceID(), this);
        }
    }
}
