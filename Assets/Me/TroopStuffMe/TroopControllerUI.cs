using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TroopUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text goingToText;
    [SerializeField] private TMP_Text comingFromText;

    [SerializeField] private TMP_Text timeLeftText; // a new text



    private void Start()
    {
        transform.localPosition = new Vector3(0f, 2.5f, 0f);
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void SetTexts(string fromUsername, string toUsername)
    {
        goingToText.text = $"Attacking: {toUsername}'s base";
        comingFromText.text = $"Sent By: {fromUsername}'s base";
    }

    public void SetTimeLeft(double timeLeftSec)
    {
        if (timeLeftText != null)
        {
            if (timeLeftSec < 0) timeLeftSec = 0;
            double mins = timeLeftSec / 60.0;
            timeLeftText.text = $"Base reached in: {mins:F1} min";
        }
    }
}

