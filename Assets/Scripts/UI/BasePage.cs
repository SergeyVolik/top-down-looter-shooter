using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SV.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class BasePage : MonoBehaviour, INavigateable
    {
        private Canvas m_Canvas;
        private GraphicRaycaster m_GraphicRaycaster;

        [SerializeField]
        private bool initPage;

        [SerializeField]
        private GameObject lastFocusedElement;
        bool firstHideExecuted = false;
        protected virtual void Awake()
        {
            m_Canvas = GetComponent<Canvas>();
            m_GraphicRaycaster = GetComponent<GraphicRaycaster>();


            Hide();


        }

        protected virtual void OnDestroy() { }
        protected virtual void Start()
        {
            if (initPage)
                UINavigationManager.Instance.Navigate(this);

        }
       
        public virtual void Hide(bool onlyDosableInput = false)
        {
           
            if(EventSystem.current && firstHideExecuted && EventSystem.current.currentSelectedGameObject != null)
                lastFocusedElement = EventSystem.current.currentSelectedGameObject;

            firstHideExecuted = true;

            if (onlyDosableInput)
            {
                m_GraphicRaycaster.enabled = false;
            }
            else
            {
                if (m_Canvas)
                    m_Canvas.enabled = false;
                if (m_GraphicRaycaster)
                    m_GraphicRaycaster.enabled = false;
            }
        }

        public virtual void Show()
        {
            if(EventSystem.current)
                EventSystem.current.SetSelectedGameObject(lastFocusedElement);

            if (m_Canvas)
                m_Canvas.enabled = true;
            if (m_GraphicRaycaster)
                m_GraphicRaycaster.enabled = true;

        }
    }

}
