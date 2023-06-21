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


public class LobbyManager : MonoBehaviour
{
    [Range(1, 12)]
    public int maxLobbies = 1;
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
    public LocalPlayer localPlayer = new LocalPlayer();
    
    private bool m_Awaked;

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

    
    public LobbyEventCallbacks LobbyEventCallbacks { get; private set; } = new LobbyEventCallbacks();


    public event Action<Lobby> OnLobbyChanged = delegate { };
    public async Task BindLocalLobbyToRemote(string lobbyID)
    {
     

        LobbyEventCallbacks.LobbyChanged += (data) =>
        {
            data.ApplyToLobby(m_CurrentLobby);
            OnLobbyChanged.Invoke(m_CurrentLobby);
           


        };

        LobbyEventCallbacks.KickedFromLobby += () =>
        {
            Debug.Log("Left Lobby");
            Dispose();
        };

        LobbyEventCallbacks.LobbyEventConnectionStateChanged += (eventData) =>
        {
            Debug.Log($"Lobby ConnectionState Changed to {eventData}");
        };

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, LobbyEventCallbacks);

        Debug.Log("LobbyManager SubscribeToLobbyEventsAsync executed");
    }


    void ParseCustomPlayerData(LocalPlayer player, string dataKey, string playerDataValue)
    {

        
            player.DisplayName.Value = playerDataValue;
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

    private void Awake()
    {
        if (m_Awaked == true)
            return;

        m_Awaked = true;



        SetupLocalPlayer();


    }

    private void SetupLocalPlayer()
    {
        if (PlayerPrefs.HasKey(nameof(localPlayer.DisplayName)))
            localPlayer.DisplayName.Value = PlayerPrefs.GetString(nameof(localPlayer.DisplayName));
        else
        {
            localPlayer.DisplayName.Value = $"Player{UnityEngine.Random.Range(0, 100)}";
        }
    }

    private void OnDestroy()
    {
        PlayerPrefs.SetString(nameof(localPlayer.DisplayName), localPlayer.DisplayName.Value);
    }



    public bool IsHost() => m_CurrentLobby != null && m_CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

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

            Debug.Log("Left from lobby");

            Dispose();
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

    Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer user)
    {
        Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

        var displayNameObject =
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
        data.Add(LocalPlayer.key_DisplayName, displayNameObject);
        return data;
    }

    public async Task CreateLobby(string lobbyName, bool isPrivate, string password = null, int maxPlayers = 4)
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

        var initplayerData = CreateInitialPlayerData(localPlayer);

        options.IsPrivate = isPrivate;

        options.Player = new Player(
            id: AuthenticationService.Instance.PlayerId,
            data: initplayerData);

        options.Data = new Dictionary<string, DataObject>()
        {
            {
                "Password", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public, // Visible publicly.
                    value: "25",
                    index: DataObject.IndexOptions.N1)
            },
        };


        m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

    
       

        await BindLocalLobbyToRemote(m_CurrentLobby.Id);

        StartHeartBeat();


    }



    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId, string lobbyCode, LocalPlayer localUser,
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
        RemoveCreateLobbies();
    }

    private void RemoveCreateLobbies()
    {
        DeleteCurrentLobby();

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

[Serializable]
public class LocalPlayer
{
    
    public CallbackValue<string> DisplayName = new CallbackValue<string>("");
   
    public const string key_DisplayName = nameof(DisplayName);
    

    public LocalPlayer()
    {

    }
}



public static class LobbyExtention
{
   

}
public static class PlayerExtention
{
    public static bool IsHost(this Player player, Lobby lobby)
    {
        return lobby.HostId == player.Id;
    }

    public static bool TryGetDisplayName(this Player player, out string DisplayName)
    {

        var result = player.Data.TryGetValue(LocalPlayer.key_DisplayName, out var name);
        DisplayName = name.Value;
        return result;
    }
}