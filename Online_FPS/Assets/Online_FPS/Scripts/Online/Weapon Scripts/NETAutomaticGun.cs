using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NETAutomaticGun : NETGun
{
    public Transform bulletStart;


    public override bool Use()
    { 
        return Shoot();
    }

    private bool Shoot()
    {
        if (CanShoot)
        {
            AmmoConsumption();

            StartCoroutine(FireRateDelay());

            //Set state (for animation)
            player.isFiring = true;

            //Start ray from center of screen
            Vector2 recoil = player.isAiming ? Vector2.zero : Recoil();
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f + recoil.x, 0.5f + recoil.y));
            ray.origin = cam.transform.position + (cam.transform.forward * 0.8f);
             
            //Raycast
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GetComponent<PhotonView>().RPC("RPC_ShootBullet", RpcTarget.All, bulletStart.position, Quaternion.LookRotation(hit.point - bulletStart.transform.position), GetComponent<PhotonView>().Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GetComponent<PhotonView>().RPC("RPC_ShootBullet", RpcTarget.All, bulletStart.position, Quaternion.LookRotation(cam.transform.forward), GetComponent<PhotonView>().Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber); 
            } 

            return true; //successfully shot bullet
        }
        else if (canShootNextBullet)
        {
            StartCoroutine(FireRateDelay());
            AudioSource src = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.EMPTY_CLIP_RIFLE, SoundManagerConstants.AudioOutput.SFX, gameObject, 0.1f);
            src.spatialBlend = 0.0f;
            src.priority = 256;
        }

        return false; //Could not shoot
    }

    [PunRPC] private void RPC_ShootBullet(Vector3 _pos, Quaternion _rot, string _damager, int _actor)
    {
        NETBullet projectile = Instantiate(bulletPrefab, _pos, _rot).GetComponent<NETBullet>();
        //Assign damages and other stuff
        projectile.SetDamages(((GunInfo)itemInfo).damageHead, ((GunInfo)itemInfo).damageBody, ((GunInfo)itemInfo).damageLeg);
        projectile.pv = GetComponent<PhotonView>();
        projectile.bulletOwnerTransf = transform.root;
        projectile.bulletOwner = _damager;
        projectile.actorNumber = _actor;

        //Effect
        Instantiate(effectPrefab, _pos, Quaternion.LookRotation(cam.transform.forward));

        //Sound
        AudioSource audioSource = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIFLE_SHOOT, SoundManagerConstants.AudioOutput.SFX, _pos, 0.1f);
        audioSource.maxDistance = 30.0f;
    }

    private void AmmoConsumption()
    {
        currentAmmoInMagazine -= 1;
    }

    public override bool CanReload()
    {
        return base.CanReload();
    }

    public override void Reload()
    {
        base.Reload();
    }
}
