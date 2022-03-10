using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunBehavior : WeaponBehavior
{
	public GameObject barrelObject;
	public GameObject bulletPrefab;
	//public float cooldown = 0.5f;
	public bool isAutomatic = false;
    public string ammoType = "bullet";
    public int startingAmmoCount = 10;
	// Use this for initialization

	public override void Fire()
	{
        
        if (!isCooling)
        {
            base.Fire();
            if (GameManager.player.GetComponent<GAME1304PlayerController>().ConsumeAmmo(ammoType))
            {
                GameObject bulletObj = GameObject.Instantiate(bulletPrefab, barrelObject.transform);
                bulletObj.transform.parent = null;

                if (bulletObj.GetComponent<BulletBehavior>() != null)
                    bulletObj.GetComponent<BulletBehavior>().init(barrelObject.transform.forward);
                isCooling = true;
                cooldownTimer = cooldown; //TODO: Make this take place in the parent so it's not at risk for individual interpretation
            }
        }
		//bulletPrefab
	}
}
