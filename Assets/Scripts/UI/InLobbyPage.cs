using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SV.UI
{
    public class InLobbyPage : BasePage
    {

        [SerializeField]
        private Button m_StartGameButton;

        [SerializeField]
        private Button m_ReturnButton;

        [SerializeField]
        private Toggle m_ReadyToggle;

        [SerializeField]
        private Toggle m_PrivateToggle;

        [SerializeField]
        private TMPro.TextMeshProUGUI m_LobbyName;

      



        [SerializeField]
        private LobbyPlayerListItem m_ListItemPrfab;

        [SerializeField]
        private RectTransform m_PlayersList;

        private List<LobbyPlayerListItem> listItems = new List<LobbyPlayerListItem>();

        public static InLobbyPage Instance { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            m_ReturnButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.LeaveLobby();


            });
            m_StartGameButton.onClick.AddListener(async () =>
            {

                var regionList = await RelayService.Instance.ListRegionsAsync();
                var targetRegion = regionList[0].Id;


                var allocation = await RelayService.Instance.CreateAllocationAsync(LobbyManager.maxPlayers, targetRegion);

                var joinCodeTask = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                
                await LobbyManager.Instance.UpdateLobby(
                    new UpdateLobbyBuilder()
                    .SetLobbyStatus(LobbyState.InGame));
                
            });



        }

        static RelayServerData HostRelayData(Allocation allocation, string connectionType = "dtls")
        {
            // Select endpoint based on desired connectionType
            var endpoint = RelayUtilities.GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
            if (endpoint == null)
            {
                throw new InvalidOperationException($"endpoint for connectionType {connectionType} not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            // Prepare the Relay server data and compute the nonce value
            // The host passes its connectionData twice into this function
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref connectionData, ref key, connectionType == "dtls");

            return relayServerData;
        }


        private async void UpdateReadyToggle(bool value)
        {
            m_ReadyToggle.interactable = false;

            if (value)
            {
                await LobbyManager.Instance.UpdatePlayerAsync(new UpdatePlayerBuilder().SetPlayerStatus(PlayerStatus.Ready));
            }
            else
            {
                await LobbyManager.Instance.UpdatePlayerAsync(new UpdatePlayerBuilder().SetPlayerStatus(PlayerStatus.Lobby));
            }
        }
        public override void Show()
        {
            base.Show();



            m_ReadyToggle.onValueChanged.AddListener(UpdateReadyToggle);


            LobbyManager.Instance.OnLobbyChanged += Instance_OnLobbyChanged;

            LobbyManager.Instance.OnKicked += LobbyEventCallbacks_KickedFromLobby;

            UpdatePageData();
        }

        private void Instance_OnLobbyChanged(Unity.Services.Lobbies.Models.Lobby obj)
        {
            Debug.Log("LobbyManager LobbyEventConnectionStateChanged callback");
            UpdatePageData();

           

        }

        private async void UpdatePageData()
        {
            var isHost = LobbyManager.Instance.IsHost();
            var readyPlayers = LobbyManager.Instance.Lobby.GetReadyPlayersCount();
            m_LobbyName.text = $"name: {LobbyManager.Instance.GetLobbyName()}" +
               $" code:{LobbyManager.Instance.GetLobbyCode()}" +
               $" id:{LobbyManager.Instance.GetLobbyId()}" +
               $" lobbyState: {LobbyManager.Instance.Lobby.GetLobbyState()}" +
               $" readyPlayers: {LobbyManager.Instance.Lobby.GetReadyPlayersCount()}" +
               $" isHost: {isHost}";

            UpdatePlayersList();



            m_PrivateToggle.interactable = isHost;

           
            m_PrivateToggle.onValueChanged.RemoveListener(UpdatePrivateToggle);
            m_PrivateToggle.isOn = LobbyManager.Instance.Lobby.IsPrivate;
            m_PrivateToggle.onValueChanged.AddListener(UpdatePrivateToggle);
          

           

            m_ReadyToggle.onValueChanged.RemoveListener(UpdateReadyToggle);

            var status = LobbyManager.Instance.GetLocalPlayer().GetStatus();
            m_ReadyToggle.isOn = status == PlayerStatus.Ready ? true : false;
            m_ReadyToggle.interactable = true;
            m_ReadyToggle.onValueChanged.AddListener(UpdateReadyToggle);

            m_StartGameButton.gameObject.SetActive(isHost && readyPlayers == LobbyManager.Instance.Lobby.Players.Count);

            if (LobbyManager.Instance.Lobby.GetLobbyState() == LobbyState.InGame)
            {
                foreach (var world in World.All)
                {
                    if (world.Flags.HasFlag(WorldFlags.GameClient))
                    {
                        World.DefaultGameObjectInjectionWorld = world;
                    }
                }

                var asyncOp = SceneManager.UnloadSceneAsync(1);
                asyncOp.completed += (op) =>
                {
                    SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
                };

               
            }
        }

        private async static void UpdatePrivateToggle(bool value)
        {


            await LobbyManager.Instance.UpdateLobby(new UpdateLobbyBuilder().SetPrivate(value));

        }

        private void LobbyEventCallbacks_KickedFromLobby()
        {



            ModalWindow.Instance.Show("Lobby", "You was kicked from the lobby!", () =>
            {
                UINavigationManager.Instance.Pop();
            });

        }

        public override void Hide(bool onlyDosableInput = false)
        {
            base.Hide(onlyDosableInput);
            if (LobbyManager.Instance)
            {
                LobbyManager.Instance.OnLobbyChanged -= Instance_OnLobbyChanged;
                LobbyManager.Instance.OnKicked -= LobbyEventCallbacks_KickedFromLobby;
            }
        }


        private void UpdatePlayersList()
        {


            ClearList();
            var lobby = LobbyManager.Instance.Lobby;

            var isHostCreated = LobbyManager.Instance.IsHost();

            foreach (var player in lobby.Players)
            {
                var data = Instantiate(m_ListItemPrfab, m_PlayersList);

                player.TryGetDisplayName(out var DisplayName);

                data.Setup(DisplayName, player.IsHost(lobby), player.Id, isHostCreated, player.GetStatus() == PlayerStatus.Ready);

                listItems.Add(data);
            }
        }

        private void ClearList()
        {

            foreach (var item in listItems)
            {
                Destroy(item.gameObject);
            }

            listItems.Clear();
        }
    }

}
