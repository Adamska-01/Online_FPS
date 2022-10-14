using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropdownManager : MonoBehaviour
{
    public TMP_Dropdown gameModeDropdown;
    public List<GameObject> dropdowns;
      

    void Update()
    {
        switch(gameModeDropdown.value)
        {
            case 0:
                ActivateDropdown(0);
                break;
            case 1:
                ActivateDropdown(1);
                break;
            case 2:
                ActivateDropdown(2);
                break;
        }
    }

    private void ActivateDropdown(int _index)
    {
        for (int i = 0; i < dropdowns.Count; i++) //Activate only the selected one
            dropdowns[i].SetActive(i == _index);
    }
}
