using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class ThirdPersonCharacterAuthoring : MonoBehaviour
{
    public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    public ThirdPersonCharacterComponent ThirdPersonCharacter = ThirdPersonCharacterComponent.GetDefault();

}

public class ThirdPersonCharacterAuthoringBaker : Baker<ThirdPersonCharacterAuthoring>
{

    public override void Bake(ThirdPersonCharacterAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        KinematicCharacterUtilities.HandleConversionForCharacter(this, entity, authoring.gameObject, authoring.CharacterBody);

        AddComponent(entity, authoring.ThirdPersonCharacter);
        AddComponent(entity, new ThirdPersonCharacterInputs());

    }
}
