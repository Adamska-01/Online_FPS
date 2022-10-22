using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public MenuManager.MenuType type;
    [HideInInspector] public bool isOpen;

    void Awake()
    {
        //Set isOpen info at the start
        isOpen = gameObject.activeSelf;
    }

    public void Open()
    { 
        isOpen = true;
        gameObject.SetActive(true); 
    }

    public void Close()
    { 
        isOpen = false;
        gameObject?.SetActive(false); 
    }
}
