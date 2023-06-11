using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOnAwake : MonoBehaviour
{
    public GameObject[] toActivate;

    private void Start()
    {
        foreach (GameObject go in toActivate) {
            go.SetActive(true);
        }
    }
   
}
