using SV.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class LevelUpPage : BasePage, INavigateable
    {
       
        public override void Show()
        {
            base.Show();
            PauseManager.Instance.Pause();
        }

        public override void Hide(bool onlyDosableInput = false)
        {
            base.Hide(onlyDosableInput);
            PauseManager.Instance.Resume();


        }

        protected override void Awake()
        {
            base.Awake();

         

            LevelUpSystemSystem.OnLevelUp += LevelUpSystemSystem_OnLevelUp;
        }

        private void OnDestroy()
        {
            LevelUpSystemSystem.OnLevelUp -= LevelUpSystemSystem_OnLevelUp;
        }

        private void LevelUpSystemSystem_OnLevelUp()
        {
            UINavigationManager.Instance.Navigate(this, true);
        }


    }

}
