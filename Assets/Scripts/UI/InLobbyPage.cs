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

        protected override void Awake()
        {
            base.Awake();


        }

        public override void Show()
        {
            base.Show();

            LobbyManager.Instance.RefreshLobbies();
        }
    }

}
