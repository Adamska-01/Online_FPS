using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PingCounter : MonoBehaviour
{
    public TMP_Text fpsText; 

    void Update()
    { 
        if(PhotonNetwork.IsConnected)
            fpsText.text = "Ping: " + PhotonNetwork.GetPing();
        else
            fpsText.text = "Ping: N/A";
    }
}
