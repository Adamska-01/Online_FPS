using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ArmsObject
{
	public ScriptableObject identifier = null;
	public List<GameObject> sceneObjects = new List<GameObject>();
	public Flashlight light = new Flashlight();
	public List<AnimatorStateCallback> callbacks = new List<AnimatorStateCallback>();
	public Transform crosshairPosition = null;
	public Transform crosshairPositionDualMode = null;


	// Helper functions
	public void SetAllSceneObjectsActiveState(bool isActive)
	{
		foreach (GameObject obj in sceneObjects) 
		{ 
			obj.SetActive(isActive);
		}
	}
}