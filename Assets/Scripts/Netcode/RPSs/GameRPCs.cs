using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine;

public struct UpdateNameRpc : IRpcCommand
{
    public FixedString128Bytes Name;
    public int networkIdToUpdate;
}

public struct ChatMessageRpc : IRpcCommand
{
    public FixedString128Bytes Message;
}