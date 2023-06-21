using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    public class CreateLobbyPage : BasePage
    {
        [SerializeField]
        private Button m_CreateLobby;

        [SerializeField]
        private TMPro.TMP_InputField m_LobbyName;

        [SerializeField]
        private TMPro.TMP_InputField m_Lobbypassword;

        [SerializeField]
        private Toggle m_PrivateToggle;

        [SerializeField]
        private InLobbyPage m_InLobbyPage;

        protected override void Awake()
        {
            base.Awake();

            m_CreateLobby.onClick.AddListener(async () =>
            {
               
                await LobbyManager.Instance.CreateLobby(lobbyName: m_LobbyName.text,  isPrivate: m_PrivateToggle.isOn, password: m_Lobbypassword.text);
                UINavigationManager.Instance.Pop();
                UINavigationManager.Instance.Navigate(m_InLobbyPage);
            });
        }
    }

}
