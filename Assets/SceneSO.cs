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
    public string sceneGuid;
    private void OnValidate()
    {
#if UNITY_EDITOR
        sceneGuid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(sceneAsset)).ToString();
#endif
    }

    [Button]
    private void UpdateGuid()
    {
        OnValidate();
    }

}
