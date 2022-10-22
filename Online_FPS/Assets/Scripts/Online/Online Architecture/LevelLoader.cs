using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance;
    void Awake()
    {
        Instance = this;
    }

    [SerializeField] private Slider loadingBar;
    [SerializeField] private TMP_Text progressText;

    //Loading functions
    public void LoadLevelOffline(int _index)
    {
        StartCoroutine(LoadLevelOfflineAsync(_index));
    }

    public void LoadLevelOnline(int _index)
    {
        StartCoroutine(LoadLevelOnlineAsync(_index));
    }
    public void ShowLoadingMenuOnOtherClients()
    {
        StartCoroutine(ProgressBarOnline());
    }


    //Loading Coroutines
    private IEnumerator LoadLevelOfflineAsync(int _index)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_index);

        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING_SCENE);

        while(!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
             
            loadingBar.value = progress;
            progressText.text = (int)(progress * 100.0f) + "%"; 
            yield return null;
        }
    }

    private IEnumerator LoadLevelOnlineAsync(int _index)
    {
        PhotonNetwork.LoadLevel(_index);

        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING_SCENE);

        while (PhotonNetwork.LevelLoadingProgress < 1)
        { 
            float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / 0.9f);

            loadingBar.value = progress;
            progressText.text = (int)(progress * 100.0f) + "%"; 
            yield return null;
        }
    }
    private IEnumerator ProgressBarOnline()
    {
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING_SCENE);
        while (PhotonNetwork.LevelLoadingProgress < 1)
        {
            float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / 0.9f);

            loadingBar.value = progress;
            progressText.text = (int)(progress * 100.0f) + "%";
            yield return null;
        }
    }
}
