using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] public TMP_Text playerCount;
    public Image typeIcon;
    public RoomInfo info;
     

    public void SetUp(RoomInfo _info, Sprite _icon)
    {
        info = _info;
        text.text = _info.Name;
        playerCount.text = _info.PlayerCount + "/" + _info.MaxPlayers;
        typeIcon.sprite = _icon;
    }

    public void OnClick()
    {
        Launcher.Instance.JoinRoom(info);
    }

    //Button sound
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
