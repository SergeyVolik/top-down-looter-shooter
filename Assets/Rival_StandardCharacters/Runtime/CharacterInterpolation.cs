using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Rival
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct CharacterInterpolation : IComponentData
    {
        [GhostField]
        public RigidTransform PreviousTransform;
        [GhostField]
        public byte InterpolateTranslation;
        [GhostField]
        public byte InterpolateRotation;
        [GhostField]
        public byte InterpolationSkipping;

        public void SkipNextInterpolation()
        {
            InterpolationSkipping |= 1;
            InterpolationSkipping |= 2;
        }

        public void SkipNextTranslationInterpolation()
        {
            InterpolationSkipping |= 1;
        }

        public void SkipNextRotationInterpolation()
        {
            InterpolationSkipping |= 2;
        }

        public bool ShouldSkipNextTranslationInterpolation()
        {
            return (InterpolationSkipping & 1) == 1;
        }

        public bool ShouldSkipNextRotationInterpolation()
        {
            return (InterpolationSkipping & 2) == 2;
        }
    }
}
