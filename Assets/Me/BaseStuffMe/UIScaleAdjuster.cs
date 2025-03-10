using UnityEngine;

/// <summary>
/// Dynamically scales a UI element inversely based on its parent's scale, 
/// so that the UI remains consistently sized in screen space.
/// </summary>
public class UIScaleAdjuster : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = no inverse scaling, 1 = fully inverse scaling.")]
    [SerializeField] private float attenuation = 1f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        if (transform.parent == null) return;

        // The parent's uniform scale
        float parentScale = transform.parent.lossyScale.x;

        // Lerp between normal scale and inverse scale
        float adjustedFactor = Mathf.Lerp(parentScale, 1f / parentScale, attenuation);

        transform.localScale = initialScale * adjustedFactor;
    }
}
