using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerData : MonoBehaviour
{
    public static LocalPlayer Player { get; private set; } = new LocalPlayer { 
          DisplayName = new CallbackValue<string>("DefaultName")
    };

    [Serializable]
    public class LocalPlayer
    {

        public CallbackValue<string> DisplayName = new CallbackValue<string>("");

        public const string key_DisplayName = nameof(DisplayName);


        public LocalPlayer()
        {

        }
    }

    private void Awake()
    {

        Player = new LocalPlayer();
        SetupLocalPlayer();
    }


    private void SetupLocalPlayer()
    {
        if (PlayerPrefs.HasKey(nameof(Player.DisplayName)))
            Player.DisplayName.Value = PlayerPrefs.GetString(nameof(Player.DisplayName));
        else
        {
            Player.DisplayName.Value = $"Player{UnityEngine.Random.Range(0, 100)}";
        }
    }
    private void OnDestroy()
    {
        PlayerPrefs.SetString(nameof(Player.DisplayName), Player.DisplayName.Value);
    }
}
