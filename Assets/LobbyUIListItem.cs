using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIListItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_Text m_LobbyName;

    [SerializeField]
    private TMPro.TMP_Text m_UsersText;

   

    public event Action onSelected = delegate { };

    private void Awake()
    {

        var button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            onSelected.Invoke();
        });
    }
    public void Setup(string name, int currentUsers,  int maxUsers)
    {
        m_LobbyName.text = name;
        m_UsersText.text = $"{currentUsers}/{maxUsers}";
    }
}
