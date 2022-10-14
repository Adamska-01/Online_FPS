using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Launcher : MonoBehaviourPunCallbacks //Access to callbacks for room creation, errrors, joining lobbies etc.
{
    private const int LOADING_EVENT_CODE = 100;
    public static Launcher Instance;
    void Awake()
    {
        Instance = this;        
    }

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_InputField nicknameInputField; private static bool hasSetNick = false;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Image roomImage;
    [SerializeField] GameObject startGameButton;
    [SerializeField] TMP_Dropdown findDropDown;
    //Rooms
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    //Map vote
    [SerializeField] Transform voteListContent;
    [SerializeField] GameObject mapVotePrefab;
    [SerializeField] GameObject mapSelectedPrefab;
    [SerializeField] List<TMP_Dropdown> mapDropdowns = new List<TMP_Dropdown>();
    //Players 
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;
      
    List<RoomInfo> currentRoomList = new List<RoomInfo>();

    private bool hasStartedTheGame = false;

    public enum GameMode
    {
        DEATHMATCH,
        TEAM_DEATHMATCH,
        CONQUEST
    }
    [System.Serializable] public class MapIndexes
    {
        public Sprite[] mapImage;
        public int[] indexes;
    }
    //Maps 
    public List<GameMode> modes = new List<GameMode>();
    public List<MapIndexes> mapIndexes = new List<MapIndexes>(); 
    public Dictionary<GameMode, MapIndexes> mapsPerMode = new Dictionary<GameMode, MapIndexes>();
    
    
    void Start()
    { 
        SoundManager.instance.PlayRandomMusicFromList();

        //Init maps 
        for (int i = 0; i < modes.Count; i++)
        {
            mapsPerMode.Add(modes[i], mapIndexes[i]);
        }

        MenuManager.Instance.OpenMenu(MenuManager.MenuType.TITLE);

        //Set ability to vote
        Hashtable hasVotedGeneral = new Hashtable { { "hasVotedGeneral", false } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(hasVotedGeneral);
    }
     

    //Enable callbacks
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
    public override void OnDisable()
    {
        //Avoid Errors
        PhotonNetwork.RemoveCallbackTarget(this);
    }


    public void ConnectToServer()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings(); //Connect to the fixed region
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING);
    }

    public void CreateRoom(TMP_Dropdown _dropD)
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
            return; 

        //Set room's properties
        RoomOptions ro = new RoomOptions();
        int[] roomVotes = new int[2];
        int[] mapsarrIndex = new int[2];
        GameMode mode = new GameMode();
        if(_dropD.options[_dropD.value].text.Contains("Deathmatch"))
        {
            mode = GameMode.DEATHMATCH;
            mapsarrIndex[0] = Random.Range(0, mapsPerMode[GameMode.DEATHMATCH].indexes.Length);
            do
            {
                mapsarrIndex[1] = Random.Range(0, mapsPerMode[GameMode.DEATHMATCH].indexes.Length);
            } while (mapsarrIndex[0] == mapsarrIndex[1]); 
        }
        else if (_dropD.options[_dropD.value].text.Contains("Team Deathmatch"))
        {
            mode = GameMode.TEAM_DEATHMATCH;
            mapsarrIndex[0] = Random.Range(0, mapsPerMode[GameMode.TEAM_DEATHMATCH].indexes.Length);
            do
            {
                mapsarrIndex[1] = Random.Range(0, mapsPerMode[GameMode.TEAM_DEATHMATCH].indexes.Length);
            } while (mapsarrIndex[0] == mapsarrIndex[1]);
        }
        else if (_dropD.options[_dropD.value].text.Contains("Conquest"))
        {
            mode = GameMode.CONQUEST;
            mapsarrIndex[0] = Random.Range(0, mapsPerMode[GameMode.CONQUEST].indexes.Length);
            do
            {
                mapsarrIndex[1] = Random.Range(0, mapsPerMode[GameMode.CONQUEST].indexes.Length);
            } while (mapsarrIndex[0] == mapsarrIndex[1]);
        }

        //Select map
        int map = -1;
        for (int i = 0; i < mapDropdowns.Count; i++)
        {
            if(mapDropdowns[i].gameObject.activeSelf)
            {
                switch(mapDropdowns[i].options[mapDropdowns[i].value].text)
                { 
                    case "Test":
                        map = mapsPerMode[(GameMode)i].indexes[0];
                        break;
                    case "Desert":
                        map = mapsPerMode[(GameMode)i].indexes[1];
                        break;
                }
            }
        }

        //Set Properties
        ro.MaxPlayers = 8;
        ro.IsVisible = true;
        ro.CustomRoomPropertiesForLobby = new string[5] { "matchType", "mapToPlay", "mode", "mapsIndexes", "mapVotes" }; //makes sure that other clients on the lobby can see it
        ro.CustomRoomProperties = new Hashtable() { 
            { "matchType", _dropD.options[_dropD.value].text },
            { "mapToPlay", map },
            { "mode", mode },
            { "mapsIndexes", mapsarrIndex }, 
            { "mapVotes",  roomVotes }
        }; 

        //Create room with the created config, 
        PhotonNetwork.CreateRoom(roomNameInputField.text, ro);

        //Loading menu
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING);
    } 

    public void SetNickname()
    {
        if(!string.IsNullOrEmpty(nicknameInputField.text))
        {
            PhotonNetwork.NickName = nicknameInputField.text;

            //Store name 
            PlayerPrefs.SetString("playerName", nicknameInputField.text);

            hasSetNick = true;

            MenuManager.Instance.OpenMenu(MenuManager.MenuType.MULTIPLAYER);
        }
    }

    public void FindRooms(TMP_Dropdown _dropD)
    { 
        //Clear the list every time you get an update
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }

        //Instantiate room button and set it up
        for (int i = 0; i < currentRoomList.Count; i++)
        {
            //Photon doesn't remove rooms that have been removed from the list
            //instead it set a bool that flags it as "removed", thus skip the iteration
            if (currentRoomList[i].RemovedFromList ||
                currentRoomList[i].PlayerCount >= currentRoomList[i].MaxPlayers ||
                (currentRoomList[i].CustomProperties["matchType"].ToString() != _dropD.options[_dropD.value].text &&
                !_dropD.options[_dropD.value].text.Contains("All")))
                continue;
             
            Sprite sprite = default;
            string matchType = currentRoomList[i].CustomProperties["matchType"].ToString();
            if (matchType.Contains("Deathmatch"))
                sprite = _dropD.options[0].image;
            else if(matchType.Contains("Conquest"))
                sprite = _dropD.options[1].image;

            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(currentRoomList[i], sprite);
        }
    }

    public void LeaveRoom()
    {
        //Clear the list of votes when leaving the room
        if (voteListContent && (int)PhotonNetwork.CurrentRoom.CustomProperties["mapToPlay"] == -1)
        {
            foreach (Transform trans in voteListContent)
            {
                trans.GetComponent<MapVote>().LeaveRoom();
            }
        }

        PhotonNetwork.LeaveRoom();

        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING);
    }

    public void JoinRoom(RoomInfo _info)
    {
        PhotonNetwork.JoinRoom(_info.Name);
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING); 
    }

    public void StartGame()
    {
        if (!hasStartedTheGame)
        {
            //Send loading screen event from master client 
            PhotonNetwork.RaiseEvent(
                LOADING_EVENT_CODE,
                null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all clients
                new ExitGames.Client.Photon.SendOptions { Reliability = true } //TCP is always reliable by design)
                );

            int map = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapToPlay"];
            if (map == -1)
            { 
                //Get Room custom properties 
                GameMode mode = (GameMode)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
                int[] votes = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapVotes"];
                int[] mapsIndexes = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapsIndexes"];

                //Get map index
                int bestVote = 0;
                if (votes[0] == votes[1])
                    bestVote = Random.Range(0, mapsPerMode[mode].indexes.Length);
                else
                    bestVote = votes.ToList().IndexOf(votes.Max());

                //Load level
                LevelLoader.Instance.LoadLevelOnline(mapsPerMode[mode].indexes[mapsIndexes[bestVote]]); 

                hasStartedTheGame = true;
            }
            else
            { 
                //Load level
                LevelLoader.Instance.LoadLevelOnline(map); 
                hasStartedTheGame = true;
            }
        }
    }

    public void NetworkingClient_EventReceived(ExitGames.Client.Photon.EventData obj)
    {
        if(obj.Code == LOADING_EVENT_CODE && !PhotonNetwork.IsMasterClient)
        {
            LevelLoader.Instance.ShowLoadingMenuOnOtherClients();
        }
    }

    public void StartSinglePlayerGame()
    {
        SoundManager.instance.FadeOutAllMusic(1.0f);
        LevelLoader.Instance.LoadLevelOffline(1); 
    }

    public void DisconnetFromServer()
    {
        if (PhotonNetwork.IsConnected)
        { 
            PhotonNetwork.Disconnect();
            MenuManager.Instance.OpenMenu(MenuManager.MenuType.LOADING);
        }
        else
        { 
            ConnectToServer();
        }
    }
    

    public void QuitGame()
    { 
        Application.Quit(); 
    }

    //--------------------------------------------------------------------------------
    //--------------------------------Photon Callbacks--------------------------------
    //--------------------------------------------------------------------------------
    public override void OnConnectedToMaster() //called when connecting to master server
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby(); //Need to be in a lobby to find/create rooms

        //Automatically load the scene for all clients (when host switches scene)
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.TITLE);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined lobby");

        //Set Nickname
        if(!hasSetNick)
        {
            MenuManager.Instance.OpenMenu(MenuManager.MenuType.NICKNAME);

            if(PlayerPrefs.HasKey("playerName"))
            {
                nicknameInputField.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
            MenuManager.Instance.OpenMenu(MenuManager.MenuType.MULTIPLAYER);
        } 
    }

    public override void OnJoinedRoom()
    {
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.ROOM);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        Sprite sprite = default;
        string matchType = PhotonNetwork.CurrentRoom.CustomProperties["matchType"].ToString();
        if (matchType.Contains("Deathmatch"))
            sprite = findDropDown.options[0].image;
        else if (matchType.Contains("Conquest"))
            sprite = findDropDown.options[1].image;
        roomImage.sprite = sprite;

        //Clear the list of players before instantiating a new one
        foreach (Transform trans in playerListContent)
        {
            Destroy(trans.gameObject);
        }
        //Create list of players 
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
         
        //Clear the list of votes before instantiating a new one
        foreach (Transform trans in voteListContent)
        {
            Destroy(trans.gameObject);
        }
        int mapToPlay = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapToPlay"];
        GameMode mode = (GameMode)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
        if (mapToPlay == -1) //Vote
        {
            //Create list of map vote
            int[] mapsarrIndex = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapsIndexes"]; 
            int[] voteMaps = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapVotes"];

            Instantiate(mapVotePrefab, voteListContent).GetComponent<MapVote>().SetUp(voteMaps[0], mapsPerMode[mode].mapImage[mapsarrIndex[0]]);
            Instantiate(mapVotePrefab, voteListContent).GetComponent<MapVote>().SetUp(voteMaps[1], mapsPerMode[mode].mapImage[mapsarrIndex[1]]);
        }
        else //Map selected
        {
            GameObject mapButton = Instantiate(mapSelectedPrefab, voteListContent);

            int imageIndex = mapsPerMode[mode].indexes.ToList().FindIndex(0, mapsPerMode[mode].indexes.Length, x => x == mapToPlay);  
            mapButton.transform.GetChild(0).GetComponent<Image>().sprite = mapsPerMode[mode].mapImage[imageIndex]; 
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.ERROR); 
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed to join the room: " + message;
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.ERROR);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        //In case the host leaves, if we are the new host set the start game button to true, else false
        startGameButton.SetActive(PhotonNetwork.IsMasterClient); 
    }

    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu(MenuManager.MenuType.MULTIPLAYER);

        //Clear the list of players when leaving the room
        if (roomListContent)
        {
            foreach (Transform trans in roomListContent)
            {
                if(trans) Destroy(trans.gameObject);
            }
        }  
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var item in roomList)
        {
            int index = currentRoomList.FindIndex(x => x.Name == item.Name);
            if(index == -1) 
                currentRoomList.Add(item);
            else
            {
                currentRoomList.RemoveAt(index);
                currentRoomList.Add(item);
            }    
        }
        currentRoomList.RemoveAll(x => x.RemovedFromList);

        FindRooms(findDropDown);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if(propertiesThatChanged.ContainsKey("mapVotes"))
        {
            int[] votes = (int[])PhotonNetwork.CurrentRoom.CustomProperties["mapVotes"]; 
            if(votes != null)
            {
                for (int i = 0; i < votes.Length; i++)
                {
                    voteListContent.GetChild(i).GetComponent<MapVote>().SetVote(votes[i]);
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) //When a player enters he room (NOT US)
    { 
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer); 
    }
}
