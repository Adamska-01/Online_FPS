using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[System.Serializable]
public class GameState
{
    public string key = null;
    public string value = null;
}

public class ApplicationManager : MonoBehaviour
{
    //Singleton
    private static ApplicationManager instance = null;
    public static ApplicationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApplicationManager>();
            }

            return instance;
        }
    }

    //Current States
    private Dictionary<string, string> gameStateDictionary = new Dictionary<string, string>();
    
    //States you wish to set at game startup
    [SerializeField] private List<GameState> startingGameStates = new List<GameState>();


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        ResetGameStates();
    }


    public string GetGameState(string _key)
    {
        gameStateDictionary.TryGetValue(_key, out string result);
        
        return result;    
    }

    public bool SetGameState(string _key, string _val)
    {
        if (_key == null || _val == null)
            return false;

        gameStateDictionary[_key] = _val;

        return true;
    }

    private void ResetGameStates()
    {
        gameStateDictionary.Clear();

        //Copy starting game states into the game 
        foreach (GameState state in startingGameStates)
        {
            if(gameStateDictionary.ContainsKey(state.key))
            {
                gameStateDictionary[state.key] = state.value;
            }
            else
            {
                gameStateDictionary.Add(state.key, state.value);
            }
        }
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("SP_MainMenu");
    }

    public void LoadGame()
    {
        ResetGameStates();
        SceneManager.LoadScene("SP_Game");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
