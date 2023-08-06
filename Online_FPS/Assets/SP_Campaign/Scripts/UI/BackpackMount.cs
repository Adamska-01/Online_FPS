using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class BackpackMount : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] private Image backpackMountImage = null;
    [SerializeField] private TMP_Text backpackMountEmptyText = null;


    //Properties
    public Image BackpackItemImage { get { return backpackMountImage; } }
    public TMP_Text EmptyText { get { return backpackMountEmptyText; } } 
}
