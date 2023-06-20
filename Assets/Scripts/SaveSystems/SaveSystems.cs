//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Entities.Graphics;
//using Unity.Physics;
//using Unity.Rendering;
//using UnityEngine;

//[Serializable]
//public struct SaveTag : IComponentData { }

//[Serializable]
//public struct MissingRenderMeshTag : IComponentData { }

//[Serializable]
//public struct MissingPhysicsColliderTag : IComponentData { }

//[Serializable]
//public struct MyBlobComponent : IComponentData
//{
//    public struct MyBlobData
//    {
//        public int Data;
//    }

//    public BlobAssetReference<MyBlobData> Value;
//}

//[Serializable]
//public struct MissingMyBlobComponentTag : IComponentData { }

//// Each prefab entity will have a unique ID stored in a PrefabID component. All entities instantiated from a given
//// prefab entity will have the same PrefabID component, but will be missing the Prefab tag component.
//[Serializable]
//public struct PrefabID : IComponentData
//{
//    public FixedString32Bytes ID;
//}

///*
// * Stores all prefabs that contain a PrefabID in a hash map keyed by ID that is accessible by other systems.
// */

////[UpdateAfter(typeof(ConvertToEntitySystem))]
//[UpdateAfter(typeof(BakingSystemGroup))]
//[UpdateInGroup(typeof(InitializationSystemGroup))]
//public partial class PrefabSystem : SystemBase
//{
//    public NativeHashMap<FixedString32Bytes, Entity> PrefabsByID;

//    private EntityCommandBufferSystem _commandBufferSystem;

//    private struct PrefabSystemState : ICleanupComponentData { }

//    protected override void OnCreate()
//    {
//        PrefabsByID = new NativeHashMap<FixedString32Bytes, Entity>(2, Allocator.Persistent);
//        _commandBufferSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
//    }

//    protected override void OnDestroy()
//    {
//        PrefabsByID.Dispose();
//    }

//    protected override void OnUpdate()
//    {
//        NativeHashMap<FixedString32Bytes, Entity> prefabs = PrefabsByID;

//        EntityCommandBuffer commandBuffer = _commandBufferSystem.CreateCommandBuffer();

//        // Add all new prefabs to the prefab hash map.
//        Entities
//            .WithAll<Prefab>()
//            .WithNone<PrefabSystemState>()
//            .ForEach(
//                (Entity entity, in PrefabID prefabID) =>
//                {
//                    prefabs.Add(prefabID.ID, entity);
//                    commandBuffer.AddComponent<PrefabSystemState>(entity);
//                }).Schedule();

//        _commandBufferSystem.AddJobHandleForProducer(Dependency);
//    }
//}

///*
// * Looks for any entities that contain the "Missing" component tags and replaces them with the actual components from
// * the associated prefab for that entity.
// */
//[UpdateInGroup(typeof(InitializationSystemGroup))]
//public partial class RestoreAfterLoadSystem : SystemBase
//{
//    private EntityCommandBufferSystem _commandBufferSystem;
//    private PrefabSystem _prefabSystem;

//    protected override void OnCreate()
//    {
//        _commandBufferSystem = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
//        _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
//    }

    
//    protected override void OnUpdate()
//    {
//        EntityCommandBuffer commandBuffer = _commandBufferSystem.CreateCommandBuffer();

//        NativeHashMap<FixedString32Bytes, Entity> prefabs = _prefabSystem.PrefabsByID;

//        Entities
//            .WithAll<MissingRenderMeshTag>()
//            .WithoutBurst()
//            .ForEach(
//                (Entity entity, in PrefabID prefabID) =>
//                {
//                    if (!prefabs.TryGetValue(prefabID.ID, out Entity prefab))
//                    {
//                        Debug.LogError($"Could not find prefab with id {prefabID.ID}");
//                        return;
//                    }
//                    var renderMesh = EntityManager.GetSharedComponentManaged<RenderMeshArray>(prefab);
//                    var matInfo = EntityManager.GetComponentData<MaterialMeshInfo>(prefab);
                   
//                    var filter = EntityManager.GetSharedComponentManaged<RenderFilterSettings>(prefab);




//                    var lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;


//                    if (EntityManager.HasComponent<CustomProbeTag>(prefab))
//                    {
//                        lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
//                    }

//                    else if (EntityManager.HasComponent<BlendProbeTag>(prefab))
//                    {
//                        lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;

//                    }


//                    var description = new RenderMeshDescription(
//                        receiveShadows: filter.ReceiveShadows,
//                        layer: filter.Layer,
//                        staticShadowCaster: filter.StaticShadowCaster,
//                        shadowCastingMode: filter.ShadowCastingMode,
//                        renderingLayerMask: filter.RenderingLayerMask,
//                        lightProbeUsage: lightProbeUsage,
//                        motionVectorGenerationMode: filter.MotionMode);


//                    RenderMeshUtility.AddComponents(entity, EntityManager, description, renderMesh, matInfo);
                   
//                    commandBuffer.RemoveComponent<MissingRenderMeshTag>(entity);
//                }
//            ).Run();

//        Entities
//            .WithAll<MissingPhysicsColliderTag>()
//            .ForEach(
//                (Entity entity, in PrefabID prefabID) =>
//                {
//                    if (!prefabs.TryGetValue(prefabID.ID, out Entity prefab))
//                    {
//                        Debug.LogError($"Could not find prefab with id {prefabID.ID}");
//                        return;
//                    }
//                    commandBuffer.AddComponent(entity, GetComponent<PhysicsCollider>(prefab));
//                    commandBuffer.RemoveComponent<MissingPhysicsColliderTag>(entity);
//                }
//            ).Schedule();

//        Entities
//            .WithAll<MissingMyBlobComponentTag>()
//            .ForEach(
//                (Entity entity, in PrefabID prefabID) =>
//                {
//                    if (!prefabs.TryGetValue(prefabID.ID, out Entity prefab))
//                    {
//                        Debug.LogError($"Could not find prefab with id {prefabID.ID}");
//                        return;
//                    }
//                    commandBuffer.AddComponent(entity, GetComponent<MyBlobComponent>(prefab));
//                    commandBuffer.RemoveComponent<MissingMyBlobComponentTag>(entity);
//                }
//            ).Schedule();

//        _commandBufferSystem.AddJobHandleForProducer(Dependency);
//    }
//}