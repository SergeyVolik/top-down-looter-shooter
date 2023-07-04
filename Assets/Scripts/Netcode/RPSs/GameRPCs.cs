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

public struct GetNameRpc : IRpcCommand
{
    public int networkId;
}

public struct GetNameResultRpc : IRpcCommand
{
    public FixedString128Bytes Name;
    public int networkId;
}

public struct ChatMessageRpc : IRpcCommand
{
    public FixedString128Bytes Message;
}