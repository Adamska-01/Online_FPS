using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using static NetworkEventQueue;


public class NetworkEventManager : /*MonoBehaviourPunCallbacks*/ MonoBehaviour
{
    public INetworkEventQueue networkEventQueue;

    private void Start()
    {
        networkEventQueue = new NetworkEventQueue();
    }


	private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(networkEventQueue);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(networkEventQueue);
    }
}

public interface IEventQueue
{
	public void RegisterEventHandler(NetworkEventCodes eventCode, QueuedEventHandler handler);

	public void SendEvent(NetworkEventData networkEvent);
}

public interface INetworkEventQueue : IEventQueue
{
}

public interface ILocalEventQueue : IEventQueue
{ 
}


public class NetworkEventQueue : INetworkEventQueue, IOnEventCallback
{
	private static readonly Dictionary<NetworkEventCodes, Func<object[], NetworkEventData>> networkEventConstructors = new Dictionary<NetworkEventCodes, Func<object[], NetworkEventData>>();
	
    private static readonly Dictionary<NetworkEventCodes, QueuedEventHandler> eventHandlerDictionary = new Dictionary<NetworkEventCodes, QueuedEventHandler>();


    public delegate void QueuedEventHandler(NetworkEventData networkEvent);

    private object eventHandlerLock; // TODO: Dunno how to make it work, research about locks


    public void RegisterEventHandler(NetworkEventCodes eventCode, QueuedEventHandler handler)
    {
        lock (eventHandlerLock)
        {
            if (eventHandlerDictionary.ContainsKey(eventCode))
            {
                eventHandlerDictionary[eventCode] += handler;
            }
            else
            {
                eventHandlerDictionary.Add(eventCode, handler);
            }
        }
    }

    public void SendEvent(NetworkEventData networkEvent)
    {
        try
        {
		    if (!eventHandlerDictionary.ContainsKey(networkEvent.EventCode)) 
				throw new Exception($"You are trying to send an event of type '{networkEvent.GetType()}' that has not been registered yet.");

            if (!networkEventConstructors.ContainsKey(networkEvent.EventCode))
            {
                networkEventConstructors.Add(networkEvent.EventCode, networkEvent.CreateNetworkEventData);
            }

            //Send package
            PhotonNetwork.RaiseEvent(
                (byte)networkEvent.EventCode,
                networkEvent.GetNetworkPackage(),
                networkEvent.EventOptions, //Send only to master client
                networkEvent.SendOptions);
        }
        catch (Exception ex)
        {
            Debug.Log($"An error has occurred while sending a network event: {ex}");
        }
    }

    public void OnEvent(EventData @event)
    {
        // Range of Code is 0-256. Anything above 200 is reserved by the Photon system for handling internal things
        if (@event.Code >= 200)
            return;

        if (!eventHandlerDictionary.TryGetValue((NetworkEventCodes)@event.Code, out var eventHandler))
        {
            Debug.LogError($"You are trying to execute an event of type '{@event.Code}' that has not been registered yet.");
            return;
        }

        if (!networkEventConstructors.TryGetValue((NetworkEventCodes)@event.Code, out var constructor))
        {
            Debug.LogError($"No constructor registered for event code '{@event.Code}'");
            return;
        }

        var networkEvent = constructor(@event.CustomData as object[]);
        eventHandler?.Invoke(networkEvent);
    }
}

// TODO: make an interface with the methods below (cannot instantiate an abstract class)
public abstract class NetworkEventData
{
    public RaiseEventOptions EventOptions { get; }

    public SendOptions SendOptions { get; }


    public NetworkEventCodes EventCode;


    protected NetworkEventData(RaiseEventOptions eventOption, SendOptions sendOptions) 
    {
        EventOptions = eventOption;
        SendOptions = sendOptions;
    }


    public abstract object[] GetNetworkPackage();

    public abstract NetworkEventData CreateNetworkEventData(object[] networkPackage);
}


// Example of event
public class NewPlayerEvent : NetworkEventData
{
    public string PlayerName { get; }


    public NewPlayerEvent(string playerName)
        : base(
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions { Reliability = true })
    {
        EventCode = NetworkEventCodes.NewPlayer;
        PlayerName = playerName;
    }


    public override object[] GetNetworkPackage()
        => new object[] 
        {
            PlayerName
        };

    public override NetworkEventData CreateNetworkEventData(object[] networkPackage)
        => new NewPlayerEvent((string)networkPackage[0]);
}

//Events
public enum NetworkEventCodes : byte
{
    NewPlayer,
    ListPlayer,
    UpdateStat,
    NextMatch,
    TimerSync,
    MatchSettings,
    CreatePlayer
}