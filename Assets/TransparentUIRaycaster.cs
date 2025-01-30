using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentUIRaycaster : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPressRaycast.gameObject == null)
        {
            // Send the event through to Mapbox
            eventData.pointerPress = null;
        }
    }
}

