using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;


public class PlayerInventoryUI_PDAEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    //Inspector-Assigned
    [SerializeField] private TMP_Text name = null;
    [SerializeField] private TMP_Text subject = null;
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color activeColor = Color.red;

    //Internal
    protected PlayerInventoryUI inventoryUI = null;
    protected InventoryItemAudio inventoryItemAudio = null;
    protected int index = -1;


    private PlayerInventoryUI InventoryUI
    {
        get
        {
            if(inventoryUI == null)
            {
                inventoryUI = GetComponentInParent<PlayerInventoryUI>();
            }
            return inventoryUI;
        }
    }



    private bool IsActive()
    {
        if (InventoryUI == null || InventoryUI.PlayerInventory == null || inventoryItemAudio == null)
            return false;

        return InventoryUI.PlayerInventory.GetActiveAudioRecording() == index ? true : false;
    }

    public void SetData(InventoryItemAudio _itemAudio, int _index)
    {
        //Cache audio
        inventoryItemAudio = _itemAudio;

        //Store the index if this item in the list
        index = _index;

        bool isActive = IsActive(); //Is this the sound that is currently playing?

        //Set text data
        name?.SetText((inventoryItemAudio != null) ? inventoryItemAudio.Person : null);
        subject?.SetText((inventoryItemAudio != null) ? inventoryItemAudio.Subject : null);

        //Set text colors
        if(name != null)
        {
            name.color = isActive ? activeColor : normalColor;
        }
        if(subject != null)
        {
            subject.color = isActive ? activeColor : normalColor;
        }
    }

    public void OnPointerEnter()
    {
        if (IsActive())
            return;

        //Set text colors
        if (name != null)
        {
            name.color = hoverColor;
        }
        if (subject != null)
        {
            subject.color = hoverColor;
        }
    }

    public void OnPointerExit()
    {
        if (IsActive())
            return;

        //Set text colors
        if (name != null)
        {
            name.color = normalColor;
        }
        if (subject != null)
        {
            subject.color = normalColor;
        }
    }

    public void OnPointerClick()
    {
        if (InventoryUI == null)
            return;

        //Play the audio recording at the corresponding index
        if (InventoryUI != null && InventoryUI.PlayerInventory != null)
        {
            InventoryUI.PlayerInventory.PlayAudioRecording(index);
        }

        InventoryUI.RefreshPDAEntries();
        InventoryUI.SelectTabGroup(1, 1); //Select PDA tab (I know.. hardcoded)
    }


    //Events
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnter();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExit();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        OnPointerClick();
    }
}
