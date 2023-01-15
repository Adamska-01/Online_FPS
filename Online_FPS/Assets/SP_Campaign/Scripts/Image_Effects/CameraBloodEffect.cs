using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CameraBloodEffect : MonoBehaviour
{
    [SerializeField] private Texture2D bloodTexture = null;
    [SerializeField] private Texture2D bloodNormalMap = null;
    [SerializeField] private Shader shader = null;

    [SerializeField] private float bloodAmount = 0.0f;
    [SerializeField] private float minBloodAmount = 0.0f;
    [SerializeField] private float distortion = 1.0f;
    [SerializeField] private float fadeSpeed = 0.05f;
    [SerializeField] private bool autoFade = true;

    private Material material = null;


    //Properties 
    public float BloodAmount    { get { return bloodAmount; }      set { bloodAmount = value; } }
    public float MinBloodAmount { get { return minBloodAmount; }   set { minBloodAmount = value; } }
    public float FadeSpeed      { get { return fadeSpeed; }        set { fadeSpeed = value; } }
    public bool AutoFade        { get { return autoFade; }         set { autoFade = value; } }
    //public float Distortion     {  get { return distortion; }   set { distortion = value; } }


    private void Update()
    {
        if(autoFade)
        {
            bloodAmount -= fadeSpeed * Time.deltaTime;

            bloodAmount = Mathf.Max(bloodAmount, minBloodAmount);
        }
    }


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

        //Send data into the shader
        if(bloodTexture != null)
        {
            material.SetTexture("_BloodTex", bloodTexture);
        }
        if (bloodNormalMap != null)
        {
            material.SetTexture("_BloodBump", bloodNormalMap);
        }

        material.SetFloat("_BloodAmount", bloodAmount);
        material.SetFloat("_Distortion", distortion);

        Graphics.Blit(source, destination, material);
    }
}
