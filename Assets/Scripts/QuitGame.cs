using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitGame : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            
            UnityEditor.EditorApplication.isPlaying = false;
#else
  Application.Quit();
#endif

        });
    }
}
