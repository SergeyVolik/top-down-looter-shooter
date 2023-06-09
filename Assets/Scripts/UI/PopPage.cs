using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SV.UI
{
    [RequireComponent(typeof(Button))]
    public class PopPage : MonoBehaviour
    {

       

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                UINavigationManager.Instance.Pop();
            });
        }
    }

}
