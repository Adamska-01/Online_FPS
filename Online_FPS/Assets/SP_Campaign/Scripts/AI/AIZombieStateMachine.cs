using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --------------------------------------------------------------------------
// CLASS	:	AIZombieStateMachine
// DESC		:	State Machine used by zombie characters
// --------------------------------------------------------------------------
public class AIZombieStateMachine : AIStateMachine
{
    //Inspector Assigned 
    [Header("Zombie Characteristics")]
    [SerializeField, Range(10.0f, 360.0f)]  private float   fov             = 50.0f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   sight           = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   hearing         = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   aggression      = 0.5f;
    [SerializeField, Range(0, 100)]         private int     health          = 100;
    [SerializeField, Range(0.0f, 1.0f)]     private float   intelligence    = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)]     private float   satisfaction    = 1.0f;

    //Private (Managed by the state machine) 
    private int     seeking     = 0;
    private bool    feeding     = false;
    private bool    crawling    = false;
    private int     attackType  = 0;

    // Hashes
    private int speedHash   = Animator.StringToHash("Speed");
    private int seekingHash = Animator.StringToHash("Seeking");
    private int feedingHash = Animator.StringToHash("Feeding");
    private int attackHash  = Animator.StringToHash("Attack");

    // Public Properties
    public float	Fov		 	{ get { return fov; } }
	public float	Hearing	 	{ get { return hearing; } }
	public float    Sight		{ get { return sight; } }
	public bool 	Crawling	{ get { return crawling; } }
	public float	Intelligence{ get { return intelligence; } }
	public float	Satisfaction{ get { return satisfaction; }	set { satisfaction = value; } }
	public float	Aggression	{ get { return aggression; }	set { aggression = value; } }
	public int		Health		{ get { return health; }		set { health = value; } }
	public int		AttackType	{ get { return attackType; }	set { attackType = value; } }
	public bool		Feeding		{ get { return feeding; }		set { feeding = value; } }
	public int		Seeking		{ get { return seeking; }		set { seeking = value; } }
	public float	Speed    	
	{ 
		get	{ return (navAgent != null) ? navAgent.speed : 0.0f; }
		set	{ if (navAgent != null) navAgent.speed = value;	}
	}

    protected override void Update()
    {
        base.Update();

        //Set animator parameters
        if (anim != null)
        {
            anim.SetFloat(speedHash, navAgent.speed);
            anim.SetInteger(seekingHash, seeking);
            anim.SetBool(feedingHash, feeding);
            anim.SetInteger(attackHash, attackType);
        }
    }
}
