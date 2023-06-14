using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu]
public class SceneSO : ScriptableObject
{
#if UNITY_EDITOR
    public SceneAsset sceneAsset;
#endif
    [ReadOnly]
    public SerializableGuid sceneGuid;
  
    private void OnValidate()
    {
#if UNITY_EDITOR
        var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(sceneAsset));
    
        Debug.Log($"scene guid {guid.ToString()}");
        sceneGuid = guid.ToString();
#endif
    }

    [Button]
    private void UpdateGuid()
    {
        OnValidate();
    }

}
