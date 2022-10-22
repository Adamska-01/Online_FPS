using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MapVote : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Image buttonImage; 
    private Room currentRoom; 
    private int vote; 
    private bool hasVotedthisMap = false;

    private void Start()
    {
        currentRoom = PhotonNetwork.CurrentRoom;
    }

    public void SetUp(int _votes, Sprite _image)
    {
        vote = _votes;  
        buttonText.text = "Votes: " + vote; 

        buttonImage.sprite = _image; 
    }

    public void SetVote(int _votes)
    {
        vote = _votes;  
        buttonText.text = "Votes: " + vote; 
    }

    public void OnClick()
    {
        //Vote only if client hasn't voted
        if(!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasVotedGeneral") ||

            (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasVotedGeneral") &&
            !(bool)PhotonNetwork.LocalPlayer.CustomProperties["hasVotedGeneral"]))
        {
            int[] votes = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapVotes"];
            vote = votes[transform.GetSiblingIndex()] = ++votes[transform.GetSiblingIndex()];

            Hashtable setValue = new Hashtable();
            setValue.Add("mapVotes", votes);
            PhotonNetwork.CurrentRoom.SetCustomProperties(setValue);

            //Vote only once
            Hashtable hasVotedGeneral = new Hashtable { { "hasVotedGeneral", true } }; 
            PhotonNetwork.LocalPlayer.SetCustomProperties(hasVotedGeneral);
             
            hasVotedthisMap = true; //Voted this map
        }
    } 
      
    public void LeaveRoom() //Remove vote (if voted)  
    {        
        if (hasVotedthisMap)
        {
            if (currentRoom != null)
            {
                //remove vote 
                int[] votes = (int[])currentRoom.CustomProperties["mapVotes"];
                vote = votes[transform.GetSiblingIndex()] = --votes[transform.GetSiblingIndex()];

                //Assign new votes
                Hashtable setValue = new Hashtable();
                setValue.Add("mapVotes", votes);
                currentRoom.SetCustomProperties(setValue);
            }

            Hashtable hasVotedGeneral = new Hashtable { { "hasVotedGeneral", false } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(hasVotedGeneral);

            hasVotedthisMap = false;
        }

        Destroy(gameObject);
    }


    //Button Sound
    public void PlayButtonPressSound()
    {
        AudioSource src = SoundManager.instance.PlaySoundAndReturn(SoundManagerConstants.Clips.BUTTON_SELECT, SoundManagerConstants.AudioOutput.SFX, transform.position);
        if (src != null)
        {
            src.spatialBlend = 0.0f;
            src.priority = 0;
        }
    }
}
