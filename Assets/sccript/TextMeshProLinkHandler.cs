using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextMeshProLinkHandler : MonoBehaviour, IPointerClickHandler
{
    // This is the event that will appear in the Inspector
    public UnityEvent<string> OnLinkClicked;

    private TextMeshProUGUI p_textMeshPro;
    private Canvas p_canvas;

    void Awake()
    {
        p_textMeshPro = GetComponent<TextMeshProUGUI>();
        p_canvas = GetComponentInParent<Canvas>();
    }

    // This function is called when a click is detected on the object
    public void OnPointerClick(PointerEventData eventData)
    {
        // Use the camera associated with the Canvas to check for link intersection
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(p_textMeshPro, eventData.position, p_canvas.worldCamera);

        // If a link was clicked
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = p_textMeshPro.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID();

            // Fire our public event, sending the link ID
            OnLinkClicked.Invoke(linkID);
        }
    }
}
