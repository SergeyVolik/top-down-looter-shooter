using Sirenix.OdinInspector;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class AssetWithGuid : ScriptableObject
{

    [ReadOnly]
    public string guid;
    

    public virtual void OnValidate()
    {
#if UNITY_EDITOR
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out long _);
#endif

    }

    public Guid GetGuid()
    {
        return new Guid(guid);
    }
}