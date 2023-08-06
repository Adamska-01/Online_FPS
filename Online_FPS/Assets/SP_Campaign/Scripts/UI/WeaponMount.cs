using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class WeaponMount : MonoBehaviour
{
    //Inspector-Assigned
    [SerializeField] private TMP_Text weaponMountName;
    [SerializeField] private Slider weaponMountSlider;
    [SerializeField] private Image weaponMountImage;
    [SerializeField] private GameObject weaponMountAmmoInfo;
    [SerializeField] private TMP_Text weaponMountRounds;
    [SerializeField] private TMP_Text weaponMountReloadType;


    //Properties
    public string WeaponName { get { return weaponMountName.text; } set { weaponMountName.SetText(value); } }
    public Slider ConditionSlider { get { return weaponMountSlider; } }
    public Sprite WeaponImage { private get { return weaponMountImage.sprite; } set { weaponMountImage.sprite = value; } }
    public GameObject AmmoInfoMount { get { return weaponMountAmmoInfo; } }
    public string RoundsText { get { return weaponMountRounds.text; } set { weaponMountRounds.SetText(value); } }
    public string RealoadTypeText { get { return weaponMountReloadType.text; } set { weaponMountReloadType.SetText(value); } }
}
