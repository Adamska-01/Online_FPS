using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private NETInputManager inputManger;
    private PlayerManager playerManager;
    public GameObject deathCamera;
    public IEnumerator CO_kill;
    public IEnumerator CO_NextMatch;

    public static MatchManager instance;
    private void Awake()
    {
        instance = this; 
    }

    //Events
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayer,
        UpdateStat,
        NextMatch,
        TimerSync,
        MatchSettings,
        CreatePlayer
    }


    public List<PlayerInfo> players = new List<PlayerInfo>();
    private List<Leaderboard> lboardPlayers = new List<Leaderboard>();
    public int index { get; private set; } 

    //Game ending 
    public enum GameStates
    {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin = 3; 
    public GameStates state = GameStates.Waiting;
    private float waitAfterEnding = 10.0f; 
    private float SettingsWaitTime = 20.0f; 

    //Timer
    public float matchLength = 180f;
    private float currentMatchTime;
    private float countdownTimerEndGame;
    private float countdownTimerStartGame;
    private float sendTimer; 

    void Start()
    {
        PlayerManager[] playerMngr = FindObjectsOfType<PlayerManager>();
        foreach (var item in playerMngr)
        {
            if (item.PV.IsMine)
                playerManager = item;
        }

        inputManger = FindObjectOfType<NETInputManager>();

        countdownTimerEndGame = waitAfterEnding;
        countdownTimerStartGame = SettingsWaitTime;

        if (!PhotonNetwork.IsConnected)
            SceneManager.LoadScene(0);
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            StartCoroutine(MatchSettings());

            //SetupTimer();
        }
    }


    void Update()
    {
        if (inputManger.ShowLeaderboard && !NETUIController.instance.leaderboard.gameObject.activeInHierarchy && state != GameStates.Ending)
            ShowLeaderboard();
        else if (!inputManger.ShowLeaderboard && NETUIController.instance.leaderboard.gameObject.activeInHierarchy && state != GameStates.Ending)
        {
            NETUIController.instance.leaderboard.SetActive(false); 
            state = GameStates.Playing;
        }

        //Timer
        if (PhotonNetwork.IsMasterClient)
        { 
            if (currentMatchTime >= 0.0f && state == GameStates.Playing)
            {
                currentMatchTime -= Time.deltaTime; 
                if (currentMatchTime <= 0.0f)
                {
                    currentMatchTime = 0.0f;

                    state = GameStates.Ending;

                    ListPlayersSend(); //Keeps state up to date and checks state 
                }

                UpdateTimerDisplay();

                //Send current time every second 
                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0.0f)
                {
                    sendTimer += 1.0f;

                    TimerSend();
                }
            }
        }
    }


    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        //Avoid Errors
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        /*
         *Range of .Code is 0-256. anything above 200 is are
         *Reserved by the Photon system for handling things
        */
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;


            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCodes.ListPlayer:
                    ListPlayerReceive(data);
                    break;
                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
                case EventCodes.NextMatch:
                    NextMatchReceive();
                    break;
                case EventCodes.TimerSync:
                    TimerReceive(data);
                    break;
                case EventCodes.MatchSettings:
                    SettingsReceive(data);
                    break;
                case EventCodes.CreatePlayer:
                    CreatePlayerReceive();
                    break;
            }
        }
    }


    //-------------------------Events <Send/Receive>-------------------------
    public void NewPlayerSend(string username)
    {
        object[] package = new object[4]; //4: name, actor, kills, deaths;
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        //Send package
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, //Send only to master client
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);
        
        //Sometimes a bug occours and I have two instances of the same player in the leaderboard
        int index = players.FindIndex(x => x.actor == player.actor);
        if (index == -1) 
            players.Add(player);

        //Update list 
        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        //Construct the list
        object[] package = new object[players.Count + 1]; //+1 for sending game state
        package[0] = state;

        for (int i = 0; i < players.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = players[i].name;
            piece[1] = players[i].actor;
            piece[2] = players[i].kills;
            piece[3] = players[i].deaths;

            package[i + 1] = piece; //+1 cause state is at index 0
        }
        
        //Send package (list)
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all clients
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void ListPlayerReceive(object[] dataReceived)
    {
        players.Clear(); //all players cleared

        state = (GameStates)dataReceived[0]; //Update state

        //recreating the list
        for (int i = 1; i < dataReceived.Length; i++) //i=1 cause state was at 0
        {
            object[] pieceOfPlayer = (object[])dataReceived[i]; //player piece is being stored
            PlayerInfo player = new PlayerInfo(
                (string)pieceOfPlayer[0],
                (int)pieceOfPlayer[1],
                (int)pieceOfPlayer[2],
                (int)pieceOfPlayer[3]);

            players.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)   //assigning our index
                index = i - 1; //-1 cause of the state...
        }

        if(PhotonNetwork.IsMasterClient) 
            SettingsSend();

        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange, string killerOrKilled)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange, killerOrKilled };

        //Send package (list)
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all clients
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].actor == actor)
            {
                switch (statType)
                {
                    case 0: //kills
                        players[i].kills += amount;

                        //Update UI text
                        if (PhotonNetwork.LocalPlayer.ActorNumber == players[i].actor)
                        {
                            NETUIController.instance.currentKillText.text = "You Have <color=red>Killed</color> " + "<color=blue>" + (string)dataReceived[3] + "</color>";
                            if (CO_kill != null)
                                StopCoroutine(CO_kill); 
                            CO_kill = KillTextActivation();
                            StartCoroutine(CO_kill); 
                        } 
                        break;
                    case 1: //deaths
                        players[i].deaths += amount;

                        //Update UI text
                        TMP_Text killFeed = Instantiate(NETUIController.instance.killsFeedPrefab, NETUIController.instance.KillsFeed.transform).GetComponent<TMP_Text>();
                        killFeed.text = (string)dataReceived[3] + " <color=red>Killed</color> " + players[i].name; 
                        break;
                }
                //if that player is us, update stats
                if (i == index)
                {
                    UpdateStatsDisplay();
                }

                //Update leaderboard while active
                if (NETUIController.instance.leaderboard.activeInHierarchy)
                    ShowLeaderboard();

                break;
            }
        }

        ScoreCheck();
    }

    private void UpdateStatsDisplay()
    {
        if (players.Count > index)
        {
            NETUIController.instance.killsIndicator.text = $"Kills: <b><color=green>{players[index].kills}</color></b>";
            NETUIController.instance.deathsIndicator.text = $"Deaths: <b><color=red>{players[index].deaths}</color></b>";
        }
        else
        {
            NETUIController.instance.killsIndicator.text = "Kills: <b><color=green>0</color></b>";
            NETUIController.instance.deathsIndicator.text = "Deaths: <b><color=red>0</color></b>";
        }
    }

    public void NextMatchSend()
    { 
        //Send package
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void NextMatchReceive()
    { 
        if (CO_NextMatch != null)
        {
            StopCoroutine(CO_NextMatch);
            CO_NextMatch = null;
        }
        state = GameStates.Playing;

        NETUIController.instance.endScreen.SetActive(false);
        NETUIController.instance.leaderboard.SetActive(false);
        NETUIController.instance.OpenPanel(PanelType.HUD);
        deathCamera.SetActive(false);

        foreach (PlayerInfo player in players)
        {
            player.kills = 0;
            player.deaths = 0;
        }
        
        UpdateStatsDisplay();

        playerManager.CreateController();

        //Reset timer
        SetupTimer();
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int)currentMatchTime, (int)countdownTimerEndGame, (int)countdownTimerStartGame, state };

        //Send package
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void TimerReceive(object[] dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        countdownTimerEndGame = (int)dataReceived[1];
        countdownTimerStartGame = (int)dataReceived[2];
        state = (GameStates)dataReceived[3];

        UpdateTimerDisplay(); 
    }

    public void SettingsSend()
    {
        object[] package = new object[] { (int)matchLength, killsToWin };

        //Send package
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.MatchSettings,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void SettingsReceive(object[] dataReceived)
    {
        matchLength = (int)dataReceived[0];
        killsToWin = (int)dataReceived[1]; 
    }

    public void CreatePlayerSend()
    { 
        //Send package
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.CreatePlayer,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, //Send to all
            new SendOptions { Reliability = true } //TCP is always reliable by design
            );
    }

    public void CreatePlayerReceive()
    { 
        playerManager.CreateController();
        deathCamera.SetActive(false);

        NETUIController.instance.OpenPanel(PanelType.HUD);

        SetupTimer();

        state = GameStates.Playing;
    }

    //-------------------------Helper Functions-------------------------
    public void SetupTimer()
    {
        if (matchLength > 0)
        {
            currentMatchTime = matchLength;
            countdownTimerEndGame = waitAfterEnding;
            countdownTimerStartGame = SettingsWaitTime;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        NETUIController.instance.timerText.text = timeToDisplay.Minutes.ToString("0") + ":" + timeToDisplay.Seconds.ToString("00");
        if (currentMatchTime <= 30) NETUIController.instance.timerText.text = $"<color=red>{NETUIController.instance.timerText.text}</color>";

        NETUIController.instance.nextMatchtimeTextEnd.text = "Next Round In: <color=red>" + countdownTimerEndGame.ToString("0") + "</color>";
        NETUIController.instance.matchtimeTextStartHost.text = "Match Starts In: <color=red>" + countdownTimerStartGame.ToString("0") + "</color>";
        NETUIController.instance.matchtimeTextStartClient.text = "Match Starts In: <color=red>" + countdownTimerStartGame.ToString("0") + "</color>";
    }

    private void ShowLeaderboard()
    {
        //Show leaderboard
        NETUIController.instance.leaderboard.SetActive(true);

        //Clear previous leaderboard list 
        foreach (Leaderboard lp in lboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear(); 

        //Sort list by the highest kills
        List<PlayerInfo> sortedPlayerList = players.OrderByDescending(x => x.kills).ToList();

        //Create new leaderboard
        foreach (PlayerInfo player in sortedPlayerList)
        {
            Leaderboard newPlayerDisplay = Instantiate(NETUIController.instance.leaderboardPlayerDisplay, NETUIController.instance.leaderboard.transform);

            newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths, (PhotonNetwork.LocalPlayer.NickName == player.name && PhotonNetwork.LocalPlayer.ActorNumber == player.actor));

            newPlayerDisplay.gameObject.SetActive(true);

            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in players)
        {
            if (player.kills >= killsToWin && killsToWin > 0) //0 == no kill limit
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameStates.Ending)
            {
                state = GameStates.Ending;
                ListPlayersSend();
            }
        }
    }

    private void StateCheck()
    {
        if (state == GameStates.Ending)
        {  
            EndGame();
        }
    }

    private void EndGame()
    {
        state = GameStates.Ending; //makes sure 

        //Death camera
        deathCamera.SetActive(true);

        //Show UI
        NETUIController.instance.OpenPanel(PanelType.END);
        ShowLeaderboard();

        //Activate cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CO_NextMatch == null)
        {
            CO_NextMatch = EndCO();
            StartCoroutine(CO_NextMatch);
        }
    } 

    private IEnumerator EndCO()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            countdownTimerEndGame = waitAfterEnding;
            while (countdownTimerEndGame > 0.0f)
            {
                --countdownTimerEndGame;
                TimerSend();
                
                yield return new WaitForSeconds(1);
            } 
             
            NextMatchSend(); 
        }
    }

    private IEnumerator KillTextActivation()
    {
        NETUIController.instance.currentKillText.CrossFadeAlpha(1.0f, 0.0f, true);
        NETUIController.instance.currentKillText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3.0f);

        NETUIController.instance.currentKillText.CrossFadeAlpha(0.0f, 2.0f, true);
        while (NETUIController.instance.currentKillText.color.a > 0.0f)
        { 
            yield return null;
        }

        NETUIController.instance.currentKillText.gameObject.SetActive(false);
    }

    private IEnumerator MatchSettings()
    {  
        var settingsPanel = NETUIController.instance.GetPannel(PanelType.MATCH_SETTINGS);
        var waitingPanel = NETUIController.instance.GetPannel(PanelType.MATCH_WAITING);
        countdownTimerStartGame = SettingsWaitTime;
        while (countdownTimerStartGame > 0.0f)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!settingsPanel.isOpen) NETUIController.instance.OpenPanel(PanelType.MATCH_SETTINGS);
                --countdownTimerStartGame;
                TimerSend();

                yield return new WaitForSeconds(1); 
            }
            else
            {  
                if (!waitingPanel.isOpen) 
                    NETUIController.instance.OpenPanel(PanelType.MATCH_WAITING); 
                if (state == GameStates.Playing)
                {
                    playerManager.CreateController();
                    NETUIController.instance.OpenPanel(PanelType.HUD); 
                    deathCamera.SetActive(false);
                    break;
                }
                yield return null; 
            }
        }

        if(PhotonNetwork.IsMasterClient)
        {
            //<<Match settings are set with onClick() events>> 
            SettingsSend();
            CreatePlayerSend();
        }
    }

    public void StartGameWithoutWaiting()
    {
        countdownTimerStartGame = 0.0f;
    }

    //-----------------------------Photon Callbacks-----------------------------
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int index = players.FindIndex(x => x.name == otherPlayer.NickName);
        if (index != -1)
            players.RemoveAt(index);
        else //no need for all clients to execute the rest
            return;

        ListPlayersSend();
        //PhotonNetwork.SendAllOutgoingCommands(); //Send this immediately
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();  
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            SceneManager.LoadScene(0);
        }
    }  
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor;
    public int kills;
    public int deaths;

    public PlayerInfo()
    {
        name = string.Empty;
        kills = deaths = 0;
    }
    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}