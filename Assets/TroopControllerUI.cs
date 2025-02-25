using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TroopUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text goingToText;
    [SerializeField] private TMP_Text comingFromText;


    private void Start()
    {
        // For a top-down view, offset the canvas upward relative to the base.
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void SetTexts(string fromUsername, string toUsername)
    {
        goingToText.text = $"Going to: {toUsername}'s base";
        comingFromText.text = $"From: {fromUsername}'s base";
    }
}

