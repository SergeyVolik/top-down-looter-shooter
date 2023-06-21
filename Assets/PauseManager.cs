using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPauseService
{
    void Pause();
    void Resume();
}
public class PauseManager : MonoBehaviour, IPauseService
{
    public static PauseManager Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<PauseManager>();
            return m_Instance;
        }
    }

    private static PauseManager m_Instance;


    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Resume()
    {
        Time.timeScale = 1;

    }
}
