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
    public static PauseManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Resume()
    {
        Time.timeScale = 1;

    }
}
