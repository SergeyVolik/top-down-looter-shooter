using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class PhysicsCastMB : MonoBehaviour
    {
        
    }

    public struct PhysicsCastComponent : IComponentData
    {

    }

    public class PhysicsCastComponentBaker : Baker<PhysicsCastMB>
    {
        public override void Bake(PhysicsCastMB authoring)
        {
            AddComponent<PhysicsCastComponent>();
        }
    }

}
