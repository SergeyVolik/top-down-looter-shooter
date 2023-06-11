using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace SV.UI
{
    public class MainMenuSettingsPage : BasePage
    {
        public Slider musicSlider;
        public Slider sfxSider;
        public Slider masterSider;


   
        protected override void Awake()
        {
            base.Awake();

         
          
            

           
        }

        private bool anyValueHasChanged;
        public override void Hide(bool onlyDosableInput = false)
        {
            base.Hide(onlyDosableInput);

            if (anyValueHasChanged)
            {
                AudioManager.Instance.SaveSettings();
                anyValueHasChanged = false;
            }
        }
        protected override void Start()
        {
            base.Start();

            musicSlider.value = AudioManager.Instance.GetMusicGlobalVolume();
            sfxSider.value = AudioManager.Instance.GetSFXGlobalVolume();
            masterSider.value = AudioManager.Instance.GetMasterGlobalVolume();

            musicSlider.onValueChanged.AddListener((v) =>
            {
                anyValueHasChanged = true;
                AudioManager.Instance.SetMusicGlobalVolume(v);
            });

            sfxSider.onValueChanged.AddListener((v) =>
            {
                anyValueHasChanged = true;

                AudioManager.Instance.SetSFXGlobalVolume(v);

            });

            masterSider.onValueChanged.AddListener((v) =>
            {
                anyValueHasChanged = true;

                AudioManager.Instance.SetMasterGlobalVolume(v);

            });
        }

    }

}
