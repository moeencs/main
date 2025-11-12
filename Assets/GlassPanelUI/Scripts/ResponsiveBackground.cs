using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a background image responsive and fills the screen properly
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class ResponsiveBackground : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private AspectRatioFitter.AspectMode aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
    [SerializeField] private bool maintainAspectRatio = true;
    
    private RectTransform rectTransform;
    private Image image;
    private AspectRatioFitter aspectFitter;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        
        // Ensure the background fills the entire canvas
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        if (maintainAspectRatio)
        {
            SetupAspectRatioFitter();
        }
    }

    private void SetupAspectRatioFitter()
    {
        aspectFitter = gameObject.GetComponent<AspectRatioFitter>();
        if (aspectFitter == null)
        {
            aspectFitter = gameObject.AddComponent<AspectRatioFitter>();
        }

        aspectFitter.aspectMode = aspectMode;
        
        // Calculate aspect ratio from sprite
        if (image.sprite != null)
        {
            Rect spriteRect = image.sprite.rect;
            float ratio = spriteRect.width / spriteRect.height;
            aspectFitter.aspectRatio = ratio;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        // Update when screen size changes
        if (aspectFitter != null && image.sprite != null)
        {
            Rect spriteRect = image.sprite.rect;
            float ratio = spriteRect.width / spriteRect.height;
            aspectFitter.aspectRatio = ratio;
        }
    }
}
