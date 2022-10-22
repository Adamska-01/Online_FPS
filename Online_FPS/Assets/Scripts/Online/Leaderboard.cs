using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
     

    private void Start()
    {
        playerNameText.gameObject.SetActive(true);
        killsText.gameObject.SetActive(true);
        deathsText.gameObject.SetActive(true);
    }

    public void SetDetails(string name, int kills, int deaths, bool isLocal)
    {
        playerNameText.text = isLocal ? $"<color=blue>{name}</color>" : name;
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString();
    }
}
