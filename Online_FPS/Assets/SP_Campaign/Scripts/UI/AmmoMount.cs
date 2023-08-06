using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class AmmoMount : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] private Image ammoMountImage = null;
    [SerializeField] private TMP_Text ammoMountEmptyText = null;
    [SerializeField] private TMP_Text ammoMountRoundsText = null;


    //Properties
    public Image AmmoImage { get { return ammoMountImage; } }
    public TMP_Text EmptyText { get { return ammoMountEmptyText; } } 
    public TMP_Text RoundText { get { return ammoMountRoundsText; } }
}
