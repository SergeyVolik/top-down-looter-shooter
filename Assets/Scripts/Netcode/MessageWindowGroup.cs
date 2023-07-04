using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial class MessageWindowGroup : ComponentSystemGroup
{
    public void Setup(MessageWindow window)
    {
        var sys = EntityManager.World.GetOrCreateSystemManaged<MessageWindowInputSystem>();
        var sys1 = EntityManager.World.GetOrCreateSystemManaged<ShowMessageWindowExecutorSystem>();

        sys.Setup(window);
        sys1.Setup(window);
    }
}