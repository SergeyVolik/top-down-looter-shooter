using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SV.ECS
{
    public class VisualMessage : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TMP_Text m_TextMeshPro;

        [Button]
        public void Show(string text, Color color)
        {
            m_TextMeshPro.color = color;
            m_TextMeshPro.text = text;
        }
    }

}
