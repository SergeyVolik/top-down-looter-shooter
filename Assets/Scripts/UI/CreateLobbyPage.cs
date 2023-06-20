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

            m_CreateLobby.onClick.AddListener(() =>
            {
                UINavigationManager.Instance.Pop();
                UINavigationManager.Instance.Navigate(m_InLobbyPage);
                LobbyManager.Instance.CreateLobby(name: m_LobbyName.text, pass: m_Lobbypassword.text);
            });
        }
    }

}
