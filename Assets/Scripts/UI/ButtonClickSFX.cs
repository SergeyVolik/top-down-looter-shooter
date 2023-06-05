using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SV.UI
{
    public class ButtonClickSFX : MonoBehaviour, ISelectHandler
    {
        public AudioSFX sfx;

        public void OnSelect(BaseEventData eventData)
        {
            if (AudioManager.Instance)
            {
                AudioManager.Instance.PlaySFX(sfx);
            }
        }

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(() => {
                AudioManager.Instance.PlaySFX(sfx);
            });

           
        }
    }

}
