using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public class LevelTimerUI : MonoBehaviour
{
   

    private EntityManager _entityManager;
    private EntityQuery timerQ;

   
    private TMP_Text text;

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        timerQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(LevelTimerComponent) });
       
        text = GetComponent<TMPro.TMP_Text>();
        text.text = "";
    }
    private void Update()
    {
       

        string textStr = default;

        var timer = timerQ.ToComponentDataArray<LevelTimerComponent>(Allocator.Temp);

        if (timer.Length > 0)
        {
            var timedata = timer[0];

            textStr = $"Time: {(timedata.duration - timedata.currentTime).ToString("0.0")}";
            text.text = textStr;


            timer.Dispose();
        }
       
    }
}
