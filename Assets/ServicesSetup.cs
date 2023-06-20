using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class ServicesSetup : MonoBehaviour
{
    private async void Awake()
    {
        string serviceProfileName = "player";

        await InitUnityServices(serviceProfileName);

        Debug.Log("[ServicesSetup] UnityServices Inited");


        await SignIn();


    }

    private async Task SignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"[ServicesSetup] AuthenticationService Inited: {AuthenticationService.Instance.IsSignedIn}");
    }

    private async Task<bool> InitUnityServices(string profileName = null)
    {


        if (UnityServices.State == ServicesInitializationState.Initialized)
            return true;

        if (UnityServices.State == ServicesInitializationState.Initializing)
            return false;



        var profile = new InitializationOptions();

        if(!string.IsNullOrEmpty(profileName))
            profile.SetProfile(profileName);

        await UnityServices.InitializeAsync(profile);


        return true;


    }

 
}
