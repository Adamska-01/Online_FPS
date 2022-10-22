using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Gun
{
    [SerializeField] Camera cam; 
     
    public override bool Use()
    {
        return Hit();
    }

    public override bool CanReload()
    {
        //No need to reload
        return false;
    }
     
    public override void Reload()
    { }

    private bool Hit()
    {
        if(canShootNextBullet)
        {
            StartCoroutine(FireRateDelay()); 

            //Start ray from center of screen
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            ray.origin = cam.transform.position;

            //Sound
            AudioSource audioSource = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.KNIFE_SWING, SoundManagerConstants.AudioOutput.SFX, cam.transform.position, 0.15f);
            audioSource.maxDistance = 4.0f;

            //Raycast
            if (Physics.Raycast(ray, out RaycastHit hit, 1.4f))
            { 
                //Damage entity
                if (hit.collider.gameObject.TryGetComponent<HitboxPlayer>(out var hitbox))
                {
                    switch (hitbox.colType)
                    {
                        case HitboxPlayer.CollisionType.BODY:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageBody);
                            break;
                        case HitboxPlayer.CollisionType.HEAD:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageHead);
                            break;
                        case HitboxPlayer.CollisionType.LEG:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageLeg);
                            break;
                    }
                }
                 
                //Impact and holes 
                ImpactsAndHoles impactsAndHoles = FindObjectOfType<ImpactsAndHoles>();
                switch (hit.collider.tag)
                {
                    case "Concrete": 
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.CONCRETE].hit, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    case "Dirt": 
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.DIRT].hit, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    case "Metal": 
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.METAL].hit, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    case "Sand": 
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.SAND].hit, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    case "Wood": 
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.WOOD].hit, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    case "Body":
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.BODY].hit, hit.point, Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                    default:
                        Instantiate(impactsAndHoles.GetBulletsAndImpacts()[ImpactsAndHoles.ImpactType.CONCRETE].hit, hit.point, Quaternion.LookRotation(hit.normal, Vector3.up));
                        break;
                }
            }

            return true;
        }

        return false;
    }
}
