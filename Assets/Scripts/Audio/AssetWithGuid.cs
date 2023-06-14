using Sirenix.OdinInspector;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class AssetWithGuid : ScriptableObject
{

    [ReadOnly]
    public SerializableGuid guid;
    

    public virtual void OnValidate()
    {
#if UNITY_EDITOR
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out var guidStr, out long _);
        guid = new SerializableGuid(GetGuid(guidStr));
#endif

    }

    public Guid GetGuid()
    {
        return new Guid(guid);
    }

    public Guid GetGuid(string guidStr)
    {
        return new Guid(guidStr);
    }
}