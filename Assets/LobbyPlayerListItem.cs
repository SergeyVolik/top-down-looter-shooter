using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerListItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI m_name;
    [SerializeField]
    private GameObject hostToggle;
    [SerializeField]
    private Button m_KickButton;

   
    public void Setup(string name, bool hostItem, string playerId, bool hostCreated, bool ready)
    {
        var readyStr = ready ? "(Ready)" : "(Not Ready)";
        m_name.text = $"{name} {readyStr}";

        hostToggle.gameObject.SetActive(hostItem);
        m_KickButton.gameObject.SetActive(false);
        if (hostCreated && !hostItem)
        {
            m_KickButton.gameObject.SetActive(true);
            m_KickButton.onClick.AddListener(async () =>
            {
                await LobbyManager.Instance.KickPlayer(playerId);
            });
        }
        
    }
}
