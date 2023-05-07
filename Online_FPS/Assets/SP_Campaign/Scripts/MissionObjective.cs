using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObjective : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player_Info playerInfo = GameSceneManager.Instance?.GetPlayerInfo(other.GetInstanceID());
        if(playerInfo != null)
        {
            playerInfo.characterManager.CompleteLevel();
        }
    }
}
