using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GameSettingsSO : ScriptableObject
{
    public int targetFps;
    [Button]
    public void DeleteAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();

        
    }
}
