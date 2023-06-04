using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SV.UI
{
    public class PausePage : BasePage
    {


        protected override void Awake()
        {
            base.Awake();

        }

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
    }

}
