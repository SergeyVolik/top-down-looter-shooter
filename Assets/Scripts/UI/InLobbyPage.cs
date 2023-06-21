using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        private TMPro.TextMeshProUGUI m_PassCode;
        [SerializeField]
        private TMPro.TextMeshProUGUI m_LobbyName;


        [SerializeField]
        private LobbyPlayerListItem m_ListItemPrfab;

        [SerializeField]
        private RectTransform m_PlayersList;

        private List<LobbyPlayerListItem> listItems = new List<LobbyPlayerListItem>();
        protected override void Awake()
        {
            base.Awake();

            m_ReturnButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.LeaveLobby();
            });


        }

        public override void Show()
        {
            base.Show();
           
            m_LobbyName.text = $"name: { LobbyManager.Instance.GetLobbyName()} code:{LobbyManager.Instance.GetLobbyCode()} id:{LobbyManager.Instance.GetLobbyId()} ";

            UpdatePlayersList();

            LobbyManager.Instance.OnLobbyChanged += Instance_OnLobbyChanged;
           
            LobbyManager.Instance.LobbyEventCallbacks.KickedFromLobby += LobbyEventCallbacks_KickedFromLobby;
        }

        private void Instance_OnLobbyChanged(Unity.Services.Lobbies.Models.Lobby obj)
        {
            Debug.Log("LobbyManager LobbyEventConnectionStateChanged callback");

            UpdatePlayersList();
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
                LobbyManager.Instance.LobbyEventCallbacks.KickedFromLobby -= LobbyEventCallbacks_KickedFromLobby;
            }
        }


        private void UpdatePlayersList()
        {
            

            ClearList();
            var lobby = LobbyManager.Instance.Lobby;


            foreach (var player in lobby.Players)
            {
                var data = Instantiate(m_ListItemPrfab, m_PlayersList);

                player.TryGetDisplayName(out var DisplayName);

                data.Setup(DisplayName, player.IsHost(lobby));

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
