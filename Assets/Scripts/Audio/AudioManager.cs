using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Text;
using QFSW.QC.Pooling;
using Sirenix.OdinInspector;
using Unity.Entities;
using System;

namespace SV
{


    // This component returns the particle system to the pool when the OnParticleSystemStopped event is received.
    [RequireComponent(typeof(AudioSource))]
    public class ReturnToPool : MonoBehaviour
    {
        public AudioSource system;
        public IObjectPool<AudioSource> pool;

        void Start()
        {
            system = GetComponent<AudioSource>();

        }

        private void Update()
        {
            if(!system.isPlaying)
                pool.Release(system);
        }

    }

    public struct PlaySFX : IComponentData
    {
        public Guid sfxSettingGuid;
    }
    public class SFXDatabaseComponent : IComponentData
    {
        public AudioSFXDatabase value;
    }

    public partial struct PlaySFXSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlaySFX>();
            state.RequireForUpdate<SFXDatabaseComponent>();

        }
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var sfxdatabase = SystemAPI.ManagedAPI.GetSingleton<SFXDatabaseComponent>().value;

            foreach (var (sfx, e) in SystemAPI.Query<PlaySFX>().WithEntityAccess())
            {
                AudioManager.Instance.PlaySFX(sfxdatabase.GetItem(sfx.sfxSettingGuid));
                ecb.DestroyEntity(e);
            }


        }
    }


    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private EntityManager em;
        public AudioSFXDatabase database;
        public AudioSFX initMusic;
        private AudioSource musicSource;
        private void Awake()
        {
            Instance = this;

            em = World.DefaultGameObjectInjectionWorld.EntityManager;

         
            entity = em.CreateSingleton<SFXDatabaseComponent>(new SFXDatabaseComponent
            {
                value = database

            }, new Unity.Collections.FixedString64Bytes("SFXDatabase"));

            PlayMusic(initMusic);
        }

        private void OnDestroy()
        {
            em.DestroyEntity(entity);
        }

        [HideInEditorMode]
        [Button]
        public void PlaySFX(AudioSFX audioSFX)
        {
            if (audioSFX == null)
                return;

            Pool.Get(out var source);

            audioSFX.Play(source);

           

        }

        [HideInEditorMode]
        [Button]
        public void PlayMusic(AudioSFX audioSFX)
        {
            if (audioSFX == null)
                return;

            if (musicSource == null)
            {
                Pool.Get(out musicSource);
                musicSource.loop = true;
            }

        

            audioSFX.Play(musicSource);



        }

        public enum PoolType
        {
            Stack,
            LinkedList
        }

        public PoolType poolType;

        // Collection checks will throw errors if we try to release an item that is already in the pool.
        public bool collectionChecks = true;
        public int maxPoolSize = 10;

        IObjectPool<AudioSource> m_Pool;
        private Entity entity;

        public IObjectPool<AudioSource> Pool
        {
            get
            {
                if (m_Pool == null)
                {
                    if (poolType == PoolType.Stack)
                        m_Pool = new ObjectPool<AudioSource>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, 10, maxPoolSize);
                    else
                        m_Pool = new LinkedPool<AudioSource>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, maxPoolSize);
                }
                return m_Pool;
            }
        }

        AudioSource CreatePooledItem()
        {
            var go = new GameObject("sfx");
            var ps = go.AddComponent<AudioSource>();

            var returnToPool = go.AddComponent<ReturnToPool>();
            returnToPool.pool = Pool;

            return ps;
        }

        // Called when an item is returned to the pool using Release
        void OnReturnedToPool(AudioSource system)
        {
            system.gameObject.SetActive(false);
        }

        // Called when an item is taken from the pool using Get
        void OnTakeFromPool(AudioSource system)
        {
            system.gameObject.SetActive(true);
        }

        // If the pool capacity is reached then any items returned will be destroyed.
        // We can control what the destroy behavior does, here we destroy the GameObject.
        void OnDestroyPoolObject(AudioSource system)
        {
            Destroy(system.gameObject);
        }



    }

}
