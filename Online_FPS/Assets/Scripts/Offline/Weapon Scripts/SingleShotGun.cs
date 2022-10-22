using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam; 
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
            player.isFiringSingleShot = true;

            //Start ray from center of screen
            Vector2 recoil = player.isAiming ? Vector2.zero : Recoil();
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f + recoil.x, 0.5f + recoil.y));
            ray.origin = cam.transform.position + (cam.transform.forward * 0.7f);

            //Raycast
            GameObject bullet = null;
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.DrawLine(cam.transform.position, hit.point);
                bullet = Instantiate(bulletPrefab, bulletStart.position, Quaternion.LookRotation(hit.point - bulletStart.transform.position));
            }
            else
            {
                Debug.DrawLine(cam.transform.position, hit.point);
                bullet = Instantiate(bulletPrefab, bulletStart.position, Quaternion.LookRotation(cam.transform.forward));
            }

            //Effect
            Instantiate(effectPrefab, bulletStart.position, Quaternion.LookRotation(cam.transform.forward));

            //Sound
            AudioSource audioSource = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.RIFLE_SHOOT, SoundManagerConstants.AudioOutput.SFX, bulletStart.position, 0.1f);
            audioSource.maxDistance = 23.0f;

            //Assign damages
            bullet.GetComponent<Bullet>().SetDamages(((GunInfo)itemInfo).damageHead, ((GunInfo)itemInfo).damageBody, ((GunInfo)itemInfo).damageLeg);

            return true;
        }
        else if (canShootNextBullet)
        {
            StartCoroutine(FireRateDelay());
            AudioSource src = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.EMPTY_CLIP_HANDGUN, SoundManagerConstants.AudioOutput.SFX, gameObject, 0.1f);
            src.spatialBlend = 0.0f;
            src.priority = 256;
        }

        return false;
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
