using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NETMeleeWeapon : NETGun
{
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
        if (canShootNextBullet)
        {
            StartCoroutine(FireRateDelay());

            //Start ray from center of screen
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            ray.origin = cam.transform.position;

            GetComponent<PhotonView>().RPC("RPC_KnifeSound", RpcTarget.All, ray.origin);

            //Raycast
            if (Physics.Raycast(ray, out RaycastHit hit, 1.4f))
            {
                if (hit.collider.gameObject.TryGetComponent<HitboxPlayer>(out var hitbox))
                {
                    //Hit indicator to the victim's client
                    GetComponent<PhotonView>().RPC("RPC_KnifeHitIndicator", RpcTarget.All, transform.root.GetComponent<PhotonView>().ViewID, hitbox.transform.root.GetComponent<PhotonView>().ViewID);

                    //Damage entity
                    switch (hitbox.colType)
                    {
                        case HitboxPlayer.CollisionType.BODY:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageBody, GetComponent<PhotonView>().Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber);
                            break;
                        case HitboxPlayer.CollisionType.HEAD:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageHead, GetComponent<PhotonView>().Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber);
                            break;
                        case HitboxPlayer.CollisionType.LEG:
                            hitbox.TakeDamage(((GunInfo)itemInfo).damageLeg, GetComponent<PhotonView>().Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber);
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

    [PunRPC] private void RPC_KnifeSound(Vector3 _pos)
    {
        //Sound
        AudioSource audioSource = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.KNIFE_SWING, SoundManagerConstants.AudioOutput.SFX, cam.transform.position, 0.15f);
        audioSource.maxDistance = 4.0f;
    }

    [PunRPC] private void RPC_KnifeHitIndicator(int _idDamager, int _idDamaged)
    {
        PhotonView[] pvs = FindObjectsOfType<PhotonView>();
        PhotonView pvDamaged = pvs.ToList().Find(x => x.ViewID == _idDamaged);

        PhotonView pvDamager = transform.root.GetComponent<PhotonView>();

        if (pvDamaged.ViewID == _idDamaged && pvDamaged.IsMine)
        { 
            if (!DI_System.CheckIfObjectInSight(pvDamager.transform.root))
                DI_System.CreateIndicator(pvDamager.transform.root);
        } 
    }
}
