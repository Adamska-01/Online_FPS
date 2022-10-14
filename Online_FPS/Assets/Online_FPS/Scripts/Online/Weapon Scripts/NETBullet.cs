using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NETBullet : MonoBehaviour
{
    private float velocity;
    private float damageHead, damageBody, damageLeg;
    private ImpactsAndHoles impactsAndHoles;
    [HideInInspector] public PhotonView pv;
    [HideInInspector] public string bulletOwner; 
    [HideInInspector] public Transform bulletOwnerTransf;
    [HideInInspector] public int actorNumber;

     
    void Start()
    {
        velocity = 180.0f;
        StartCoroutine("DestroySelf");
        impactsAndHoles = FindObjectOfType<ImpactsAndHoles>(); 
    }


    void Update()
    {
        Vector3 movement = transform.forward * velocity * Time.deltaTime;
        float distance = movement.magnitude;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, distance))
        { 
            //Deal damage if is a player
            if (hit.collider.gameObject.TryGetComponent<HitboxPlayer>(out var hitbox))
            { 
                //Deal damage only once, to the victim's client (owner only)
                if(hitbox.transform.root.GetComponent<PhotonView>().IsMine)
                {
                    if (!DI_System.CheckIfObjectInSight(bulletOwnerTransf))
                        DI_System.CreateIndicator(bulletOwnerTransf);

                    switch (hitbox.colType)
                    {
                        case HitboxPlayer.CollisionType.BODY:
                            hitbox.TakeDamage(damageBody, bulletOwner, actorNumber);
                            break;
                        case HitboxPlayer.CollisionType.HEAD:
                            hitbox.TakeDamage(damageHead, bulletOwner, actorNumber);
                            break;
                        case HitboxPlayer.CollisionType.LEG:
                            hitbox.TakeDamage(damageLeg, bulletOwner, actorNumber);
                            break;
                    } 
                }
            }

            //Single player shooting range difficulty select
            if (hit.collider.gameObject.TryGetComponent<DifficultyButton>(out var diffButton))
            {
                diffButton.SelectDifficulty(diffButton.diff);
            }
            //Single player start test buton
            if (hit.collider.gameObject.TryGetComponent<StartAndStopTest>(out var testButton))
            {
                testButton.StartOrStopTest();
            } 

            InstantiateImpactAndHole(hit);

            Destroy(this.gameObject, Time.deltaTime);
        }

        transform.position += movement;
    }

    private IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(3.0f);
        Destroy(gameObject);
    }

    public void SetDamages(float _head, float _body, float _leg)
    {
        damageHead = _head;
        damageBody = _body;
        damageLeg = _leg;
    }

    private void InstantiateImpactAndHole(RaycastHit _hit)
    {
        switch (_hit.collider.tag)
        {
            case "Concrete": 
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.CONCRETE].hole, _hit.point + (_hit.normal * 0.001f), Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            case "Dirt": 
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.DIRT].hole, _hit.point + (_hit.normal * 0.001f), Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            case "Metal": 
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.METAL].hole, _hit.point + (_hit.normal * 0.001f), Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            case "Sand": 
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.SAND].hole, _hit.point + (_hit.normal * 0.001f), Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            case "Wood": 
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.WOOD].hole, _hit.point + (_hit.normal * 0.001f), Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            case "Body":
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.BODY].hit, _hit.point, Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
            default:
                Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.CONCRETE].hole, _hit.point, Quaternion.LookRotation(_hit.normal, Vector3.up));
                break;
        }
    }
}
