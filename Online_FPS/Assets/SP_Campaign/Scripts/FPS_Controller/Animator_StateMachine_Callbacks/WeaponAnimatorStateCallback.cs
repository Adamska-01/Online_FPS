using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


/// <summary>
/// Describes a single muzzle flash in a sequence of muzzle flashes.
/// </summary>
[System.Serializable]
public class MuzzleFlashDescriptor
{
	public GameObject muzzleFlash = null;
	public float lightIntensity = 1.0f;
	public Color lightColor = Color.white;
	public float Range = 10.0f;
}


/// <summary>
/// Base weapon callback implementation providing a common interface for activating 
/// muzzle flashes.
/// </summary>
public class WeaponAnimatorStateCallback : AnimatorStateCallback
{
	// Inspector-Assigned
	public Light muzzleFlashLight = null;
	public List<MuzzleFlashDescriptor> muzzleFlashFrames = new List<MuzzleFlashDescriptor>();
	public float muzzleFlashTime = 0.1f;
	public int muzzleFlashesPerShot = 1;

	// Internal
	protected int currentMuzzleFlashIndex = 0;
	protected int lightReferenceCount = 0;

	protected virtual void OnEnable()
	{
		lightReferenceCount = 0;
		currentMuzzleFlashIndex = 0;
	}

	protected virtual void OnDisable()
	{
		if (muzzleFlashLight != null)
		{
			muzzleFlashLight.gameObject.SetActive(false);
		}

		for (int i = 0; i < muzzleFlashFrames.Count; i++)
		{
			muzzleFlashFrames[i].muzzleFlash?.SetActive(false);
		}
	}


	public void DoMuzzleFlash()
	{
		if (muzzleFlashesPerShot < 1)
			return;

		if (muzzleFlashesPerShot > 1)
		{
			StartCoroutine(EnableMuzzleFlashSequence());
		}
		else
		{
			EnableMuzzleFlash();
		}
	}

    protected void EnableMuzzleFlash()
    {
		if(muzzleFlashFrames.Count > 0 && muzzleFlashFrames[currentMuzzleFlashIndex] != null)
		{
			MuzzleFlashDescriptor frame = muzzleFlashFrames[currentMuzzleFlashIndex];

			frame.muzzleFlash?.SetActive(true);

			if(muzzleFlashLight != null)
			{
				muzzleFlashLight.color = frame.lightColor; 
				muzzleFlashLight.intensity = frame.lightIntensity; 
				muzzleFlashLight.range = frame.Range; 
				muzzleFlashLight.gameObject.SetActive(true); 
			}

			lightReferenceCount++;

			StartCoroutine(DisableMuzzleFlash(currentMuzzleFlashIndex));

			currentMuzzleFlashIndex++;
			currentMuzzleFlashIndex = currentMuzzleFlashIndex >= muzzleFlashFrames.Count ? 0 : currentMuzzleFlashIndex;
		}
    }

    protected IEnumerator EnableMuzzleFlashSequence()
    {
		int counter = 0;
		float timer = float.MaxValue;

		while (counter < muzzleFlashesPerShot)
		{
			timer += Time.deltaTime;
			if(timer > muzzleFlashTime)
			{
				EnableMuzzleFlash();
				counter++;
				timer = 0.0f;
			}

			yield return null;
		}
	}

    protected IEnumerator DisableMuzzleFlash(int _flashIndex)
    {
		yield return new WaitForSeconds(muzzleFlashTime);

		muzzleFlashFrames[_flashIndex].muzzleFlash.SetActive(false);

		lightReferenceCount--;

		if(lightReferenceCount <= 0 && muzzleFlashLight != null)
		{
			muzzleFlashLight.gameObject.SetActive(false);
		}
    }
}
