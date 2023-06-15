using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class TopDownCharacterAuthoring : MonoBehaviour
{
    public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    public TopDownCharacterComponent ThirdPersonCharacter = TopDownCharacterComponent.GetDefault();

}

public class TopDownCharacterAuthoringBaker : Baker<TopDownCharacterAuthoring>
{

    public override void Bake(TopDownCharacterAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        KinematicCharacterUtilities.HandleConversionForCharacter(this, entity, authoring.gameObject, authoring.CharacterBody);

        AddComponent(entity, authoring.ThirdPersonCharacter);
        AddComponent(entity, new TopDownCharacterInputs());
    }
}
