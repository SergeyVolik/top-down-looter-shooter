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
        private IPageHidedListener[] m_Hideale;
        private IPageShowedListener[] m_Showed;
        [SerializeField]
        private bool initPage;

        [SerializeField]
        private GameObject lastFocusedElement;
        bool firstHideExecuted = false;



        protected virtual void Awake()
        {
            m_Canvas = GetComponent<Canvas>();
            m_GraphicRaycaster = GetComponent<GraphicRaycaster>();

            m_Hideale = GetComponentsInChildren<IPageHidedListener>();
            m_Showed =  GetComponentsInChildren<IPageShowedListener>();

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

            if (m_Hideale != null)
            {
                foreach (var item in m_Hideale)
                {
                    item.OnHided();
                }
            }
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

            if (m_Showed != null)
            {
                foreach (var item in m_Showed)
                {
                    item.OnShowed();
                }
            }

            if (m_Canvas)
                m_Canvas.enabled = true;
            if (m_GraphicRaycaster)
                m_GraphicRaycaster.enabled = true;

            transform.SetAsLastSibling();
           
        }

        
    }

}
