using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using UnityEngine;


public class LobbyManager : MonoBehaviour
{
    [Range(1, 12)]
    public int maxLobbies = 1;
    public static LobbyManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }



    public async void QueryLobbies()
    {
        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
        {
            Count = maxLobbies,
            
        };

        var data = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

        
    }

    public void CreateLobby(string name, string pass)
    {
       
    }

    public void RefreshLobbies()
    {
      
    }
}

public class CallbackValue<T>
{
    public Action<T> onChanged;


    public CallbackValue()
    {

    }
    public CallbackValue(T cachedValue)
    {
        m_CachedValue = cachedValue;
    }

    public T Value
    {
        get => m_CachedValue;
        set
        {
            if (m_CachedValue != null && m_CachedValue.Equals(value))
                return;
            m_CachedValue = value;
            onChanged?.Invoke(m_CachedValue);
        }
    }

    public void ForceSet(T value)
    {
        m_CachedValue = value;
        onChanged?.Invoke(m_CachedValue);
    }

    public void SetNoCallback(T value)
    {
        m_CachedValue = value;
    }

    T m_CachedValue = default;
}