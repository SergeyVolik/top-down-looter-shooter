using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPlayerListItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI m_name;
    [SerializeField]
    private GameObject hostToggle;
    public void Setup(string name, bool host)
    {
        m_name.text = name;

        hostToggle.gameObject.SetActive(host);
    }
}
