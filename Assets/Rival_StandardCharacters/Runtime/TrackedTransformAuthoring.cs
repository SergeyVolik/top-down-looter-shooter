using log4net.Util;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rival
{
    [DisallowMultipleComponent]
    public class TrackedTransformAuthoring : MonoBehaviour
    {
       
    }

    public class TrackedTransformAuthoringBaker : Baker<TrackedTransformAuthoring>
    {
        public override void Bake(TrackedTransformAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            RigidTransform currentTransform = new RigidTransform(authoring.transform.rotation, authoring.transform.position);
            TrackedTransform trackedTransform = new TrackedTransform
            {
                CurrentFixedRateTransform = currentTransform,
                PreviousFixedRateTransform = currentTransform,
            };
            AddComponent(e, trackedTransform);
        }
    }
}
