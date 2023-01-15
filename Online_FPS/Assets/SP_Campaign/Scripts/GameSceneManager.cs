using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player_Info
{
    public Collider         collider            = null;
    public CharacterManager characterManager    = null;
    public Camera           camera              = null;
    public CapsuleCollider  meleeTrigger        = null;
}


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
    private Dictionary<int, Player_Info>     playerInfos   = new Dictionary<int, Player_Info>();

    //Properties 
    public ParticleSystem BloodParticles { get { return bloodParticles; } }


    public void RegisterAIStateMachine(int _key, AIStateMachine _sm)
    {
        if(!stateMachines.ContainsKey(_key))
        {
            stateMachines.Add(_key, _sm);
        }
    }
    public void RegisterPlayerInfo(int _key, Player_Info _pi)
    {
        if (!playerInfos.ContainsKey(_key))
        {
            playerInfos.Add(_key, _pi);
        }
    }

    public AIStateMachine GetAIStateMachine(int _key)
    {
        stateMachines.TryGetValue(_key, out AIStateMachine sm);
        
        return sm;
    }
    public Player_Info GetPlayerInfo(int _key)
    {
        playerInfos.TryGetValue(_key, out Player_Info sm);

        return sm;
    }
}
