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
    private Dictionary<int, AIStateMachine>  stateMachines     = new Dictionary<int, AIStateMachine>();
    private Dictionary<int, Player_Info>     playerInfos       = new Dictionary<int, Player_Info>();
    private Dictionary<int, InteractiveItem> interactiveItems  = new Dictionary<int, InteractiveItem>();

    //Properties 
    public ParticleSystem BloodParticles { get { return bloodParticles; } }


    //----------------------------------------------------------------------------
    //---------------------------- Register Functions ----------------------------
    //----------------------------------------------------------------------------
    public void RegisterAIStateMachine(int _key, AIStateMachine _stateMachine)
    {
        if(!stateMachines.ContainsKey(_key))
        {
            stateMachines.Add(_key, _stateMachine);
        }
    }
    public void RegisterPlayerInfo(int _key, Player_Info _playerInfo)
    {
        if (!playerInfos.ContainsKey(_key))
        {
            playerInfos.Add(_key, _playerInfo);
        }
    }
    public void RegisterInteractiveItem(int _key, InteractiveItem _interactiveItem)
    {
        if (!interactiveItems.ContainsKey(_key))
        {
            interactiveItems.Add(_key, _interactiveItem);
        }
    }

    //----------------------------------------------------------------------------
    //----------------------------- Getter Functions -----------------------------
    //----------------------------------------------------------------------------
    public AIStateMachine GetAIStateMachine(int _key)
    {
        stateMachines.TryGetValue(_key, out AIStateMachine stateMachine);
        
        return stateMachine;
    }
    public Player_Info GetPlayerInfo(int _key)
    {
        playerInfos.TryGetValue(_key, out Player_Info info);

        return info;
    }

    public InteractiveItem GetInteractiveItem(int _key)
    {
        interactiveItems.TryGetValue(_key, out InteractiveItem interactive);

        return interactive;
    }
}
