using Sirenix.OdinInspector;
using SV;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetDatabase<T> : ScriptableObject where T : AssetWithGuid
{
    public T[] items;

    public Dictionary<Guid, T> itemsDic;
    public void OnValidate()
    {
#if UNITY_EDITOR
        items = Resources.FindObjectsOfTypeAll<T>();

#endif
    }

    private void OnEnable()
    {
        OnValidate();
        itemsDic = new Dictionary<Guid, T>();

        foreach (var item in items)
        {
            var guid = item.GetGuid();

            Debug.Log(guid);

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