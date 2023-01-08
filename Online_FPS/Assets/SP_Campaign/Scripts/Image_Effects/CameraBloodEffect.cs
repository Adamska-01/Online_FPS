using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CameraBloodEffect : MonoBehaviour
{
    [SerializeField] private Shader shader = null;

    private Material material = null;


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shader == null) //shader MUST be assigned 
            return;

        //Create material
        if(material == null)
        {
            material = new Material(shader);
        }

        //Safe check 
        if (material == null)
            return;

        Graphics.Blit(source, destination, material);
    }
}
