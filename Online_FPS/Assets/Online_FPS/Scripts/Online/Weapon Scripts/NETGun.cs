using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NETGun : NETItem
{
    [SerializeField] protected NETPlayerController player;
    [SerializeField] protected NETRecoil recoil;
    public Camera cam;
    public GameObject bulletPrefab;
    public GameObject effectPrefab;
    //Clip info
    public int ammoAvailable;
    public int currentAmmoInMagazine;
    public int maxAmmoInMagazine;
    public bool canShootNextBullet;
    public bool CanShoot { get { return currentAmmoInMagazine > 0.0f && canShootNextBullet; } }


    void Start()
    {
        //Start with full reloaded weapon
        currentAmmoInMagazine = maxAmmoInMagazine;

        canShootNextBullet = true;
    }


    public abstract override bool Use();
     
    public override bool CanReload()
    {
        if (currentAmmoInMagazine < maxAmmoInMagazine && ammoAvailable > 0)
            return true;

        return false;
    }

    public override void Reload()
    {
        int availableSpaceInMagazine = maxAmmoInMagazine - currentAmmoInMagazine;
        int bulletsToAdd = Mathf.Min(availableSpaceInMagazine, ammoAvailable);

        ammoAvailable -= bulletsToAdd;
        currentAmmoInMagazine += bulletsToAdd;
    }

    protected Vector2 Recoil()
    {
        //Increase recoil
        recoil.recoilIntensityCounter += recoil.increaseRate;
        if (recoil.recoilIntensityCounter > recoil.recoilMaxIntensity)
            recoil.recoilIntensityCounter = recoil.recoilMaxIntensity;

        //Get recoil vec
        Vector2 offset = new Vector2(Random.Range(0, recoil.recoilIntensityCounter), Random.Range(0, recoil.recoilIntensityCounter));
        if (Random.Range(0.0f, 1.0f) < 0.5f)
            offset.x = -offset.x;

        return offset;
    }

    protected IEnumerator FireRateDelay()
    {
        canShootNextBullet = false;

        yield return new WaitForSeconds(((GunInfo)itemInfo).fireRate);

        canShootNextBullet = true;
    }
}
