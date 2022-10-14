using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShootingRangeUI : MonoBehaviour
{
    public static ShootingRangeUI instance;
    void Awake()
    {
        instance = this;        
    }

    public TMP_Text scoreText;
    public TMP_Text timeText;
    public Image panelImage;
    public Color initialColor;


    private void Start()
    {
        initialColor = panelImage.color; //deselected color
    } 
}
