using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Text;
using QFSW.QC.Pooling;
using Sirenix.OdinInspector;
using Unity.Entities;
using System;
using UnityEngine.Audio;
using UnityEngine.InputSystem.LowLevel;
using Unity.VisualScripting;
using DG.Tweening;
using Unity.Mathematics;

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
            if (!system.isPlaying)
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

        private const string SFXVolumeParam = "SFXVolume";
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";

        private EntityManager em;
        public AudioSFXDatabase database;
        public AudioSFX initMusic;
        private AudioSource musicSource;

        public AudioMixer mixer;
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private float musicVolume = 1f;
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

        private void Start()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }
        private void OnDestroy()
        {
            em.DestroyEntity(entity);


        }

        public float GetSFXGlobalVolume()
        {
         
            return sfxVolume;
        }
        public float GetMasterGlobalVolume()
        {

            return masterVolume;
        }
        public float GetMusicGlobalVolume()
        {
           
            return musicVolume;
        }


        public void SetMasterGlobalVolume(float volume)
        {

            masterVolume = Mathf.Clamp(volume, 0, 1);
            AudioListener.volume = masterVolume;
        }
        public void SetSFXGlobalVolume(float volume)
        {
            var clamped = Mathf.Clamp(volume, 0, 1);
            var value = (volume == 0 ? -80 : MathF.Log10(clamped) * 20);
            sfxVolume = clamped;
            mixer.SetFloat(SFXVolumeParam, value);

        }
        public void SetMusicGlobalVolume(float volume)
        {
            var clamped = Mathf.Clamp(volume, 0, 1);
            var value = (volume == 0 ? -80 : MathF.Log10(clamped) * 20);
            musicVolume = clamped;
           
            mixer.SetFloat(MusicVolumeParam, value);

        }

      

        public void SaveSettings()
        {
            Debug.Log($"save master v={GetMasterGlobalVolume()}");
            PlayerPrefs.SetFloat(MasterVolumeParam, GetMasterGlobalVolume());
            PlayerPrefs.SetFloat(SFXVolumeParam, GetSFXGlobalVolume());
            PlayerPrefs.SetFloat(MusicVolumeParam, GetMusicGlobalVolume());

        }
        private void LoadSettings()
        {
          
            SetMasterGlobalVolume(PlayerPrefs.GetFloat(MasterVolumeParam, 1f));
            SetSFXGlobalVolume(PlayerPrefs.GetFloat(SFXVolumeParam, 1f));
            SetMusicGlobalVolume(PlayerPrefs.GetFloat(MusicVolumeParam, 1f));

            Debug.Log($"load master v={GetMasterGlobalVolume()} pref={PlayerPrefs.GetFloat(MasterVolumeParam, 1f)}");

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
