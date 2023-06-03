using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    public class PausePage : BasePage
    {

        [SerializeField]
        private Button resume;

        [SerializeField]
        private Button mainmenu;

        

        protected override void Awake()
        {
            base.Awake();
            resume.onClick.AddListener(() =>
            {
                UINavigationManager.Instance.Pop();
                PauseManager.Instance.Resume();
            });

            mainmenu.onClick.AddListener(() =>
            {
              
            });
        }
    }

}
