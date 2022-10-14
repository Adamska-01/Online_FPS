using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchSettingsSelection : MonoBehaviour
{
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private bool isDuration;

    private int currentSelection;

    private void Awake()
    {
        SelectSetting(0);
    }

    private void SelectSetting(int _index)
    {
        leftButton.interactable = _index != 0;
        rightButton.interactable = _index != transform.childCount - 1;

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == _index);
        }
    }

    public void ChangeSelection(int _change)
    {
        currentSelection += _change;
        SelectSetting(currentSelection);

        if (isDuration)
        {
            string duration = transform.GetChild(currentSelection).GetComponent<TMP_Text>().text.Split('m')[0];
            MatchManager.instance.matchLength = int.Parse(duration) * 60;
        }
        else
            MatchManager.instance.killsToWin = int.Parse(transform.GetChild(currentSelection).GetComponent<TMP_Text>().text);
    }
}
