using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public enum InventoryPanelType { None, Backpack, AmmoBelt, Weapons, PDA }


#region Support_Structures 
[System.Serializable]
public struct InventoryUI_PDARefences
{
    public Transform logEntries;
    public RawImage pdaImage;
    public TMP_Text pdaAuthor;
    public TMP_Text pdaSubject;
    public Slider timelineSlider;
    public Toggle autoplayOnPickup;
    public GameObject logEntryPrefab;
}

[System.Serializable]
public struct InventoryUIStatus
{
    public Slider healthSlider;
    public Slider infectionSlider;
    public Slider staminaSlider;
    public Slider flashlightSlider;
    public Slider nightVisionSlider;
}

[System.Serializable]
public struct InventoryUI_TabGroupItem
{
    public TMP_Text tabText;
    public GameObject layoutContainer;
}

[System.Serializable]
public struct InventoryUI_TabGroup
{
    public List<InventoryUI_TabGroupItem> items;
}

[System.Serializable]
public struct InventoryUI_DescriptionLayout
{
    public GameObject layoutContainer;
    public Image image;
    public TMP_Text title;
    public ScrollRect scrollView;
    public TMP_Text Content;
}

[System.Serializable]
public struct InventoryUI_ActionButtons
{
    public GameObject buttonObject;
    public TMP_Text buttonText;
}
#endregion


// --------------------------------------------------------------------------------
// CLASS	:	PlayerInventoryUI
// DESC		:	Manages the UI used to interact and display the players inventory
// --------------------------------------------------------------------------------
public class PlayerInventoryUI : MonoBehaviour
{
    //Inspector-Assigned
    [Header("Equipment Mount References")]
    [SerializeField] protected List<GameObject> backpackMounts = new List<GameObject>();
    protected List<BackpackMount> backpackMountDetails = new List<BackpackMount>();
    [SerializeField] protected List<GameObject> weaponMounts = new List<GameObject>();
    protected List<WeaponMount> WeaponMountDetails = new List<WeaponMount>();
    [SerializeField] protected List<GameObject> ammoMounts = new List<GameObject>();
    protected List<AmmoMount> ammoMountDetails = new List<AmmoMount>();

    [Header("PDA References")]
    [SerializeField] protected InventoryUI_PDARefences pdaReferences;
    
    [Header("UI Meter References")]
    [SerializeField] protected InventoryUIStatus statusReferences;
    
    [Header("Backpack / PDA Tab Group")]
    [SerializeField] protected InventoryUI_TabGroup tabGroup;
    
    [Header("Description Layouts")]
    [SerializeField] protected InventoryUI_DescriptionLayout generalDescriptionLayout;
    [SerializeField] protected InventoryUI_DescriptionLayout weaponDescriptionLayout;
    
    [Header("Action Button UI References")]
    [SerializeField] protected InventoryUI_ActionButtons actionButton1;
    [SerializeField] protected InventoryUI_ActionButtons actionButton2;
    
    [Header("Shared Variables")]
    [SerializeField] private SharedFloat health;
    [SerializeField] private SharedFloat infection;
    [SerializeField] private SharedFloat stamina;
    [SerializeField] private SharedFloat flashlight;
    [SerializeField] private SharedFloat nightVision;

    [Header("Item Selection Colors")]
    [SerializeField] private Color tabTextHover = Color.cyan;
    [SerializeField] private Color tabTextInactive = Color.gray;
    [SerializeField] private Color backpackMountHover = Color.cyan;
    [SerializeField] private Color ammoMountHover = Color.gray;
    [SerializeField] private Color weaponMountHover = Color.red;


    //Internals
    protected Color backpackMountOriginalColor;
    protected Color weaponMountOriginalColor;
    protected Color ammoMountOriginalColor;
    protected Color tabTextOriginalColor;

    protected InventoryPanelType selectedPanelType = InventoryPanelType.None;
    protected int selectedMount = -1;
    protected bool isInitialized = false;
    protected int activeTab = 0;
    protected bool audioPlayOnPickup = true;


    private void OnEnable()
    {
        Invalidate();
    }

    private void OnDisable()
    {
        
    }

    private void OnDestroy()
    {
        //Unsubscribe events (status shared variable)
        if (statusReferences.healthSlider != null && health != null)           health.OnVariableAssigned -= () => { statusReferences.healthSlider.value = health.Value; };
        if (statusReferences.staminaSlider != null && stamina != null)         stamina.OnVariableAssigned -= () => { statusReferences.staminaSlider.value = stamina.Value; };
        if (statusReferences.infectionSlider != null && infection != null)     infection.OnVariableAssigned -= () => { statusReferences.infectionSlider.value = infection.Value; };
        if (statusReferences.flashlightSlider != null && flashlight != null)   flashlight.OnVariableAssigned -= () => { statusReferences.flashlightSlider.value = flashlight.Value; };
        if (statusReferences.nightVisionSlider != null && nightVision != null) nightVision.OnVariableAssigned -= () => { statusReferences.nightVisionSlider.value = nightVision.Value; };
    }

    protected void Invalidate()
    {
        if(!isInitialized) //Make sure it has been initialized
        {
            Initialize();
        }

        //Reset selections
        selectedPanelType = InventoryPanelType.None;
        selectedMount = -1;

        //Deactivate description panels (Both Generic and Weapon)
        generalDescriptionLayout.layoutContainer?.SetActive(false);
        weaponDescriptionLayout.layoutContainer?.SetActive(false);

        //Deactivate both action buttons
        actionButton1.buttonObject?.SetActive(false);
        actionButton2.buttonObject?.SetActive(false);

        //Clear the weapon mounts
        for (int i = 0; i < WeaponMountDetails.Count; i++)
        {
            WeaponMountDetails[i].WeaponImage = null;
            WeaponMountDetails[i].WeaponName = "";
            WeaponMountDetails[i].ConditionSlider.enabled = false;

            //weaponMounts[i].SetActive(false);
            weaponMounts[i].transform.GetComponent<Image>().fillCenter = false;
        }

        //Iterate over the UI backpack mounts and set all to empty and unselected
        for (int i = 0; i < backpackMountDetails.Count; i++)
        {
            //Clear sprite and deactivate mount 
            backpackMountDetails[i].BackpackItemImage.sprite = null;
            backpackMountDetails[i].BackpackItemImage.gameObject.SetActive(false);
            
            //Enable the text for this slot that says "Empty"
            backpackMountDetails[i].EmptyText.gameObject.SetActive(true);

            //Make all the mounts look unselected
            if(backpackMounts[i] != null)
            {
                Image img = backpackMounts[i].GetComponent<Image>();
                if(img != null)
                {
                    img.fillCenter = false;
                    img.color = backpackMountOriginalColor;
                }
            }
        }

        //Configure the ammo slots
        for (int i = 0; i < ammoMountDetails.Count; i++)
        {
            //Clear sprite and deactivate mount 
            if(ammoMounts[i] != null)
            {
                ammoMountDetails[i].AmmoImage.sprite = null;
                ammoMountDetails[i].AmmoImage.gameObject.SetActive(false);

                //Give mount frame the 'unselected' look
                Image img = ammoMounts[i].GetComponent<Image>();
                if (img != null)
                {
                    img.fillCenter = false;
                    img.color = ammoMountOriginalColor;
                }
            }

            //Enable the text for this slot that says "Empty"
            ammoMountDetails[i].EmptyText.gameObject.SetActive(true);
            ammoMountDetails[i].RoundText.gameObject.SetActive(false);
        }

        //Other PDA things
        if(pdaReferences.autoplayOnPickup)
        {
            pdaReferences.autoplayOnPickup.isOn = audioPlayOnPickup;
        }
    }

    protected void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true; //Run this function only on first 'enable'

        //Cache original color of backpack frame color so we can restore when not selected
        if(backpackMounts.Count > 0 && backpackMounts[0] != null) //Backpack Mounts
        {
            Image tmp = backpackMounts[0].GetComponent<Image>();
            if(tmp != null)
            {
                backpackMountOriginalColor = tmp.color;
            }
        }
        if (weaponMounts.Count > 0 && weaponMounts[0] != null) //Weapon Mounts
        {
            Image tmp = weaponMounts[0].GetComponent<Image>();
            if (tmp != null)
            {
                weaponMountOriginalColor = tmp.color;
            }
        }
        if (ammoMounts.Count > 0 && ammoMounts[0] != null) //Ammo Mounts
        {
            Image tmp = ammoMounts[0].GetComponent<Image>();
            if (tmp != null)
            {
                ammoMountOriginalColor = tmp.color;
            }
        }
        if (tabGroup.items.Count > 0 && tabGroup.items[0].tabText != null) //Tab Group Text
        {
            tabTextOriginalColor = tabGroup.items[0].tabText.color;
        }

        //Cache all backpack mount detail scripts
        for (int i = 0; i < backpackMounts.Count; i++)
        {
            backpackMountDetails.Add(null);

            //Assing backpack mount details
            if (backpackMounts[i] != null)
            {
                backpackMountDetails[i] = backpackMounts[i].GetComponent<BackpackMount>();
            }
        }
        //Cache all ammo mount detail scripts
        for (int i = 0; i < ammoMounts.Count; i++)
        {
            ammoMountDetails.Add(null);

            //Assing ammo mount details
            if (ammoMounts[i] != null)
            {
                ammoMountDetails[i] = ammoMounts[i].GetComponent<AmmoMount>();
            }
        }
        //Cache all weapon mount detail scripts
        for (int i = 0; i < weaponMounts.Count; i++)
        {
            WeaponMountDetails.Add(null);

            //Assing weapon mount details
            if (weaponMounts[i] != null)
            {
                WeaponMountDetails[i] = weaponMounts[i].GetComponent<WeaponMount>();
            }
        }

        SelectTabGroup(activeTab);

        //Make a first assignment of all the status sliders (all future assignments are handled by the events)
        if (statusReferences.healthSlider != null && health != null)           statusReferences.healthSlider.value = health.Value;
        if (statusReferences.staminaSlider != null && stamina != null)         statusReferences.staminaSlider.value = stamina.Value;
        if (statusReferences.infectionSlider != null && infection != null)     statusReferences.infectionSlider.value = infection.Value;
        if (statusReferences.flashlightSlider != null && flashlight != null)   statusReferences.flashlightSlider.value = flashlight.Value;
        if (statusReferences.nightVisionSlider != null && nightVision != null) statusReferences.nightVisionSlider.value = nightVision.Value;

        //Subscribe events (status shared variable)
        if (statusReferences.healthSlider != null && health != null)           health.OnVariableAssigned += () => { statusReferences.healthSlider.value = health.Value; };
        if (statusReferences.staminaSlider != null && stamina != null)         stamina.OnVariableAssigned += () => { statusReferences.staminaSlider.value = stamina.Value; };
        if (statusReferences.infectionSlider != null && infection != null)     infection.OnVariableAssigned += () => { statusReferences.infectionSlider.value = infection.Value; };
        if (statusReferences.flashlightSlider != null && flashlight != null)   flashlight.OnVariableAssigned += () => { statusReferences.flashlightSlider.value = flashlight.Value; };
        if (statusReferences.nightVisionSlider != null && nightVision != null) nightVision.OnVariableAssigned += () => { statusReferences.nightVisionSlider.value = nightVision.Value; };
    }

    public void SelectTabGroup(int panel)
    {
        activeTab = panel;

        //For every tab
        for (int i = 0; i < tabGroup.items.Count; i++)
        {
            //Enable/Disable selected tab
            tabGroup.items[i].layoutContainer?.SetActive(i == activeTab);
                
            //Set Selected/Deselected color
            if (tabGroup.items[i].tabText != null) 
            {
                tabGroup.items[i].tabText.color = (i == activeTab) ? tabTextOriginalColor : tabTextInactive;
            }
        }
    }

    protected void DisplayWeaponDescription()
    {
        //Disable Non-Weapon Layout 
        generalDescriptionLayout.layoutContainer?.SetActive(false);
        
        //Enable Weapons Layout
        weaponDescriptionLayout.layoutContainer?.SetActive(true);

        //Enable action buttons
        actionButton1.buttonObject?.SetActive(true);
        actionButton2.buttonObject?.SetActive(true);
    }

    protected void DisplayGeneralDescription()
    {
        //Disable Non-Weapon Layout 
        generalDescriptionLayout.layoutContainer?.SetActive(true);

        //Enable Weapons Layout
        weaponDescriptionLayout.layoutContainer?.SetActive(false);

        //Enable action buttons
        actionButton1.buttonObject?.SetActive(true);
        actionButton2.buttonObject?.SetActive(true);
    }

    protected void HideDescription()
    {
        //Disable Non-Weapon Layout 
        generalDescriptionLayout.layoutContainer?.SetActive(false);

        //Enable Weapons Layout
        weaponDescriptionLayout.layoutContainer?.SetActive(false);

        //Enable action buttons
        actionButton1.buttonObject?.SetActive(false);
        actionButton2.buttonObject?.SetActive(false);
    }

    public void OnEnterBackpackMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if(_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnEnterBackpackMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if(mount >= 0 && mount < backpackMounts.Count)
        {
            //Set the color of the frame of this slot to the hover color (if not currently selected)
            if(selectedPanelType != InventoryPanelType.Backpack || selectedMount != mount)
            {
                _image.color = backpackMountHover;
            }

            //If the selected panel is not none then something else is selected at the momment
            //so do not update the info panel
            if (selectedPanelType != InventoryPanelType.None)
                return;

            //Update description window 
            DisplayGeneralDescription();
        }
    }

    public void OnExitBackpackMount(Image _image)
    {
        //Get the slot index from the name of the object passed
        if (_image != null)
        {
            _image.color = backpackMountOriginalColor;
        }

        if (selectedPanelType != InventoryPanelType.None)
            return;

        HideDescription();
    }

    public void OnClickBackpackMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if (_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnClickBackpackMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if (mount >= 0 && mount < backpackMounts.Count)
        {
            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.Backpack)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = backpackMountHover;
                _image.fillCenter = false;

                DisplayGeneralDescription(); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.Backpack;
                selectedMount = mount;

                _image.color = backpackMountOriginalColor;
                _image.fillCenter = true;

                DisplayGeneralDescription(); //Display info
            }
        }
    }

    public void OnEnterAmmoMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if (_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnEnterAmmoMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if (mount >= 0 && mount < ammoMounts.Count)
        {
            //Set the color of the frame of this slot to the hover color (if not currently selected)
            if (selectedPanelType != InventoryPanelType.AmmoBelt || selectedMount != mount)
            {
                _image.color = ammoMountHover;
            }

            //If the selected panel is not none then something else is selected at the momment
            //so do not update the info panel
            if (selectedPanelType != InventoryPanelType.None)
                return;

            //Update description window 
            DisplayGeneralDescription();
        }
    }

    public void OnExitAmmoMount(Image _image)
    {
        //Get the slot index from the name of the object passed
        if (_image != null)
        {
            _image.color = ammoMountOriginalColor;
            
            if (selectedPanelType != InventoryPanelType.None)
                return;
        }

        HideDescription();
    }

    public void OnClickAmmoMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if (_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnClickAmmoMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if (mount >= 0 && mount < ammoMounts.Count)
        {
            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.AmmoBelt)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = ammoMountHover;
                _image.fillCenter = false;

                DisplayGeneralDescription(); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.AmmoBelt;
                selectedMount = mount;

                _image.color = ammoMountOriginalColor;
                _image.fillCenter = true;

                DisplayGeneralDescription(); //Display info
            }
        }
    }

    public void OnEnterWeaponMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if (_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnEnterWeaponMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if (mount >= 0 && mount < weaponMounts.Count)
        {
            //Set the color of the frame of this slot to the hover color (if not currently selected)
            if (selectedPanelType != InventoryPanelType.Weapons  || selectedMount != mount)
            {
                _image.color = weaponMountHover;
            }

            //If the selected panel is not none then something else is selected at the momment
            //so do not update the info panel
            if (selectedPanelType != InventoryPanelType.None)
                return;

            //Update description window 
            DisplayWeaponDescription();
        }
    }

    public void OnExitWeaponMount(Image _image)
    {
        //Get the slot index from the name of the object passed
        if (_image != null)
        {
            _image.color = weaponMountOriginalColor;

            if (selectedPanelType != InventoryPanelType.None)
                return;
        }

        HideDescription();
    }

    public void OnClickWeaponMount(Image _image)
    {
        int mount;

        //Get the slot index from the name of the object passed
        if (_image == null || !int.TryParse(_image.name, out mount))
        {
            Debug.LogError("OnClickWeaponMount Error! Could not parse the image name as INT");
            return;
        }

        //Valid Index? 
        if (mount >= 0 && mount < weaponMounts.Count)
        {
            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.Weapons)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = weaponMountHover;
                _image.fillCenter = false;

                DisplayWeaponDescription(); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.Weapons;
                selectedMount = mount;

                _image.color = weaponMountOriginalColor;
                _image.fillCenter = true;

                DisplayWeaponDescription(); //Display info
            }
        }
    }

    public void OnEnterTab(int _index)
    {
        if(_index >= 0 || _index < tabGroup.items.Count)
        {
            if(tabGroup.items[_index].tabText != null)
            {
                tabGroup.items[_index].tabText.color = (activeTab != _index) ? tabTextHover : tabTextOriginalColor;
            }
        }
    }

    public void OnExitTab(int _index)
    {
        if (_index >= 0 || _index < tabGroup.items.Count)
        {
            if (tabGroup.items[_index].tabText != null)
            {
                tabGroup.items[_index].tabText.color = (activeTab == _index) ? tabTextOriginalColor : tabTextInactive;
            }
        }
    }

    public void OnClickTab(int _index)
    {
        if (_index >= 0 || _index < tabGroup.items.Count)
        {
            SelectTabGroup(_index);
        }
    }
}
