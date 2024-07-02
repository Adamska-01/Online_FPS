using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryPanelType 
{ 
    None, 
    Backpack, 
    AmmoBelt, 
    Weapons, 
    PDA 
}


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
    public TMP_Text transcriptText;
    public TMP_Text notificationText;
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
public class InventoryUI_TabGroupItem
{
    public TMP_Text tabText;
    public GameObject layoutContainer;
}

[System.Serializable]
public class InventoryUI_TabGroup
{
    public List<InventoryUI_TabGroupItem> items;
    public int activeItem;
    public Color HoverColor;
    public Color InactiveColor;
    public Color NormalColor;
}

[System.Serializable]
public struct InventoryUI_DescriptionLayout
{
    public GameObject layoutContainer;
    public Image image;
    public TMP_Text title;
    public ScrollRect scrollView;
    public TMP_Text Description;
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
    [Header("Inventory")]
    [SerializeField] protected Inventory inventory = null;

    //Inspector-Assigned
    [Header("Equipment Mount References")]
    [SerializeField] protected List<GameObject> backpackMounts = new List<GameObject>();
    protected List<BackpackMount> backpackMountDetails = new List<BackpackMount>();
    [SerializeField] protected List<GameObject> weaponMounts = new List<GameObject>();
    protected List<WeaponMount> weaponMountDetails = new List<WeaponMount>();
    [SerializeField] protected List<GameObject> ammoMounts = new List<GameObject>();
    protected List<AmmoMount> ammoMountDetails = new List<AmmoMount>();

    [Header("PDA References")]
    [SerializeField] protected InventoryUI_PDARefences pdaReferences;
    
    [Header("Backpack Scroll Rect")]
    [SerializeField] protected ScrollRect backpackScrollRect = null;

    [Header("UI Meter References")]
    [SerializeField] protected InventoryUIStatus statusReferences;
    
    [Header("Tab Groups")]
    [SerializeField] protected List<InventoryUI_TabGroup> tabGroups = new List<InventoryUI_TabGroup>();
    
    [Header("Description Layouts")]
    [SerializeField] protected InventoryUI_DescriptionLayout generalDescriptionLayout;
    [SerializeField] protected InventoryUI_DescriptionLayout weaponDescriptionLayout;
    
    [Header("Action Button UI References")]
    [SerializeField] protected InventoryUI_ActionButtons actionButton1;
    [SerializeField] protected InventoryUI_ActionButtons actionButton2;
    
    [Header("Shared Variables")]
    [SerializeField] private SharedFloat health = null;
    [SerializeField] private SharedFloat infection = null;
    [SerializeField] private SharedFloat stamina = null;
    [SerializeField] private SharedFloat flashlight = null;
    [SerializeField] private SharedFloat nightVision = null;
    [SerializeField] private SharedString transcriptText = null;
    [SerializeField] private SharedTimedStringQueue notificationQueue = null;

    [Header("Item Selection Colors")]
    [SerializeField] private Color backpackMountHover = Color.cyan;
    [SerializeField] private Color ammoMountHover = Color.gray;
    [SerializeField] private Color weaponMountHover = Color.red;

    private Action onInventoryToggled;

    
    //Properties
    public Inventory PlayerInventory { get { return inventory; }  set { inventory = value; } }
	public event Action OnInventoryToggled
	{
		add => onInventoryToggled += value; 
		remove => onInventoryToggled -= value;
	}


	//Internals
	protected Color backpackMountOriginalColor;
    protected Color weaponMountOriginalColor;
    protected Color ammoMountOriginalColor;
    protected InventoryPanelType selectedPanelType = InventoryPanelType.None;
    protected int selectedMount = -1;
    protected bool isInitialized = false;

    private Action OnHealthUpdate;
    private Action OnStaminaUpdate;
    private Action OnInfectionUpdate;
    private Action OnFlashlightUpdate;
    private Action OnNightvisionUpdate;


    private void OnEnable()
    {
        //Clear buffered input
        Input.ResetInputAxes();

        Time.timeScale = 0.0f;

        //Pause any currently playing sounds
        AudioListener.pause = true;

        Invalidate(); //Redraw UI
    }

    private void OnDisable()
    {
        Time.timeScale = 1.0f;

        //Unpause any currently playing sounds
        AudioListener.pause = false;

        //Clear buffered input
        Input.ResetInputAxes();
    }

	private void Start()
	{
		//Disable Inventory UI at start up
		gameObject.SetActive(false);

		SetCursorVisibility();
	}

	private void OnDestroy()
    {
        //Unsubscribe events (status shared variable)
        if (statusReferences.healthSlider != null && health != null)           health.OnVariableValueChanged -= OnHealthUpdate;
        if (statusReferences.staminaSlider != null && stamina != null)         stamina.OnVariableValueChanged -= OnStaminaUpdate;
        if (statusReferences.infectionSlider != null && infection != null)     infection.OnVariableValueChanged -= OnInfectionUpdate;
        if (statusReferences.flashlightSlider != null && flashlight != null)   flashlight.OnVariableValueChanged -= OnFlashlightUpdate;
        if (statusReferences.nightVisionSlider != null && nightVision != null) nightVision.OnVariableValueChanged -= OnNightvisionUpdate;
    }

    protected void Invalidate()
    {
        if (!isInitialized) //Make sure it has been initialized
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
        for (int i = 0; i < weaponMountDetails.Count; i++)
        {
            weaponMountDetails[i].WeaponImage = null;
            weaponMountDetails[i].WeaponName = "";
            weaponMountDetails[i].ConditionSlider.enabled = false;

            weaponMounts[i].SetActive(false);
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
            if (backpackMounts[i] != null)
            {
                Image img = backpackMounts[i].GetComponent<Image>();
                if (img != null)
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
            if (ammoMounts[i] != null)
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
        if (pdaReferences.autoplayOnPickup && inventory != null)
        {
            pdaReferences.autoplayOnPickup.isOn = inventory.AutoPlayOnPickup;
        }

        //Paint the UI using the inventory 
        if (inventory != null)
        {
            //Configure weapon mounts
            for (int i = 0; i < weaponMountDetails.Count; i++)
            {
                //Do we have a weapon mount here?
                if (weaponMountDetails[i] != null)
                {
                    InventoryWeaponMountInfo weaponMountInfo = inventory.GetWeapon(i);
                    InventoryItemWeapon weapon = null;
                    if (weaponMountInfo != null)
                    {
                        weapon = weaponMountInfo.weapon;
                    }

                    //No weapon here so skip this mount
                    if (weapon == null)
                    {
                        continue;
                    }

                    //Set the sprite and name of the weapon
                    weaponMountDetails[i].WeaponImage = weapon.InventoryImage;
                    weaponMountDetails[i].WeaponName = weapon.InventoryName;
                    //Hide/Desplay Ammo info (no need if it's a melee weapon)
                    if (weapon.WeaponFeedType == InventoryWeaponFeedType.Melee)
                    {
                        weaponMountDetails[i].AmmoInfoMount?.SetActive(false);
                    }
                    else
                    {
                        weaponMountDetails[i].AmmoInfoMount?.SetActive(true);

                        //Display reload type
                        weaponMountDetails[i].RealoadTypeText = weapon.ReloadType.ToString();
                        weaponMountDetails[i].RoundsText = weaponMountInfo.inGunRounds + " / " + weapon.AmmoCapacity.ToString();
                    }
                    //Update condition slider 
                    if (weaponMountDetails[i].ConditionSlider != null)
                    {
                        weaponMountDetails[i].ConditionSlider.enabled = true;
                        weaponMountDetails[i].ConditionSlider.value = weaponMountInfo.condition;
                    }

                    weaponMounts[i].SetActive(true); //Safe check
                }
            }

            //Configure ammo mounts
            for (int i = 0; i < ammoMountDetails.Count; i++)
            {
                //Do we have an ammo mount here?
                if (ammoMountDetails[i] != null)
                {
                    InventoryAmmoMountInfo ammoMountInfo = inventory.GetAmmo(i);
                    InventoryItemAmmo ammo = null;
                    if (ammoMountInfo != null)
                    {
                        ammo = ammoMountInfo.ammo;
                    }

                    //No weapon here so skip this mount
                    if (ammo == null)
                    {
                        continue;
                    }

                    //Set the sprite and the rounds of the ammo mount
                    ammoMountDetails[i].AmmoImage?.gameObject.SetActive(true);
                    ammoMountDetails[i].AmmoImage.sprite = ammo.InventoryImage;

                    ammoMountDetails[i].RoundText?.gameObject.SetActive(true);
                    ammoMountDetails[i].RoundText?.SetText(ammoMountInfo.rounds.ToString());

                    //Disable 'Empty' Text
                    ammoMountDetails[i].EmptyText?.gameObject.SetActive(false);

                    ammoMounts[i].SetActive(true); //Safe check
                }
            }

            //Configure backpack mounts by setting them all to empty and unselected
            for (int i = 0; i < backpackMountDetails.Count; i++)
            {
                //Do we have a backpack mount here?
                if (backpackMountDetails[i] != null)
                {
                    InventoryBackpackMountInfo backpackMountInfo = inventory.GetBackpack(i);
                    InventoryItem backpackItem = null;
                    if (backpackMountInfo != null)
                    {
                        backpackItem = backpackMountInfo.item;
                    }

                    //No weapon here so skip this mount
                    if (backpackItem == null)
                    {
                        continue;
                    }

                    //Set the sprite of backpack mount
                    backpackMountDetails[i].BackpackItemImage?.gameObject.SetActive(true);
                    backpackMountDetails[i].BackpackItemImage.sprite = backpackItem.InventoryImage;

                    //Disable 'Empty' Text
                    backpackMountDetails[i].EmptyText?.gameObject.SetActive(false);

                    backpackMounts[i].SetActive(true); //Safe check
                }

            }

            if (pdaReferences.logEntries)
            {
                int audioCount = inventory.GetAudioRecordingCount();
                int displayCount = pdaReferences.logEntries.childCount;
                for (int i = 0; i < audioCount; i++)
                {
                    InventoryItemAudio audioItem = inventory.GetAudioRecording(i);

                    //Check if we need to create a new entry or not
                    if (i >= displayCount)
                    {
                        GameObject go = Instantiate(pdaReferences.logEntryPrefab);
                        if (go != null)
                        {

                            PlayerInventoryUI_PDAEntry pdaEntry = go.GetComponent<PlayerInventoryUI_PDAEntry>();
                            if (pdaEntry != null)
                            {
                                //Set entry's parent and order 
                                pdaEntry.transform.SetParent(pdaReferences.logEntries, false);
                                pdaEntry.transform.SetSiblingIndex(i); //Set order

                                //Set entry's corresponding data
                                pdaEntry.SetData(audioItem, i);
                            }
                        }
                    }
                    else //"Repurpose" existing entry 
                    {
                        PlayerInventoryUI_PDAEntry pdaEntry = pdaReferences.logEntries.GetChild(i).GetComponent<PlayerInventoryUI_PDAEntry>();
                        if (pdaEntry != null)
                        {
                            pdaEntry.SetData(audioItem, i);

                        }
                    }
                }

                //If displayCount is greater than audioCount, destroy the rest of the unnecessary entries
                for (int i = audioCount; i < displayCount; i++)
                {
                    Destroy(pdaReferences.logEntries.GetChild(i).gameObject);
                }
            }
        }
    }

    public void RefreshPDAEntries()
    {
        if (inventory == null)
            return;

        //Refresh
        for (int i = 0; i < pdaReferences.logEntries.childCount; i++)
        {
            //Get PDA Entry UI
            PlayerInventoryUI_PDAEntry pdaEntry = pdaReferences.logEntries.GetChild(i).GetComponent<PlayerInventoryUI_PDAEntry>();
            InventoryItemAudio audioItem = inventory.GetAudioRecording(i);

            //Set pda data
            if(pdaEntry != null && audioItem != null) 
            {
                pdaEntry.SetData(audioItem, i);
            }
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
        //Cache all normal colors of all tab groups
        for (int i = 0; i < tabGroups.Count; i++)
        {
            InventoryUI_TabGroup tabGroup = tabGroups[i];

            if (tabGroup.items.Count > 0 && tabGroup.items[0].tabText != null) //Tab Group Text
            {
                tabGroup.NormalColor = tabGroup.items[0].tabText.color;
            }
        }

        //Set corresponding index to each tab (to select/deselect tab using the SelectTabGroup function)
        for (int i = 0; i < tabGroups.Count; i++)
        {
            var tabs = tabGroups[i].items;
            for (int j = 0; j < tabs.Count; j++)
            {
                TabEntity tab = tabs[j].tabText.GetComponentInParent<TabEntity>();
                if(tab != null)
                {
                    tab.GroupIndex = i;
                    tab.TabIndex = j;
                }
            }
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
            weaponMountDetails.Add(null);

            //Assing weapon mount details
            if (weaponMounts[i] != null)
            {
                weaponMountDetails[i] = weaponMounts[i].GetComponent<WeaponMount>();
            }
        }

        SelectTabGroup(0, 0);
        SelectTabGroup(1, 0);

        StartCoroutine(ResetScrollViews());

        //Cache lamda event listeners
        OnHealthUpdate = () => { statusReferences.healthSlider.value = health.Value; };
        OnStaminaUpdate = () => { statusReferences.staminaSlider.value = stamina.Value; };
        OnInfectionUpdate = () => { statusReferences.infectionSlider.value = infection.Value; };
        OnFlashlightUpdate = () => { statusReferences.flashlightSlider.value = flashlight.Value; };
        OnNightvisionUpdate = () => { statusReferences.nightVisionSlider.value = nightVision.Value; };

        //Make a first assignment of all the status sliders (all future assignments are handled by the events)
        if (statusReferences.healthSlider != null && health != null)           OnHealthUpdate?.Invoke();
        if (statusReferences.staminaSlider != null && stamina != null)         OnStaminaUpdate?.Invoke();
        if (statusReferences.infectionSlider != null && infection != null)     OnInfectionUpdate?.Invoke();
        if (statusReferences.flashlightSlider != null && flashlight != null)   OnFlashlightUpdate?.Invoke();
        if (statusReferences.nightVisionSlider != null && nightVision != null) OnNightvisionUpdate?.Invoke();

        //Subscribe events (status shared variable)
        if (statusReferences.healthSlider != null && health != null)           health.OnVariableValueChanged += OnHealthUpdate;
        if (statusReferences.staminaSlider != null && stamina != null)         stamina.OnVariableValueChanged += OnStaminaUpdate;
        if (statusReferences.infectionSlider != null && infection != null)     infection.OnVariableValueChanged += OnInfectionUpdate;
        if (statusReferences.flashlightSlider != null && flashlight != null)   flashlight.OnVariableValueChanged += OnFlashlightUpdate;
        if (statusReferences.nightVisionSlider != null && nightVision != null) nightVision.OnVariableValueChanged += OnNightvisionUpdate;
    }

    protected IEnumerator ResetScrollViews()
    {
        yield return null;

        if(backpackScrollRect != null)
        {
            backpackScrollRect.verticalNormalizedPosition = 1.0f;
        }
        if (generalDescriptionLayout.scrollView != null)
        {
            generalDescriptionLayout.scrollView.verticalNormalizedPosition = 1.0f;
        }
        if (weaponDescriptionLayout.scrollView != null)
        {
            weaponDescriptionLayout.scrollView.verticalNormalizedPosition = 1.0f;
        }
    }

    public void SelectTabGroup(int _tabGroupIndex, int _panel)
    {
        if (_tabGroupIndex < 0 || _tabGroupIndex >= tabGroups.Count)
            return;

        //Fetch group we wish to process
        InventoryUI_TabGroup tabGroup = tabGroups[_tabGroupIndex];
       
        tabGroup.activeItem = _panel; //Cache active panel

        //For every tab
        for (int i = 0; i < tabGroup.items.Count; i++)
        {
            //Enable selected tab
            tabGroup.items[i].layoutContainer?.SetActive(i == tabGroup.activeItem);
                
            //Set Selected/Deselected color
            if (tabGroup.items[i].tabText != null) 
            {
                tabGroup.items[i].tabText.color = (i == tabGroup.activeItem) ? tabGroup.NormalColor : tabGroup.InactiveColor;
            }
        }
    }

    protected void DisplayWeaponDescription(InventoryItem _item)
    {
        if (_item == null) //Hide description if no item is specified
        {
            HideDescription();
            return;
        }

        //Disable Non-Weapon Layout 
        generalDescriptionLayout.layoutContainer?.SetActive(false);
        //Enable Weapons Layout
        weaponDescriptionLayout.layoutContainer?.SetActive(true);

        //Set sprite, title and description
        if(weaponDescriptionLayout.image != null)
        {
            weaponDescriptionLayout.image.sprite = _item.InventoryImage;
        }
        weaponDescriptionLayout.title?.SetText(_item.InventoryName);
        weaponDescriptionLayout.Description?.SetText(_item.InventoryDescription);


        //Enable/Disable action buttons
        actionButton1.buttonObject?.SetActive(_item.InventoryAction != InventoryAction.None);
        actionButton1.buttonText?.SetText(_item.InventoryActionText);

        actionButton2.buttonObject?.SetActive(true);
        actionButton2.buttonText?.SetText("Drop");

        //Reset the scroll to the top
        if(weaponDescriptionLayout.scrollView != null)
        {
            weaponDescriptionLayout.scrollView.verticalNormalizedPosition = 1.0f;
        }
    }

    protected void DisplayGeneralDescription(InventoryItem _item)
    {
        if (_item == null) //Hide description if no item is specified
        {
            HideDescription();
            return;
        }

        //Disable Non-Weapon Layout 
        generalDescriptionLayout.layoutContainer?.SetActive(true);
        //Enable Weapons Layout
        weaponDescriptionLayout.layoutContainer?.SetActive(false);

        //Set sprite, title and description
        if (generalDescriptionLayout.image != null)
        {
            generalDescriptionLayout.image.sprite = _item.InventoryImage;
        }
        generalDescriptionLayout.title?.SetText(_item.InventoryName);
        generalDescriptionLayout.Description?.SetText(_item.InventoryDescription);

        //Enable/Disable action buttons
        actionButton1.buttonObject?.SetActive(_item.InventoryAction != InventoryAction.None);
        actionButton1.buttonText?.SetText(_item.InventoryActionText);

        actionButton2.buttonObject?.SetActive(true);
        actionButton2.buttonText?.SetText("Drop");

        //Reset the scroll to the top
        if (generalDescriptionLayout.scrollView != null)
        {
            generalDescriptionLayout.scrollView.verticalNormalizedPosition = 1.0f;
        }
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
            //Get the mount object using the index
            InventoryBackpackMountInfo itemMount = inventory.GetBackpack(mount);
            if(itemMount == null || itemMount.item == null)
                return;

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
            DisplayGeneralDescription(itemMount.item);
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
            //Get the mount object using the index
            InventoryBackpackMountInfo itemMount = inventory.GetBackpack(mount);
            if (itemMount == null || itemMount.item == null)
                return;

            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.Backpack)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = backpackMountHover;
                _image.fillCenter = false;

                DisplayGeneralDescription(itemMount.item); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.Backpack;
                selectedMount = mount;

                _image.color = backpackMountOriginalColor;
                _image.fillCenter = true;

                DisplayGeneralDescription(itemMount.item); //Display info
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
            //Get the mount object using the index
            InventoryAmmoMountInfo itemMount = inventory.GetAmmo(mount);
            if (itemMount == null || itemMount.ammo == null)
                return;

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
            DisplayGeneralDescription(itemMount.ammo);
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
            //Get the mount object using the index
            InventoryAmmoMountInfo itemMount = inventory.GetAmmo(mount);
            if (itemMount == null || itemMount.ammo == null)
                return;
            
            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.AmmoBelt)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = ammoMountHover;
                _image.fillCenter = false;

                DisplayGeneralDescription(itemMount.ammo); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.AmmoBelt;
                selectedMount = mount;

                _image.color = ammoMountOriginalColor;
                _image.fillCenter = true;

                DisplayGeneralDescription(itemMount.ammo); //Display info
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
            //Get the mount object using the index
            InventoryWeaponMountInfo itemMount = inventory.GetWeapon(mount);
            if (itemMount == null || itemMount.weapon == null)
                return;

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
            DisplayWeaponDescription(itemMount.weapon);
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
            //Get the mount object using the index
            InventoryWeaponMountInfo itemMount = inventory.GetWeapon(mount);
            if (itemMount == null || itemMount.weapon == null)
                return;

            //We are clicking on the selected item, so unselect
            if (mount == selectedMount && selectedPanelType == InventoryPanelType.Weapons)
            {
                Invalidate(); //Bring back to default state (redraw)

                _image.color = weaponMountHover;
                _image.fillCenter = false;

                DisplayWeaponDescription(itemMount.weapon); //Display info
            }
            else //We are selecting 
            {
                Invalidate(); //Bring back to default state (redraw)

                selectedPanelType = InventoryPanelType.Weapons;
                selectedMount = mount;

                _image.color = weaponMountOriginalColor;
                _image.fillCenter = true;

                DisplayWeaponDescription(itemMount.weapon); //Display info
            }
        }
    }

    public void OnEnterTab(int _groupIndex, int _tabIndex)
    {
        if (_groupIndex == -1 || _tabIndex == -1)
            return;

        InventoryUI_TabGroup tabGroup = tabGroups[_groupIndex];

        if(_tabIndex >= 0 && _tabIndex < tabGroup.items.Count)
        {
            if(tabGroup.items[_tabIndex].tabText != null)
            {
                tabGroup.items[_tabIndex].tabText.color = (tabGroup.activeItem != _tabIndex) ? tabGroup.HoverColor : tabGroup.NormalColor;
            }
        }
    }

    public void OnExitTab(int _groupIndex, int _tabIndex)
    {
        if (_groupIndex == -1 || _tabIndex == -1)
            return;

        InventoryUI_TabGroup tabGroup = tabGroups[_groupIndex];
        
        if (_tabIndex >= 0 && _tabIndex < tabGroup.items.Count)
        {
            if (tabGroup.items[_tabIndex].tabText != null)
            {
                tabGroup.items[_tabIndex].tabText.color = (tabGroup.activeItem == _tabIndex) ? tabGroup.NormalColor : tabGroup.InactiveColor;
            }
        }
    }

    public void OnClickTab(int _groupIndex, int _tabIndex)
    {
        if (_groupIndex == -1 || _tabIndex == -1)
            return;

        InventoryUI_TabGroup tabGroup = tabGroups[_groupIndex];
        
        if (_tabIndex >= 0 && _tabIndex < tabGroup.items.Count)
        {
            SelectTabGroup(_groupIndex, _tabIndex);
        }
    }

    public void OnActionButton1Pressed()
    {
        if (inventory == null)
            return;

        switch (selectedPanelType)
        {
            case InventoryPanelType.Backpack:
                inventory.UseBackpackItem(selectedMount);
                break;
            case InventoryPanelType.Weapons:
                inventory.ReloadWeaponItem(selectedMount);
                break;
        }

        Invalidate(); //Repaint invenotory
    }

    public void OnActionButton2Pressed()
    {
        if (inventory == null)
            return;

        switch (selectedPanelType)
        {
            case InventoryPanelType.Backpack:
                inventory.DropBackpackItem(selectedMount);
                break;
            case InventoryPanelType.Weapons:
                inventory.DropWeaponItem(selectedMount);
                break;
            case InventoryPanelType.AmmoBelt:
                inventory.DropAmmoItem(selectedMount);
                break;
        }

        Invalidate(); //Repaint invenotory
    }

    public void OnAutoplayOnPickup()
    {
        if (pdaReferences.autoplayOnPickup != null && inventory != null)
        {
            inventory.AutoPlayOnPickup = pdaReferences.autoplayOnPickup.isOn;
        }
    }

    public void OnBeginAudio(InventoryItemAudio _audioItem)
    {
        if (_audioItem == null)
            return;

        //Set UI elements for the audio item, image, author, subject etc...
        if(pdaReferences.pdaImage != null)
        {
            pdaReferences.pdaImage.texture = _audioItem.Image;
            pdaReferences.pdaImage.color = Color.white;
        }
        pdaReferences.pdaAuthor?.SetText(_audioItem.Person);
        pdaReferences.pdaSubject?.SetText(_audioItem.Subject);
    }

    public void OnUpdateAudio(float _time)
    {
        if (pdaReferences.timelineSlider != null)
        {
            pdaReferences.timelineSlider.value = _time;
        }
    }

    public void OnEndAudio()
    {
        //Set Timeline UI slider back to 0
        if (pdaReferences.timelineSlider != null)
        {
            pdaReferences.timelineSlider.value = 0.0f;
        }

        //Clear PDA Panel's image author and subject
        if (pdaReferences.pdaImage != null)
        {
            pdaReferences.pdaImage.texture = null;
            pdaReferences.pdaImage.color = Color.black;
        }

        //Clear texts (subject and author)
        pdaReferences.pdaAuthor?.SetText(string.Empty);
        pdaReferences.pdaSubject?.SetText(string.Empty);

        // Defer PDA Entry list refresh until next frame (only if active)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(LateOnEndAudio());
        }
    }

    public IEnumerator LateOnEndAudio()
    {
        yield return null;

        RefreshPDAEntries();
    }

    public void SetCursorVisibility()
    {
		Cursor.visible = gameObject.activeSelf;
		Cursor.lockState = gameObject.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
	}

	#region UI_Input
	public bool ToggleInventory()
    {
        var toggleValue = !gameObject.activeSelf;

		gameObject.SetActive(toggleValue);

        onInventoryToggled?.Invoke();

        SetCursorVisibility();

		return toggleValue;
	}
	#endregion
}
