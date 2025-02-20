using UnityEngine;

public class UIScaleAdjuster : MonoBehaviour
{
    // Set attenuation between 0 and 1.
    // 0: no counter-scaling (UI follows parent's scale)
    // 1: full inverse scaling (UI becomes smaller as parent scales up)
    [SerializeField] private float attenuation = 1f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        // Get the parent's scale (assuming uniform scaling).
        float parentScale = transform.parent.lossyScale.x;

        // Instead of adding to the scale, we invert it.
        // Lerp lets you blend between the parent's scale (attenuation = 0) 
        // and the inverse of the parent's scale (attenuation = 1).
        float adjustedFactor = Mathf.Lerp(parentScale, 1f / parentScale, attenuation);

        transform.localScale = initialScale * adjustedFactor;
    }
}
