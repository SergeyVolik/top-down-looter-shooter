
//using System;
//using System.IO;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Entities;
//using Unity.Entities.Serialization;
//using Unity.Physics;
//using Unity.Rendering;
//using UnityEngine;

//public class SaveManager : MonoBehaviour
//{
//    private EntityQuery _entitiesToSave;

//    private void Start()
//    {
//        // Cache a query that gathers all of the entities that should be saved.
//        // NOTE: You don't have to use a special tag component for all entities you want to save. You could instead just
//        // save, for example, anything with a Translation component which would exclude things like Singletons entities.
//        // It is important to note that prefabs (anything with a Prefab tag component) are automatically excluded from
//        // an EntityQuery unless EntityQueryOptions.IncludePrefab is set.
//        var savableEntities = new EntityQueryDesc
//        {
//            Any = new ComponentType[]
//            {
//                typeof(SaveTag),
//            },
//            Options = EntityQueryOptions.Default
//        };
//        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//        _entitiesToSave = entityManager.CreateEntityQuery(savableEntities);
//    }

//    // Looks for and removes a set of components and then adds a different set of components to the same set
//    // of entities. 
//    private void ReplaceComponents(
//        ComponentType[] typesToRemove,
//        ComponentType[] typesToAdd,
//        EntityManager entityManager)
//    {
//        EntityQuery query = entityManager.CreateEntityQuery(
//            new EntityQueryDesc { Any = typesToRemove, Options = EntityQueryOptions.Default }
//        );
//        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

//        foreach (ComponentType removeType in typesToRemove)
//        {
//            entityManager.RemoveComponent(entities, removeType);
//        }
//        foreach (ComponentType addType in typesToAdd)
//        {
//            entityManager.AddComponent(entities, addType);
//        }
//    }

//    public void Save(string filepath)
//    {
//        /*
//         * 1. Create a new world.
//         * 2. Copy over the entities we want to serialize to the new world.
//         * 3. Remove all shared components, components containing blob asset references, and components containing
//         *    external entity references.
//         * 4. Serialize the new world to a save file.
//         */

//        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//        using (var serializeWorld = new World("Serialization World"))
//        {
//            EntityManager serializeEntityManager = serializeWorld.EntityManager;
//            serializeEntityManager.CopyEntitiesFrom(entityManager, _entitiesToSave.ToEntityArray(Allocator.Temp));

//            // Remove RenderMesh and related components
//            ReplaceComponents(
//                new ComponentType[]
//                {
//                    typeof(RenderMesh),
//                    //typeof(EditorRenderData),
//                    typeof(WorldRenderBounds),
//                    typeof(ChunkWorldRenderBounds),
//                    //typeof(HybridChunkInfo),
//                    typeof(RenderBounds)
//                },
//                new ComponentType[] { typeof(MissingRenderMeshTag) },
//                serializeEntityManager
//            );

//            // Remove physics colliders
//            ReplaceComponents(
//                new ComponentType[]
//                {
//                    typeof(PhysicsCollider),
//                },
//                new ComponentType[] { typeof(MissingPhysicsColliderTag) },
//                serializeEntityManager
//            );

//            // Remove blob assets.
//            ReplaceComponents(
//                new ComponentType[]
//                {
//                    typeof(MyBlobComponent),
//                },
//                new ComponentType[] { typeof(MissingMyBlobComponentTag) },
//                serializeEntityManager
//            );

//            // Need to remove the SceneTag shared component from all entities because it contains an entity reference
//            // that exists outside the subscene which isn't allowed for SerializeUtility. This breaks the link from the
//            // entity to the subscene, but otherwise doesn't seem to cause any problems.
//            serializeEntityManager.RemoveComponent<SceneTag>(serializeEntityManager.UniversalQuery);
//            serializeEntityManager.RemoveComponent<SceneSection>(serializeEntityManager.UniversalQuery);



//            // Save
//            using (var writer = new StreamBinaryWriter1(filepath))
//            {
//                SerializeUtility.SerializeWorld(serializeEntityManager, writer);
//            }
//        }
//    }

//    internal unsafe class StreamBinaryWriter1 : Unity.Entities.Serialization.BinaryWriter
//    {
//        private Stream stream;
//        private byte[] buffer;
//        public long Position
//        {
//            get => stream.Position;
//            set => stream.Position = value;
//        }

//        public StreamBinaryWriter1(string fileName, int bufferSize = 65536)
//        {
//            stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
//            buffer = new byte[bufferSize];
//        }

//        public void Dispose()
//        {
//            stream.Dispose();
//        }

//        public void WriteBytes(void* data, int bytes)
//        {
//            int remaining = bytes;
//            int bufferSize = buffer.Length;

//            fixed (byte* fixedBuffer = buffer)
//            {
//                while (remaining != 0)
//                {
//                    int bytesToWrite = System.Math.Min(remaining, bufferSize);
//                    UnsafeUtility.MemCpy(fixedBuffer, data, bytesToWrite);
//                    stream.Write(buffer, 0, bytesToWrite);
//                    data = (byte*)data + bytesToWrite;
//                    remaining -= bytesToWrite;
//                }
//            }
//        }

//        public long Length => stream.Length;
//    }
//    public void Load(string filepath)
//    {
//        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

//        entityManager.DestroyEntity(_entitiesToSave);

//        using (var deserializeWorld = new World("Deserialization World"))
//        {
//            ExclusiveEntityTransaction transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();

//            using (var reader = new StreamBinaryReader(filepath))
//            {
//                SerializeUtility.DeserializeWorld(transaction, reader);
//            }

//            deserializeWorld.EntityManager.EndExclusiveEntityTransaction();

//            entityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
//        }
//    }


//    internal unsafe class StreamBinaryReader : Unity.Entities.Serialization.BinaryReader
//    {
//        internal string FilePath { get; }
//#if UNITY_EDITOR
//        private Stream stream;
//        private byte[] buffer;
//        public long Position
//        {
//            get => stream.Position;
//            set => stream.Position = value;
//        }
//#else
//        public long Position { get; set; }
//#endif

//        public StreamBinaryReader(string filePath, long bufferSize = 65536)
//        {
//            if (string.IsNullOrEmpty(filePath))
//                throw new ArgumentException("The filepath can neither be null nor empty", nameof(filePath));

//            FilePath = filePath;
//#if UNITY_EDITOR
//            stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
//            buffer = new byte[bufferSize];
//#else
//            Position = 0;
//#endif
//        }

//        public void Dispose()
//        {
//#if UNITY_EDITOR
//            stream.Dispose();
//#endif
//        }

//        public void ReadBytes(void* data, int bytes)
//        {
//#if UNITY_EDITOR
//            int remaining = bytes;
//            int bufferSize = buffer.Length;

//            fixed (byte* fixedBuffer = buffer)
//            {
//                while (remaining != 0)
//                {
//                    int read = stream.Read(buffer, 0, System.Math.Min(remaining, bufferSize));
//                    remaining -= read;
//                    UnsafeUtility.MemCpy(data, fixedBuffer, read);
//                    data = (byte*)data + read;
//                }
//            }
//#else
//            var readCmd = new ReadCommand
//            {
//                Size = bytes, Offset = Position, Buffer = data
//            };
//            Assert.IsFalse(string.IsNullOrEmpty(FilePath));
//#if ENABLE_PROFILER
//            // When AsyncReadManagerMetrics are available, mark up the file read for more informative IO metrics.
//            // Metrics can be retrieved by AsyncReadManagerMetrics.GetMetrics
//            var readHandle = AsyncReadManager.Read(FilePath, &readCmd, 1, subsystem: AssetLoadingSubsystem.EntitiesStreamBinaryReader);
//#else
//            var readHandle = AsyncReadManager.Read(FilePath, &readCmd, 1);
//#endif
//            readHandle.JobHandle.Complete();

//            if (readHandle.Status != ReadStatus.Complete)
//            {
//                throw new IOException($"Failed to read from {FilePath}!");
//            }
//            Position += bytes;
//#endif
//        }
//    }
//}

