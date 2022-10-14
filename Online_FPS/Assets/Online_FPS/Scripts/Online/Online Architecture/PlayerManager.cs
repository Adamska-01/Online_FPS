using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;
    private PhotonView pv;
    public PhotonView PV { get { return  pv; } }
    private GameObject controller;
    private GameObject deathCamera;

    private void Awake()
    {
        instance = this;
        pv = GetComponent<PhotonView>();
        deathCamera = GameObject.Find("DeathCamera");
    }

    private void Start()
    {
        if (pv.IsMine && MatchManager.instance.state == MatchManager.GameStates.Playing) //if pv is owned by the local player
        {
            CreateController();
            deathCamera.SetActive(false);
        }
    }

    public void CreateController()
    {
        //Instantiate player controller
        if(controller != null)
            PhotonNetwork.Destroy(controller);

        Transform spawnpoint = NETSpawner.instance.GetSpawnPoint();
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { pv.ViewID });

        if(pv.IsMine)
        {
            DI_System indSys = FindObjectOfType<DI_System>();
            indSys.player = controller.transform;
            indSys.cam = controller.GetComponentInChildren<Camera>();
        }
    }

    public void Die(string _damager)
    {
        NETUIController.instance.deathText.text = "You were killed by <color=red>" + _damager + "</color>";

        //Update stats
        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber , 1, 1, _damager);

        if (controller != null)
        {
            StartCoroutine(DieCo());
        }
    }

    public IEnumerator DieCo()
    {
        //Death effect 
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "DeathEffect"), controller.transform.position + new Vector3(0.0f, 1.2f, 0.0f), Quaternion.identity);

        controller.GetComponentInChildren<CameraLerp>().LerpCamera();

        //Open death panel
        NETUIController.instance.OpenPanel(PanelType.DEATH);

        yield return new WaitForSeconds(5.0f);
        
        //Destroy player 
        PhotonNetwork.Destroy(controller);
        controller = null;

        //deathCamera.SetActive(true);

        //Open HUD and close death
        NETUIController.instance.OpenPanel(PanelType.HUD);

        //deathCamera.SetActive(false);

        //Respawn
        if(MatchManager.instance.state == MatchManager.GameStates.Playing && controller == null)
            CreateController();
    }
}
