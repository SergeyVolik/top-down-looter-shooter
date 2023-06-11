using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    [RequireComponent(typeof(Button))]
    public class NavigatePage : MonoBehaviour
    {

        public BasePage page;
        public bool addtive;
        public bool clearNavManager;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (clearNavManager)
                {
                    UINavigationManager.Instance.PopAll();
                }

                UINavigationManager.Instance.Navigate(page, addtive);
            });
        }
    }

}
