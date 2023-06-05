using UnityEngine;
using UnityEngine.Windows;

namespace SV.UI
{
    public class GameplayRootUIPage : BasePage
    {
        private PlayerControlls m_Input;

        [SerializeField]
        private BasePage optionsPage;
        protected override void Awake()
        {

            m_Input = new PlayerControlls();
           
            m_Input.Enable();
            base.Awake();
           
        }

        public override void Show()
        {
            base.Show();

            m_Input.Controlls.Pause.performed += Pause_performed;
        }

        private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            UINavigationManager.Instance.Navigate(optionsPage);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_Input.Controlls.Pause.performed -= Pause_performed;
        }
        public override void Hide(bool onlyDosableInput = false)
        {
            base.Hide(onlyDosableInput);
            if (m_Input != null)
                m_Input.Controlls.Pause.performed -= Pause_performed;
        }
    }

}
