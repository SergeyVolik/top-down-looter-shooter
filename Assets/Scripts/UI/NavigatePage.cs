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
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                UINavigationManager.Instance.Navigate(page, addtive);
            });
        }
    }

}
