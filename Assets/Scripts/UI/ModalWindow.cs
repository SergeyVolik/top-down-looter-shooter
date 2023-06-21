using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SV.UI
{
    public class ModalWindow : BasePage
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI m_Title;
        [SerializeField]
        private TMPro.TextMeshProUGUI m_Message;
        [SerializeField]
        private Button m_OkButton;
        public static ModalWindow Instance { get; private set; }

        protected override void Awake()
        {
            Instance = this;
        }

        public void Show(string title, string message, Action okCallback)
        {
            m_Title.text = title;
            m_Message.text = message;

            UnityAction callback = null;

            Show();
            callback = new UnityAction(() =>
            {
                okCallback?.Invoke();
                m_OkButton.onClick.RemoveListener(callback);
                Hide();
            });

            m_OkButton.onClick.AddListener(callback);

        }
    }

}
