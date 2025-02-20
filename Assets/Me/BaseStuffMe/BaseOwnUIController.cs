using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;

public class BaseOwnUIController : MonoBehaviour
{
    private Canvas _canvas;
    private AbstractMap _map;
    public float disableZoomThreshold = 16f; // Adjust as needed

    private void Awake()
    {
        // Get the Canvas component on this object
        _canvas = GetComponent<Canvas>();

        // Retrieve the AbstractMap from the scene at runtime
        _map = FindObjectOfType<AbstractMap>();
        if (_map == null)
        {
            Debug.LogWarning("AbstractMap not found in the scene.");
        }
    }

    private void Start()
    {
        // For a top-down view, offset the canvas upward relative to the base.
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        // Rotate the canvas so it lies flat on the ground.
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void Update()
    {
        if (_map == null || _canvas == null) { return; }

        // If the zoom level is less than the threshold, disable the canvas (i.e. when zoomed out).
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
