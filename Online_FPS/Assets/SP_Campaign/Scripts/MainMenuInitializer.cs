using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuInitializer : MonoBehaviour
{
    [SerializeField] private Button PlayButton = null;
    [SerializeField] private Button QuitButton = null;


    private void Awake()
    {
        //Hide Curesor and unlock 
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        //Bind buttons 
        if (ApplicationManager.Instance != null)
        {
            PlayButton?.onClick.AddListener(ApplicationManager.Instance.LoadGame);
            QuitButton?.onClick.AddListener(ApplicationManager.Instance.QuitGame);
        }
    }
}
