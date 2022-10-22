using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private Collider[] ragdollColliders;
    private CharacterController characterController;
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
       

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        ik = GetComponent<UpperBodyIK>();
        ToggleRagdoll(false);

        HealthValue = MAX_HEALTH_VALUE; 
        ArmourValue = MAX_ARMOUR_VALUE;

        HUDController.instance.healthText.text = HealthValue.ToString();
        HUDController.instance.armourText.text = ArmourValue.ToString();
    }
     
    void FixedUpdate()
    {
        updateUI(); 
    } 


    private void updateUI() 
    {
        HealthValue = Mathf.Clamp(HealthValue, 0, MAX_HEALTH_VALUE);
        ArmourValue = Mathf.Clamp(ArmourValue, 0, MAX_ARMOUR_VALUE);

        HUDController.instance.healthText.text = HealthValue.ToString();
        HUDController.instance.armourText.text = ArmourValue.ToString(); 
    }

    public void TakeDamage(float damage, string _damager, int _actor)
    {
        if (!isDead)
        {
            Debug.Log("Took Damage: " + damage);
            if(ArmourValue > 0.0f)
            {
                ArmourValue -= damage;
                if (ArmourValue < 0.0f)
                    HealthValue += ArmourValue;
            }
            else
                HealthValue -= damage;
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
        characterController.enabled = !_state;
    }


    //-------------------------Getter and Setter----------------------------- 
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
