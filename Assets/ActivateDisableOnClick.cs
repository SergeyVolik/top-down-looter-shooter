using UnityEngine;
using UnityEngine.UI;

public class ActivateDisableOnClick : MonoBehaviour
{
    [SerializeField]
    GameObject[] toActivate;
    [SerializeField]
    GameObject[] toDisable;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => { 
            foreach (GameObject go in toDisable) {
                go.SetActive(false);
            }
            foreach (GameObject go in toActivate)
            {
                go.SetActive(true);
            }
        });
    }

}
