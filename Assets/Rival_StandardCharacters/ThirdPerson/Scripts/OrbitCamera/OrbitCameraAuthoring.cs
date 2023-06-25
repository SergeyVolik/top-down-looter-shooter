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

        //if (authoring.FollowedCharacter)
        //{
        //    authoring.OrbitCamera.FollowedCharacterEntity = GetEntity(authoring.FollowedCharacter, TransformUsageFlags.Dynamic);
        //}

        
        AddComponent(entity, authoring.OrbitCamera);
        AddComponent(entity, new OrbitCameraInputs());

        DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer = AddBuffer<OrbitCameraIgnoredEntityBufferElement>(entity);

        //if (authoring.OrbitCamera.FollowedCharacterEntity != Entity.Null)
        //{
        //    ignoredEntitiesBuffer.Add(new OrbitCameraIgnoredEntityBufferElement
        //    {
        //        Entity = authoring.OrbitCamera.FollowedCharacterEntity,
        //    });
        //}
        for (int i = 0; i < authoring.IgnoredEntities.Count; i++)
        {
            ignoredEntitiesBuffer.Add(new OrbitCameraIgnoredEntityBufferElement
            {
                Entity = GetEntity(authoring.IgnoredEntities[i], TransformUsageFlags.Dynamic),
            });
        }
    }
}