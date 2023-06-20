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
        private TextMesh m_TextMeshPro;


        public class Baker : Baker<VisualMessage>
        {
            public override void Bake(VisualMessage authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponentObject(entity, new VisualMessageGO {  
                      text = authoring.m_TextMeshPro
                });

                AddComponentObject(entity, new ShowVisualMessageComponent { });
              
            }
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
        public TextMesh text;
    }


}
