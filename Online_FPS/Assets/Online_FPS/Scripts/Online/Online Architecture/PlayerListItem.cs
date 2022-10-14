using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text text;
    private Player player;

    public void SetUp(Player _player)
    {
        player = _player;
        text.text = _player.NickName;
    }


    //----------------------Photon Callbacks----------------------
    public override void OnPlayerLeftRoom(Player otherPlayer) //If other player leaves the room
    {
        if(player == otherPlayer) //if we've left the room 
        {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom() //If we leave the room
    {
        Destroy(gameObject);
    }
}
