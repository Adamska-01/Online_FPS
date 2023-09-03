using UnityEngine;
using UnityEngine.EventSystems;


public class TabEntity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Inventory Class")]
    [SerializeField] private PlayerInventoryUI inventoryUI;

    //Internals
    private int groupIndex = -1;
    private int tabIndex = -1;


    //Public Properties 
    public int GroupIndex { get { return groupIndex; } set { groupIndex = value; } }
    public int TabIndex { get { return tabIndex; } set { tabIndex = value; } }



    //Events

    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryUI?.OnEnterTab(groupIndex, tabIndex);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI?.OnExitTab(groupIndex, tabIndex);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("OnPointerClick");
        inventoryUI?.OnClickTab(groupIndex, tabIndex);
    }
}
