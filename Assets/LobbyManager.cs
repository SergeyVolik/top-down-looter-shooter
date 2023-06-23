using ProjectDawn.Navigation.Sample.Zerg;
using SV.ECS;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

[Flags]
public enum PlayerStatus
{
    None = 0,
    Connecting = 1, // User has joined a lobby but has not yet connected to Relay.
    Lobby = 2, // User is in a lobby and connected to Relay.
    Ready = 4, // User has selected the ready button, to ready for the "game" to start.
    InGame = 8, // User is part of a "game" that has started.
    Menu = 16 // User is not in a lobby, in one of the main menus.
}

[Flags] // Some UI elements will want to specify multiple states in which to be active, so this is Flags.
public enum LobbyState
{
    None = 0,
    Lobby = 1,
    CountDown = 2,
    InGame = 4
}

public class LobbyManager : MonoBehaviour
{
    
    public const int maxPlayers = 4;
    public static LobbyManager Instance
    {
        get
        {

            if (m_Instance == null)
            {
                m_Instance = FindAnyObjectByType<LobbyManager>();

                if (m_Instance != null)
                    m_Instance.Awake();
            }

            return m_Instance;
        }
    }


    private static LobbyManager m_Instance;

    private Lobby m_CurrentLobby;
    public Lobby Lobby => m_CurrentLobby;
  

    private bool m_Awaked;

    public string LocalPlayerId => AuthenticationService.Instance.PlayerId;
    public string GetLobbyCode() => m_CurrentLobby?.LobbyCode;
    public string GetLobbyId() => m_CurrentLobby?.Id;
    public string GetLobbyName() => m_CurrentLobby?.Name;

    ServiceRateLimiter m_QueryCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter m_CreateCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter m_JoinCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter m_QuickJoinCooldown = new ServiceRateLimiter(1, 10f);
    ServiceRateLimiter m_GetLobbyCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter m_DeleteLobbyCooldown = new ServiceRateLimiter(2, 1f);
    ServiceRateLimiter m_UpdateLobbyCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter m_UpdatePlayerCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter m_LeaveLobbyOrRemovePlayer = new ServiceRateLimiter(5, 1);
    ServiceRateLimiter m_HeartBeatCooldown = new ServiceRateLimiter(5, 30);

    private Task m_HeartBeatTask;
    private LobbyEventConnectionState m_lastConnectionState;

    public LobbyEventCallbacks LobbyEventCallbacks { get; private set; } = new LobbyEventCallbacks();

    public bool IsHost() => m_CurrentLobby != null && m_CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

    public Player GetLocalPlayer() => m_CurrentLobby.Players.FirstOrDefault(e => e.Id == LocalPlayerId);
    public event Action<Lobby> OnLobbyChanged = delegate { };
    public event Action OnKicked = delegate { };

    private void Awake()
    {
        if (m_Awaked == true)
            return;

        m_Awaked = true;

    }
   

    

    public async Task UpdatePlayerAsync(UpdatePlayerBuilder builder )
    {
        if (!InLobby())
            return;

        if (m_UpdatePlayerCooldown.TaskQueued)
            return;

        await m_UpdatePlayerCooldown.QueueUntilCooldown();


      

        string playerId = AuthenticationService.Instance.PlayerId;

        m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, builder.Build());
    }

    public async Task UpdateLobby(UpdateLobbyBuilder updateOptions)
    {
        if (m_UpdateLobbyCooldown.TaskQueued)
            return;

        await m_UpdateLobbyCooldown.QueueUntilCooldown();

        
        m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions.Build());
    }

  



    public async Task BindLocalLobbyToRemote(string lobbyID)
    {


        LobbyEventCallbacks.LobbyChanged += (data) =>
        {
            Debug.Log("LobbyChanged");
            if (m_CurrentLobby != null)
            {
                data.ApplyToLobby(m_CurrentLobby);
                OnLobbyChanged.Invoke(m_CurrentLobby);


                if (m_CurrentLobby.HostId == LocalPlayerId)
                {
                    StartHeartBeat();
                }
                else
                {
                    StopHeartBeat();
                }

            }
            else
            {
                StopHeartBeat();
            }

        };

        LobbyEventCallbacks.KickedFromLobby += () =>
        {
            Debug.Log("Kicked Executed");

            if (m_lastConnectionState == LobbyEventConnectionState.Unsubscribed)
            {
                OnKicked.Invoke();
                Debug.Log("Left Lobby");
                Dispose();
            }

        };

        LobbyEventCallbacks.LobbyEventConnectionStateChanged += (eventData) =>
        {
            m_lastConnectionState = eventData;
            Debug.Log($"Lobby ConnectionState Changed to {eventData}");
        };

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, LobbyEventCallbacks);

        Debug.Log("LobbyManager SubscribeToLobbyEventsAsync executed");
    }



    public async Task<Lobby> QuickJoinLobbyAsync(LocalPlayerData.LocalPlayer localUser)
    {
        //We dont want to queue a quickjoin
        if (m_QuickJoinCooldown.IsCoolingDown)
        {
            UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
            return null;
        }

        await m_QuickJoinCooldown.QueueUntilCooldown();
        List<QueryFilter> filters = new List<QueryFilter>();


        string uasId = AuthenticationService.Instance.PlayerId;

        var joinRequest = new QuickJoinLobbyOptions
        {
            Filter = filters,
            Player = new Player(id: uasId, data: CreateInitialPlayerData(localUser))
        };

        m_CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinRequest);

        await BindLocalLobbyToRemote(m_CurrentLobby.Id);

        return m_CurrentLobby;
    }

    public bool InLobby()
    {
        if (m_CurrentLobby == null)
        {
            Debug.LogWarning("LobbyManager not currently in a lobby. Did you CreateLobbyAsync or JoinLobbyAsync?");
            return false;
        }

        return true;
    }



   






    public async Task<QueryResponse> QueryLobbies(int items = 25)
    {
        if (m_QueryCooldown.TaskQueued)
            return null;
        await m_QueryCooldown.QueueUntilCooldown();

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = items;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            return lobbies;


            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        return null;
    }

    public void Dispose()
    {
        m_CurrentLobby = null;
        LobbyEventCallbacks = new LobbyEventCallbacks();

    }

    public async void LeaveLobby()
    {
        await m_LeaveLobbyOrRemovePlayer.QueueUntilCooldown();

        if (!InLobby())
            return;

        await KickPlayer(AuthenticationService.Instance.PlayerId);

        Debug.Log("Left from lobby");

        Dispose();
    }

    public async Task KickPlayer(string playerId)
    {
        if (!InLobby())
        {
            return;
        }

        if (!IsHost() && playerId != AuthenticationService.Instance.PlayerId)
        {

            Debug.LogError("You are not Host. You can't kick other players. Only self.");
            return;
        }

        try
        {

            await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, playerId);

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }






    public void DeleteCurrentLobby()
    {
        if (m_CurrentLobby != null)
        {
            LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
            Dispose();
        }

    }

    Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayerData.LocalPlayer user)
    {
        Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

        var displayNameObject =
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
        data.Add(LocalPlayerData.LocalPlayer.key_DisplayName, displayNameObject);
        return data;
    }

    public async Task CreateLobby(string lobbyName, bool isPrivate, int maxPlayers = 4)
    {
        if (m_CreateCooldown.IsCoolingDown)
        {
            Debug.LogWarning("Create Lobby hit the rate limit.");
            return;
        }



        await m_CreateCooldown.QueueUntilCooldown();

        CreateLobbyOptions options = new CreateLobbyOptions();
        // Ensure you sign-in before calling Authentication Instance.
        // See IAuthenticationService interface.

        var initplayerData = CreateInitialPlayerData(LocalPlayerData.Player);

        options.IsPrivate = isPrivate;

        options.Player = new Player(
            id: AuthenticationService.Instance.PlayerId,
            data: initplayerData);


       

        options.Data = new Dictionary<string, DataObject>()
        {
            {
                LobbyExtention.key_LobbyState, new DataObject(
                    visibility: DataObject.VisibilityOptions.Public, // Visible publicly.
                    value: ((int)LobbyState.Lobby).ToString(),
                    index: DataObject.IndexOptions.N1)
            },
        };


        m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);




        await BindLocalLobbyToRemote(m_CurrentLobby.Id);

        StartHeartBeat();


    }



    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId, string lobbyCode, LocalPlayerData.LocalPlayer localUser,
            string password = null)
    {

        if (m_JoinCooldown.IsCoolingDown ||
                (lobbyId == null && lobbyCode == null))
        {
            return null;
        }

        await m_JoinCooldown.QueueUntilCooldown();

        string uasId = AuthenticationService.Instance.PlayerId;
        var playerData = CreateInitialPlayerData(localUser);

        if (!string.IsNullOrEmpty(lobbyId))
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions
            { Player = new Player(id: uasId, data: playerData) };
            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);

            Debug.Log($"Join with lobbyId: {lobbyId}");
        }
        else
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
            { Player = new Player(id: uasId, data: playerData) };
            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            Debug.Log($"Join with lobbyCode: {lobbyCode}");
        }



        await BindLocalLobbyToRemote(m_CurrentLobby.Id);


        return m_CurrentLobby;
    }





    async Task SendHeartbeatPingAsync()
    {
        if (!InLobby())
            return;
        if (m_HeartBeatCooldown.IsCoolingDown)
            return;
        await m_HeartBeatCooldown.QueueUntilCooldown();

        await LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
    }


    void StopHeartBeat()
    {
        if (m_HeartBeatTask != null)
        {
            m_HeartBeatTask.Dispose();
            m_HeartBeatTask = null;
        }
    }
    void StartHeartBeat()
    {
#pragma warning disable 4014
        m_HeartBeatTask = HeartBeatLoop();
#pragma warning restore 4014
    }

    async Task HeartBeatLoop()
    {
        while (m_CurrentLobby != null)
        {
            await SendHeartbeatPingAsync();
            await Task.Delay(8000);
        }
    }


    void OnApplicationQuit()
    {
        LeaveLobby();
       
    }

}

public class CallbackValue<T>
{
    public Action<T> onChanged;


    public CallbackValue()
    {

    }
    public CallbackValue(T cachedValue)
    {
        m_CachedValue = cachedValue;
    }

    public T Value
    {
        get => m_CachedValue;
        set
        {
            if (m_CachedValue != null && m_CachedValue.Equals(value))
                return;
            m_CachedValue = value;
            onChanged?.Invoke(m_CachedValue);
        }
    }

    public void ForceSet(T value)
    {
        m_CachedValue = value;
        onChanged?.Invoke(m_CachedValue);
    }

    public void SetNoCallback(T value)
    {
        m_CachedValue = value;
    }

    T m_CachedValue = default;
}

//Manages the Amount of times you can hit a service call.
//Adds a buffer to account for ping times.
//Will Queue the latest overflow task for when the cooldown ends.
//Created to mimic the way rate limits are implemented Here:  https://docs.unity.com/lobby/rate-limits.html
public class ServiceRateLimiter
{
    public Action<bool> onCooldownChange;
    public readonly int coolDownMS;
    public bool TaskQueued { get; private set; } = false;

    readonly int m_ServiceCallTimes;
    bool m_CoolingDown = false;
    int m_TaskCounter;

    //(If you're still getting rate limit errors, try increasing the pingBuffer)
    public ServiceRateLimiter(int callTimes, float coolDown, int pingBuffer = 100)
    {
        m_ServiceCallTimes = callTimes;
        m_TaskCounter = m_ServiceCallTimes;
        coolDownMS =
            Mathf.CeilToInt(coolDown * 1000) +
            pingBuffer;
    }

    public async Task QueueUntilCooldown()
    {
        if (!m_CoolingDown)
        {
#pragma warning disable 4014
            ParallelCooldownAsync();
#pragma warning restore 4014
        }

        m_TaskCounter--;

        if (m_TaskCounter > 0)
        {
            return;
        }

        if (!TaskQueued)
            TaskQueued = true;
        else
            return;

        while (m_CoolingDown)
        {
            await Task.Delay(10);
        }
    }

    async Task ParallelCooldownAsync()
    {
        IsCoolingDown = true;
        await Task.Delay(coolDownMS);
        IsCoolingDown = false;
        TaskQueued = false;
        m_TaskCounter = m_ServiceCallTimes;
    }

    public bool IsCoolingDown
    {
        get => m_CoolingDown;
        private set
        {
            if (m_CoolingDown != value)
            {
                m_CoolingDown = value;
                onCooldownChange?.Invoke(m_CoolingDown);
            }
        }
    }
}



public class UpdateLobbyBuilder
{
    private UpdateLobbyOptions updateOptions;

    public UpdateLobbyBuilder()
    {
        updateOptions = new UpdateLobbyOptions
        {

             Data = new Dictionary<string, DataObject>(),

        };
    }

    public UpdateLobbyBuilder SetPrivate(bool isPrivate)
    {
        updateOptions.IsPrivate = isPrivate;
        return this;
    }

    public UpdateLobbyBuilder SetLocked(bool isLocked)
    {
        updateOptions.IsLocked = isLocked;
        return this;
    }

    public UpdateLobbyBuilder SetHostId(string hostId)
    {
        updateOptions.HostId = hostId;
        return this;
    }

    public UpdateLobbyBuilder SetMaxPlayers(int maxPlayers)
    {
        updateOptions.MaxPlayers = maxPlayers;
        return this;
    }

    public UpdateLobbyBuilder SetName(string name)
    {
        updateOptions.Name = name;
        return this;
    }

    public UpdateLobbyBuilder SetLobbyStatus(LobbyState state)
    {
        DataObject data = new DataObject(DataObject.VisibilityOptions.Public, ((int)state).ToString(), DataObject.IndexOptions.N1);
        updateOptions.Data.Add(LobbyExtention.key_LobbyState, data);
        return this;
    }

    public UpdateLobbyBuilder SetRaplyCode(string replayCode)
    {
        DataObject data = new DataObject(DataObject.VisibilityOptions.Public, replayCode, DataObject.IndexOptions.S1);
        updateOptions.Data.Add(LobbyExtention.key_RelayCode, data);
        return this;
    }

    public UpdateLobbyOptions Build()
    {
        return updateOptions;
    }

}

public class UpdatePlayerBuilder
{
    private UpdatePlayerOptions updateOptions;

    public UpdatePlayerBuilder()
    {
        updateOptions = new UpdatePlayerOptions
        {

            Data = new Dictionary<string, PlayerDataObject>()

        };
    }

    public UpdatePlayerBuilder SetDiplayName(string name)
    {
        updateOptions.Data.Add(
            LocalPlayerData.LocalPlayer.key_DisplayName,
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name));
                  
        return this;
    }

    public UpdatePlayerBuilder SetPlayerStatus(PlayerStatus status)
    {
        updateOptions.Data.Add(
           PlayerExtention.key_PlayerStatus,
           new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)status).ToString()));
        return this;
    }



    public UpdatePlayerOptions Build()
    {
        return updateOptions;
    }

}

public static class LobbyExtention
{
    public const string key_LobbyState = "LobbyState";
    public const string key_RelayCode = "RelayCode";
    public static LobbyState GetLobbyState(this Lobby Lobby)
    {
        var result = Lobby.Data.TryGetValue(key_LobbyState, out var dataValue);


        if (result)
        {
            return (LobbyState)int.Parse(dataValue.Value);
        }
        return LobbyState.None;

    }

    public static string GetReplayCode(this Lobby Lobby)
    {
        var result = Lobby.Data.TryGetValue(key_RelayCode, out var dataValue);


        if (result)
        {
            return dataValue.Value;
        }

        return null;

    }

    public static int GetReadyPlayersCount(this Lobby Lobby)
    {
        int ready = 0;


        foreach (var item in Lobby.Players)
        {
            if (item.GetStatus() == PlayerStatus.Ready)
            {
                ready++;
            }
        }
        return ready;
    }
}
public static class PlayerExtention
{
    public const string key_PlayerStatus = "PlayerStatus";
    public static bool IsHost(this Player player, Lobby lobby)
    {
        return lobby.HostId == player.Id;
    }

    public static bool TryGetDisplayName(this Player player, out string DisplayName)
    {
        DisplayName = null;

        var result = player.Data.TryGetValue(LocalPlayerData.LocalPlayer.key_DisplayName, out var name);

       
        if (result)
        {
            DisplayName = name.Value;
        }
       
        return result;
    }

    public static PlayerStatus GetStatus(this Player player)
    {
        var result = player.Data.TryGetValue(key_PlayerStatus, out var playerDataValue);

        if (result)
        {
            return (PlayerStatus)int.Parse(playerDataValue.Value);
        }
        return PlayerStatus.None;
    }

  
}