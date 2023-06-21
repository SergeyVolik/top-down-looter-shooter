using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    public class FindLobbyPage : BasePage
    {
        [SerializeField]
        private Button m_RefreshButton;
        [SerializeField]
        private Button m_JoinButton;
        [SerializeField]
        private TMPro.TMP_InputField m_LobbyName;
        [SerializeField]
        private TMPro.TMP_InputField m_Lobbypassword;
        [SerializeField]
        private TMPro.TMP_InputField m_NickName;
        [SerializeField]
        private LobbyUIListItem m_ListItemPrefab;

        [SerializeField]
        private Transform m_LobyItemsListParent;

        [SerializeField]
        private TMPro.TMP_Text m_NoLobbiesText;

        
        private List<LobbyUIListItem> m_LobbyItems = new List<LobbyUIListItem>();

        protected override void Awake()
        {
            base.Awake();

            m_NickName.text = LobbyManager.Instance.localPlayer.DysplayName;
            m_NickName.onValueChanged.AddListener((v) =>
            {
                LobbyManager.Instance.localPlayer.DysplayName = v;
            });

            m_RefreshButton.onClick.AddListener(RefershListOfLobbies);


        }

        private async void RefershListOfLobbies()
        {
            for (int i = 0; i < m_LobbyItems.Count; i++)
            {
                Destroy(m_LobbyItems[i].gameObject);
            }

            m_LobbyItems.Clear();

            m_NoLobbiesText.text = "Loading...";
            m_NoLobbiesText.gameObject.SetActive(true);

            var queryResult = await LobbyManager.Instance.QueryLobbies();
          

            
            if (queryResult.Results.Count != 0)
            {
                m_LobyItemsListParent.gameObject.SetActive(true);
                m_NoLobbiesText.gameObject.SetActive(false);
                for (int i = 0; i < queryResult.Results.Count; i++)
                {
                    var lobby = queryResult.Results[i];
                    var item = Instantiate(m_ListItemPrefab, m_LobyItemsListParent);
                    item.Setup($"Lobby{lobby.Name}", lobby.Players.Count, lobby.MaxPlayers);
                    m_LobbyItems.Add(item);
                }

            }
            else {
                m_LobyItemsListParent.gameObject.SetActive(false);
                m_NoLobbiesText.gameObject.SetActive(true);
                m_NoLobbiesText.text = "No Lobbies";
            }
        }
        public override void Show()
        {
            base.Show();

            RefershListOfLobbies();
        }
        private void UpdateContent()
        {
            
        }
    }

}
