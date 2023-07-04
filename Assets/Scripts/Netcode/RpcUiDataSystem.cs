using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

// Management for the queue which passes data between DOTS and GameObject systems, this way
// the two are decoupled a bit cleaner
[UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class RpcUiDataSystem : SystemBase
{
    private bool m_OwnsData;
    protected override void OnCreate()
    {
        m_OwnsData = !RpcUiData.Messages.Data.IsCreated;
        if (m_OwnsData)
        {

            RpcUiData.Messages.Data = new UnsafeRingQueue<FixedString128Bytes>(32, Allocator.Persistent);
        }
        Enabled = false;
    }

    protected override void OnUpdate() { }

    protected override void OnDestroy()
    {
        if (m_OwnsData)
        {
            RpcUiData.Messages.Data.Dispose();

        }
    }
}




public abstract class RpcUiData
{
    public static readonly SharedStatic<UnsafeRingQueue<FixedString128Bytes>> Messages = SharedStatic<UnsafeRingQueue<FixedString128Bytes>>.GetOrCreate<MessagesKey>();


    // Identifiers for the shared static fields
    private class MessagesKey { }

}