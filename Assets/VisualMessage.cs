using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

    public class ShowVisualMessageComponent : IComponentData, IEnableableComponent
    {
        public Color color;
        public string text;
        public float3 pos;
    }
    public class VisualMessageGO : IComponentData
    {
        public VisualMessage value;
    }


}
