using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Calculate the direction from the canvas to the camera.
            Vector3 direction = mainCamera.transform.position - transform.position;
            // Lock the vertical component so the canvas stays upright.
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                // Calculate a rotation that looks along the horizontal direction.
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                // Optionally, if you need to flip the UI so that text isn’t backwards, rotate 180° on Y.
                targetRotation *= Quaternion.Euler(0, 180, 0);
                transform.rotation = targetRotation;
            }
        }
    }
}
