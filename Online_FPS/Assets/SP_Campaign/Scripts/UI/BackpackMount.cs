using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class BackpackMount : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Inventory Class")]
    [SerializeField] private PlayerInventoryUI inventoryUI;
    
    //Inspector-Assigned
    [Header("Mount Components")]
    [SerializeField] private Image backpackMountImage = null;
    [SerializeField] private TMP_Text backpackMountEmptyText = null;



    //Properties
    public Image BackpackItemImage { get { return backpackMountImage; } }
    public TMP_Text EmptyText { get { return backpackMountEmptyText; } }


    //Events
    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryUI.OnClickBackpackMount(GetComponent<Image>());
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryUI.OnEnterBackpackMount(GetComponent<Image>());
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.OnExitBackpackMount(GetComponent<Image>());
    }
}
