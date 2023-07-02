using Rival;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode.Hybrid;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(GhostPresentationGameObjectEntityOwner))]
public class CharacterAnimationSync : MonoBehaviour
{
    private GhostPresentationGameObjectEntityOwner m_EntityOwner;
    private Animator m_Animator;

    private static readonly int moveParam = Animator.StringToHash("Move");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private void Start()
    {
        m_EntityOwner = GetComponent<GhostPresentationGameObjectEntityOwner>();
        m_Animator = GetComponent<Animator>();
        m_Animator.fireEvents = false;

    }

    private void Update()
    {
        if (!HasComponentData<KinematicCharacterBody>())
        {
            Debug.LogError("KinematicCharacterBody not exist");
            return;
        }

        var controller = GetEntityComponentData<KinematicCharacterBody>();


        m_Animator.SetFloat(moveParam, controller.sprint ? math.length(controller.MoveVector * 2) : math.clamp(math.length(controller.MoveVector), 0, 1));
        m_Animator.SetBool(IsGrounded, controller.IsGrounded);

    }

    void SetEntityComponentData<T>(T data) where T : unmanaged, IComponentData
    {
        m_EntityOwner.World.EntityManager.SetComponentData(m_EntityOwner.Entity, data);
    }
    bool HasComponentData<T>() where T : unmanaged, IComponentData
    {
        return m_EntityOwner.World.EntityManager.HasComponent<T>(m_EntityOwner.Entity);
    }
    T GetEntityComponentData<T>() where T : unmanaged, IComponentData
    {
        return m_EntityOwner.World.EntityManager.GetComponentData<T>(m_EntityOwner.Entity);
    }
}
