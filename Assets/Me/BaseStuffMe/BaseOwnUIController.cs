using UnityEngine;
using Mapbox.Unity.Map;
using UnityEngine.UI;

/// <summary>
/// UI overlay for the local player's base (or can be adapted for any base).
/// Disables the canvas if the map's zoom is below a threshold.
/// </summary>
public class BaseOwnUIController : MonoBehaviour
{
    private Canvas _canvas;
    private AbstractMap _map;

    [Tooltip("If zoom is below this level, hide this UI.")]
    [SerializeField] private float disableZoomThreshold = 16f;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _map = FindObjectOfType<AbstractMap>();
        if (_map == null)
        {
            Debug.LogWarning("[BaseOwnUIController] No AbstractMap found in scene.");
        }
    }

    private void Start()
    {
        // Position canvas above the base
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void Update()
    {
        if (_map == null || _canvas == null) return;

        // If the zoom level is less than the threshold, disable the canvas
        if (_map.Zoom < disableZoomThreshold && _canvas.enabled)
        {
            _canvas.enabled = false;
        }
        else if (_map.Zoom >= disableZoomThreshold && !_canvas.enabled)
        {
            _canvas.enabled = true;
        }
    }
}
