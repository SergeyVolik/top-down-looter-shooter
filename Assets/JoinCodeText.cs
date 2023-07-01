using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinCodeText : MonoBehaviour
{
    public TMPro.TextMeshProUGUI text;

    private void Awake()
    {
        var server = WorldExt.GetServerWorld();

        if (server != null)
        {
            server.GetOrCreateSystemManaged<RenderJoinCodeSystem>().text = text;
        }
        
    }
}
