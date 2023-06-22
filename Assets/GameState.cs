using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public enum Status
    {
        None,
        Destroyed
    }
    public static Status status;

    private void OnDestroy()
    {
        status = Status.Destroyed;
    }
    public static bool IsDestroyed() => status == Status.Destroyed;
}
