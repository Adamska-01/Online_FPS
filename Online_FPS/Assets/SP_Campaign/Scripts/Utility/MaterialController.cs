using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialController
{
	// Inspector Assigned
	[SerializeField] protected Material material = null;

	[SerializeField] protected Texture diffuseTexture = null;
	[SerializeField] protected Color diffuseColor = Color.white;

	[SerializeField] protected Texture normalMap = null;
	[SerializeField] protected float normalStrength = 1.0f;

	[SerializeField] protected Texture emissiveTexture = null;
	[SerializeField] protected Color emissionColor = Color.black;
	[SerializeField] protected float emissionScale = 1.0f;

	// Private / Protected
	protected MaterialController backup = null; //Copy of the current material 
	protected bool started = false;

	// Property to fetch the underlying material
	public Material Material { get { return material; } }


	public void OnStart()
	{
		if (material == null || started) //Don't run more than once
			return;

		started = true;
		backup = new MaterialController();

		// Backup settings in a temp controller
		backup.diffuseColor = material.GetColor("_Color");
		backup.diffuseTexture = material.GetTexture("_MainTex");
		backup.emissionColor = material.GetColor("_EmissionColor");
		backup.emissionScale = 1; //Not a shader variable (emissive color is already scaled)
		backup.emissiveTexture = material.GetTexture("_EmissionMap");
		backup.normalMap = material.GetTexture("_BumpMap");
		backup.normalStrength = material.GetFloat("_BumpScale");
		
		//Register this controller with the game scene manager using material instance ID. The GameScene manager will reset
		//all registered materials when the scene closes
		GameSceneManager.Instance.RegisterMaterialController(Material.GetInstanceID(), this);
	}

	public void Activate(bool _activate)
	{
		// Can't call this function until it's start has been called
		if (!started || Material == null) 
			return;

		// Set the material to the assigned properties
		if (_activate)
		{
			material.SetColor("_Color", diffuseColor);
			material.SetTexture("_MainTex", diffuseTexture);
			material.SetColor("_EmissionColor", emissionColor * emissionScale);
			material.SetTexture("_EmissionMap", emissiveTexture);
			material.SetTexture("_BumpMap", normalMap);
			material.SetFloat("_BumpScale", normalStrength);
		}
		else
		{
			material.SetColor("_Color", backup.diffuseColor);
			material.SetTexture("_MainTex", backup.diffuseTexture);
			material.SetColor("_EmissionColor", backup.emissionColor * backup.emissionScale);
			material.SetTexture("_EmissionMap", backup.emissiveTexture);
			material.SetTexture("_BumpMap", backup.normalMap);
			material.SetFloat("_BumpScale", backup.normalStrength);
		}
	}

	// ------------------------------------------------------------------------------------------------
	// Name	:	OnReset
	// Desc	:	Called to reset the material. This should be called only by the game scene manager
	//			otherwise you could overwrite the properties of your material asset
	// ------------------------------------------------------------------------------------------------
	public void OnReset()
	{

		if (backup == null || material == null) return;

		material.SetColor("_Color", backup.diffuseColor);
		material.SetTexture("_MainTex", backup.diffuseTexture);
		material.SetColor("_EmissionColor", backup.emissionColor * backup.emissionScale);
		material.SetTexture("_EmissionMap", backup.emissiveTexture);
		material.SetTexture("_BumpMap", backup.normalMap);
		material.SetFloat("_BumpScale", backup.normalStrength);
	}

	// ------------------------------------------------------------------------------------------------
	// Name	:	GetInstanceID
	// Desc	:	Returns the instance ID of the managed material
	// ------------------------------------------------------------------------------------------------
	public int GetInstanceID()
	{
		if (Material == null) 
			return -1;
		
		return Material.GetInstanceID();
	}

}

