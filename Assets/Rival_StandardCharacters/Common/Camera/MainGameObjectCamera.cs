

using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class MainGameObjectCamera : MonoBehaviour
{
    private void Update()
    {
        foreach (World world in World.All)
        {
            if (world.IsClient())
            {
                MainCameraSystem mainCameraSystem = world.GetExistingSystemManaged<MainCameraSystem>();
                if (mainCameraSystem != null && mainCameraSystem.Enabled)
                {
                    mainCameraSystem.CameraGameObjectTransform = this.transform;
                    Destroy(this);
                }
            }
        }
    }
}