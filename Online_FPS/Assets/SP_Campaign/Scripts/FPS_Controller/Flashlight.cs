using UnityEngine;


/// <summary>
/// Enum representing different types of flashlights, such as Primary and Secondary.
/// </summary>
public enum FlashlightType
{
    Primary,
    Secondary
}


/// <summary>
/// A serializable class for managing flashlight functionality in the CharacterManager.
/// </summary>
[System.Serializable]
public class Flashlight
{
    [Tooltip("The GameObject representing the light source of the flashlight.")]
    [SerializeField] protected GameObject lightObj = null;
    
    [Tooltip("The GameObject representing the mesh or visual component of the flashlight.")]
    [SerializeField] protected GameObject lightMesh = null;


    /// <summary>
    /// Update the data for the flashlight, setting the light source and mesh.
    /// </summary>
    /// <param name="_obj">The GameObject representing the light source.</param>
    /// <param name="_mesh">The GameObject representing the mesh or visual component.</param>
    public void UpdateData(GameObject _obj, GameObject _mesh)
    {
        lightObj = _obj;
        lightMesh = _mesh;
    }

    /// <summary>
    /// Activate or deactivate the light source of the flashlight.
    /// </summary>
    /// <param name="_enableLight">A boolean flag to enable or disable the light source.</param>
    public void ActivateLight(bool _enableLight)
    {
        lightObj?.SetActive(_enableLight);
    }

    /// <summary>
    /// Activate or deactivate the mesh or visual component of the flashlight.
    /// </summary>
    /// <param name="_enableMesh">A boolean flag to enable or disable the mesh or visual component.</param>
    public void ActivateMesh(bool _enableMesh)
    {
        lightMesh?.SetActive(_enableMesh);
    }
}
