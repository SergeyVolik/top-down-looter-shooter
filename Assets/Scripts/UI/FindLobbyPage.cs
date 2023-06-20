using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    public class JoinLobbyPage : BasePage
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
        private LobbyUIListItem m_ListItemPrefab;

        [SerializeField]
        private Transform m_LobyItemsListParent;

        [SerializeField]
        private TMPro.TMP_Text m_NoLobbiesText;

        
        private List<LobbyUIListItem> m_LobbyItems = new List<LobbyUIListItem>();

        protected override void Awake()
        {
            base.Awake();

            m_RefreshButton.onClick.AddListener(RefershListOfLobbies);
        }

        private async void RefershListOfLobbies()
        {
            LobbyManager.Instance.RefreshLobbies();

            for (int i = 0; i < m_LobbyItems.Count; i++)
            {
                Destroy(m_LobbyItems[i].gameObject);
            }

            m_LobbyItems.Clear();

            var rndItems = Random.Range(0, 3);

            m_NoLobbiesText.text = "Loading...";
            m_NoLobbiesText.gameObject.SetActive(true);

            await Task.Delay(1000);

            if (rndItems != 0)
            {
                m_LobyItemsListParent.gameObject.SetActive(true);
                m_NoLobbiesText.gameObject.SetActive(false);
                for (int i = 0; i < rndItems; i++)
                {
                    var item = Instantiate(m_ListItemPrefab, m_LobyItemsListParent);
                    item.Setup($"Lobby{i}", 1, 4);
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
