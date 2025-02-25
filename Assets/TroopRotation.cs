using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopRotation : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        // For a top-down view, offset the canvas upward relative to the base.
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        // Rotate the canvas so it lies flat on the ground.
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
