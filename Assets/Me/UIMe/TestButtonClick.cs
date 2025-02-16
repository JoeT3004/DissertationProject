using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestButtonClick : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => Debug.Log("Test Button Clicked!"));
    }
}

