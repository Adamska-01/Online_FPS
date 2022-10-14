using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NETPlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private Collider[] ragdollColliders;
    private CharacterController characterController;
    private PhotonView pv;
    private PlayerManager playerManager;
    private UpperBodyIK ik;

    //max values
    public const float MAX_HEALTH_VALUE = 100.0f;
    public const float MAX_ARMOUR_VALUE = 50.0f;

    //health
    private float HealthValue;
    //Stamina
    private float ArmourValue;
    //State
    private bool isDead;


    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        characterController = GetComponent<CharacterController>();
        ik = GetComponent<UpperBodyIK>();
        ToggleRagdoll(false);

        playerManager = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    void Start()
    { 
        pv = GetComponent<PhotonView>();
         
        //Set values 
        HealthValue = MAX_HEALTH_VALUE;
        ArmourValue = MAX_ARMOUR_VALUE;
        //Set UI
        NETUIController.instance.healthText.text = HealthValue.ToString();
        NETUIController.instance.armourText.text = ArmourValue.ToString();
    }
     

    void FixedUpdate()
    {
        updateUI();
    }


    private void updateUI()
    {
        if (!pv.IsMine)
            return;

        HealthValue = Mathf.Clamp(HealthValue, 0, MAX_HEALTH_VALUE);
        ArmourValue = Mathf.Clamp(ArmourValue, 0, MAX_ARMOUR_VALUE);

        NETUIController.instance.healthText.text = HealthValue.ToString();
        NETUIController.instance.armourText.text = ArmourValue.ToString();
    }

    public void TakeDamage(float damage, string _damager, int _actor)
    {
        pv.RPC("RPC_TakeDamage", RpcTarget.All, damage, _damager, _actor);
    }

    [PunRPC] private void RPC_TakeDamage(float damage, string _damager, int _actor)
    {
        //Make sure this runs only on the victim's client
        if (!pv.IsMine)
            return;

        if (!isDead)
        { 
            if (ArmourValue > 0.0f)
            {
                ArmourValue -= damage;
                if (ArmourValue < 0.0f)
                    HealthValue += ArmourValue;
            }
            else
                HealthValue -= damage;

            if (HealthValue <= 0)
            { 
                isDead = true;

                //Die on other clients
                pv.RPC("RPC_Die", RpcTarget.All, _actor); 

                playerManager.Die(_damager); 

                MatchManager.instance.UpdateStatsSend(_actor, 0, 1, pv.Owner.NickName);
            }  
        }
    }

    [PunRPC] private void RPC_Die(int _actor)
    {
        //Sound
        AudioSource audioSource = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.DEATH, SoundManagerConstants.AudioOutput.SFX, transform.position);
        audioSource.maxDistance = 4.0f;

        //Death ragdoll
        PhotonView[] pvs = FindObjectsOfType<PhotonView>();
        PhotonView damagerPv = pvs.ToList().Find(x => x.Owner.ActorNumber == _actor);
        ToggleRagdoll(true);
        foreach (Rigidbody item in ragdollBodies)
        {
            item.AddExplosionForce(30.0f, damagerPv.transform.position, 5.0f, 0.0f, ForceMode.Impulse);
        }
    }

    public void Heal(float value)
    {
        if (!isDead)
        {
            HealthValue += value;
            updateUI();
        }
    }

    public bool CanHeal()
    {
        return HealthValue < MAX_HEALTH_VALUE;
    }

    private void ToggleRagdoll(bool _state)
    {
        animator.enabled = !_state;

        foreach (Rigidbody item in ragdollBodies)
        {
            item.isKinematic = !_state;
        }

        foreach (Collider item in ragdollColliders)
        {
            item.enabled = _state;
        }

        ik.SetIK(!_state);
        ik.SetFBBIK(!_state);
        if (characterController != null) characterController.enabled = !_state;
    }


    //----------------------Getter and Setter---------------------------- 
    public void SetIsDead(bool value)
    {
        isDead = value;
    }

    public void SetSanity(float value)
    {
        HealthValue = value;
    }

    public void SetStamina(float value)
    {
        ArmourValue = value;
    }

    public float GetHealth()
    {
        return HealthValue;
    }

    public float GetArmour()
    {
        return ArmourValue;
    }

    public bool IsDead()
    {
        return isDead;
    } 
}
