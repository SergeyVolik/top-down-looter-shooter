using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{
    public class CharacterControllerMB : MonoBehaviour
    {
        public float speed;

    }
    public struct CharacterControllerComponent : IComponentData
    {
        public float speed;
    }

    public struct CharacterMoveInputComponent : IEnableableComponent, IComponentData
    {
        public Vector2 value;
    }

    public class CharacterControllerComponentBaker : Baker<CharacterControllerMB>
    {
        public override void Bake(CharacterControllerMB authoring)
        {
            AddComponent(new CharacterControllerComponent
            {
                speed = authoring.speed
            });

            AddComponent(new CharacterMoveInputComponent());
        }
    }

}
