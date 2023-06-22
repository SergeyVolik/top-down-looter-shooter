using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class RelayHUD : MonoBehaviour
{
    public TMPro.TextMeshProUGUI JoinCodeLabel;

    public void Awake()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var joinQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<JoinCode>());
        if (joinQuery.HasSingleton<JoinCode>())
        {
            var joinCode = joinQuery.GetSingleton<JoinCode>().Value;
            JoinCodeLabel.text = $"Join code: {joinCode}";
            enabled = false;
        }
        else
        {
            Debug.LogError("JoinCode not exist");
        }
    }

    private void Update()
    {
        
    }
}