using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct ClearMWComponent : IComponentData
{

}



public struct UserName : IComponentData
{
    public FixedString128Bytes Name;
}

public struct ChatUserInitialized : ICleanupComponentData
{
    public Unity.Entities.Hash128 userGuid;
    public FixedString128Bytes Name;
}


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(MessageWindowGroup))]
public partial class ShowMessageWindowExecutorSystem : SystemBase
{
    private MessageWindow m_Window;

    protected override void OnCreate()
    {
        base.OnCreate();


    }

    private const string SystemTag = "[System]";

    private StringBuilder m_StringBuilder = new StringBuilder();


    public void Setup(MessageWindow window)
    {
        m_Window = window;

    }

    protected override void OnUpdate()
    {
        if (m_Window == null)
            return;

        var clearMW = SystemAPI.QueryBuilder().WithAll<ClearMWComponent>().Build();

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        var buffer = ecb.CreateCommandBuffer(this.CheckedStateRef.WorldUnmanaged);

        bool build = false;

        if (!RpcUiData.Messages.Data.IsEmpty)
        {
            build = true;
        }

        if (RpcUiData.Messages.Data.IsCreated && RpcUiData.Messages.Data.TryDequeue(out var message))
        {
            m_StringBuilder.AppendLine($"{message}");

        }


        if (build)
        {
            m_Window.Text.text = m_StringBuilder.ToString();
        }

        if (clearMW.CalculateEntityCount() != 0)
        {

            m_StringBuilder.Clear();
            m_Window.Text.text = "";
        }




        buffer.DestroyEntity(clearMW);
    }
}