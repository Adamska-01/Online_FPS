using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

        //Copy starting game states into the game 
        foreach (GameState state in startingGameStates)
        {
            gameStateDictionary.Add(state.key, state.value);
        }

        SetGameState("fesa", "deasfc");
    }

    private void Start()
    {
        
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
}
