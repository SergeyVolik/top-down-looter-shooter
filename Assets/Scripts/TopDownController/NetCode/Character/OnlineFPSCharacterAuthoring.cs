using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;

namespace Rival.Samples.OnlineFPS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsShapeAuthoring))]
    public class OnlineFPSCharacterAuthoring : MonoBehaviour
    {
        public GameObject View;
        public GameObject MeshRoot;
        public GameObject WeaponSocket;

        public OnlineFPSCharacterComponent OnlineFPSCharacter;
        public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();

        public class Baker : Baker<OnlineFPSCharacterAuthoring>
        {
            public override void Bake(OnlineFPSCharacterAuthoring authoring)
            {
                Entity entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                KinematicCharacterUtilities.HandleConversionForCharacter(this, entity, authoring.gameObject, authoring.CharacterBody);

                authoring.OnlineFPSCharacter.ViewEntity = GetEntity(authoring.View, TransformUsageFlags.Dynamic);
                authoring.OnlineFPSCharacter.MeshRootEntity = GetEntity(authoring.MeshRoot,TransformUsageFlags.Dynamic);
                authoring.OnlineFPSCharacter.WeaponSocketEntity = GetEntity(authoring.WeaponSocket, TransformUsageFlags.Dynamic);

                AddComponent(entity, authoring.OnlineFPSCharacter);
                AddComponent(entity, new OnlineFPSCharacterInputs());
                AddComponent(entity, new ActiveWeapon());
            }
        }

    }

   
}
