
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class MainGameObjectCamera : MonoBehaviour
{
  
    private void Awake()
    {
        
    }
    private void Update()
    {
        foreach (World world in World.All)
        {
            if (world.IsClient())
            {
              
                
                var entity = world.EntityManager.CreateEntity();

                world.EntityManager.AddComponentObject(entity, Camera.main);
              
                Destroy(this);


                
            }
        }
    }
}