using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	GameSceneManager
// DESC		:	Singleton class that acts as the scene database 
// --------------------------------------------------------------------------
public class GameSceneManager : MonoBehaviour
{
    //Singleton
    private static GameSceneManager instance = null;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameSceneManager>();
            }

            return instance;
        }
    }

    //Inspector-Assigned
    [SerializeField] private ParticleSystem bloodParticles = null;
     
    //Private
    private Dictionary<int, AIStateMachine> stateMachines = new Dictionary<int, AIStateMachine>();

    //Properties 
    public ParticleSystem BloodParticles { get { return bloodParticles; } }


    public void RegisterAIStateMachine(int _key, AIStateMachine _sm)
    {
        if(!stateMachines.ContainsKey(_key))
        {
            stateMachines.Add(_key, _sm);
        }
    }

    public AIStateMachine GetAIStateMachine(int _key)
    {
        stateMachines.TryGetValue(_key, out AIStateMachine sm);
        
        return sm;
    }
}
