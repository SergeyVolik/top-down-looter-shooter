using Sirenix.OdinInspector;
using SV;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetDatabase<T> : ScriptableObject where T : AssetWithGuid
{
    public T[] items;

    public Dictionary<Guid, T> itemsDic;
    public void OnValidate()
    {
#if UNITY_EDITOR
        items = Resources.FindObjectsOfTypeAll<T>();
        EditorUtility.SetDirty(this);

#endif
    }

    private void OnEnable()
    {
        OnValidate();
        itemsDic = new Dictionary<Guid, T>();

        foreach (var item in items)
        {
            if (item == null)
            {
                Debug.LogError($"AssetDatabase {typeof(T)} item is null");
                continue;
            }
            var guid = item.GetGuid();

            itemsDic.Add(guid, item);

        }
    }

    [Button]
    private void UpdateGuid()
    {
        OnEnable();
    }

    public T GetItem(Guid sfxSettingGuid)
    {
        T result = null;

        itemsDic.TryGetValue(sfxSettingGuid, out result);

        return result;
    }
}