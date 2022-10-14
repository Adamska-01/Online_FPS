using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventsListener : MonoBehaviour
{
    public void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += Launcher.Instance.NetworkingClient_EventReceived;
    }

    public void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= Launcher.Instance.NetworkingClient_EventReceived;
    } 
}
