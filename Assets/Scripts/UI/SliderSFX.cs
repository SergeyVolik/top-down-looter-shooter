using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SV.UI
{
    public class SliderSFX : MonoBehaviour, ISelectHandler
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
            var slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener((v) => {
                AudioManager.Instance.PlaySFX(sfx);
            });

           
        }
    }

}
