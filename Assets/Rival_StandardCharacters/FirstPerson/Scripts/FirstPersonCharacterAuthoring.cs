using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Rival;
using Unity.Physics;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class FirstPersonCharacterAuthoring : MonoBehaviour
{
    public Transform CharacterViewTransform;
    public AuthoringKinematicCharacterBody CharacterBody = AuthoringKinematicCharacterBody.GetDefault();
    public FirstPersonCharacterComponent FirstPersonCharacter = FirstPersonCharacterComponent.GetDefault();

    private void Start()
    {
        
    }

}

public class FirstPersonCharacterAuthoringBaker : Baker<FirstPersonCharacterAuthoring>
{
    public override void Bake(FirstPersonCharacterAuthoring authoring)
    {
        if (!authoring.enabled)
            return;

        if (authoring.CharacterViewTransform == null)
        {
            UnityEngine.Debug.LogError("ERROR: the CharacterViewTransform must not be null. You must assign a 1st-level child object of the character to this field (the object that represents the camera point). Conversion will be aborted");
            return;
        }
        if (authoring.CharacterViewTransform.parent != authoring.transform)
        {
            UnityEngine.Debug.LogError("ERROR: the CharacterViewTransform must be a direct 1st-level child of the character authoring GameObject. Conversion will be aborted");
            return;
        }

        var CharacterBody = authoring.CharacterBody;
        var FirstPersonCharacter = authoring.FirstPersonCharacter;

        var entity = GetEntity(TransformUsageFlags.Dynamic);

        KinematicCharacterUtilities.HandleConversionForCharacter(this, entity, authoring.gameObject, CharacterBody);

     

        FirstPersonCharacter.CharacterViewEntity = GetEntity(authoring.CharacterViewTransform, TransformUsageFlags.Dynamic);

        AddComponent(entity, FirstPersonCharacter);
        AddComponent(entity, new FirstPersonCharacterInputs());
    }
}
