using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public class OrbitCameraAuthoring : MonoBehaviour
{
    public GameObject FollowedCharacter;
    public List<GameObject> IgnoredEntities = new List<GameObject>();
    public OrbitCamera OrbitCamera = OrbitCamera.GetDefault();

    
}

public class OrbitCameraAuthoringBaker : Baker<OrbitCameraAuthoring>
{
    public override void Bake(OrbitCameraAuthoring authoring)
    {

        var entity = GetEntity(TransformUsageFlags.Dynamic);

        authoring.OrbitCamera.CurrentDistanceFromMovement = authoring.OrbitCamera.TargetDistance;
        authoring.OrbitCamera.CurrentDistanceFromObstruction = authoring.OrbitCamera.TargetDistance;
        authoring.OrbitCamera.PlanarForward = -math.forward();


        
        AddComponent(entity, authoring.OrbitCamera);
        AddComponent(entity, new OrbitCameraInputs());

    
    }
}