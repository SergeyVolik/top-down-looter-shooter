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
            m_PassCode.text = LobbyManager.Instance.GetLobbyCode();
            m_LobbyName.text = LobbyManager.Instance.GetLobbyName();

            UpdatePlayersList();
        }

        private void UpdatePlayersList()
        {
            var players = LobbyManager.Instance.GetPlayerData();

            ClearList();

            foreach (var player in players)
            {
                var data = Instantiate(m_ListItemPrfab, m_PlayersList);

                data.Setup(player.DysplayName, LobbyManager.Instance.IsHost());

                listItems.Add(data);
            }
        }

        private void ClearList()
        {

            foreach (var item in listItems)
            {
                Destroy(item);
            }

            listItems.Clear();
        }
    }

}
