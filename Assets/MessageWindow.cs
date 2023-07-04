using UnityEngine;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    private Canvas m_Canvas;
    private GraphicRaycaster m_CanvasRaycaster;

    [SerializeField]
    private TMPro.TextMeshProUGUI text;
    [SerializeField]
    private TMPro.TMP_InputField inputField;


    public TMPro.TMP_InputField InputField => inputField;

    [SerializeField]
    private Button m_Send;

    public TMPro.TextMeshProUGUI Text => text;
    private void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
        m_CanvasRaycaster = GetComponent<GraphicRaycaster>();


        var world = WorldExt.GetClientWorld();
        var system = world.GetOrCreateSystemManaged<MessageWindowGroup>();
        system.Setup(this);

        Activate(false);
        text.text = "";

        m_Send.onClick.AddListener(() =>
        {
            world.GetOrCreateSystemManaged<MessageWindowInputSystem>().SendRPC(InputField.text);
        });
    }





    public void Activate(bool activate)
    {
        m_Canvas.enabled = activate;
        m_CanvasRaycaster.enabled = activate;
        enabled = activate;
    }

}






