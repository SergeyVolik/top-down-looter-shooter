using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(MessageWindowGroup))]
public partial class MessageWindowInputSystem : SystemBase
{
    private MessageWindow m_Window;
    public void Setup(MessageWindow window)
    {
        m_Window = window;

    }

    protected override void OnCreate()
    {
        base.OnCreate();

    }
    protected override void OnUpdate()
    {
        if (m_Window == null)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            m_Window.Activate(!m_Window.enabled);
            m_Window.InputField.text = "";
            m_Window.InputField.Select();
            m_Window.InputField.ActivateInputField();
        }


        if (!m_Window.enabled)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {

            SendRPC(m_Window.InputField.text);
            m_Window.InputField.text = "";

            m_Window.InputField.Select();
            m_Window.InputField.ActivateInputField();

        }


    }

    public void SendRPC(FixedString128Bytes message, Entity targetEntity = default)
    {

        if (!m_Window.isActiveAndEnabled)
            return;
        if (message.IsEmpty)
            return;

        var entity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(entity, new ChatMessageRpc() { Message = message });
        EntityManager.AddComponent<SendRpcCommandRequest>(entity);
        if (targetEntity != Entity.Null)
            EntityManager.SetComponentData(entity,
                new SendRpcCommandRequest() { TargetConnection = targetEntity });
    }
}