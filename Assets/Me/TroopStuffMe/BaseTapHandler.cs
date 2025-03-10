using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles pointer clicks on an enemy base marker, telling AttackManager to open the troop selection UI.
/// Skips if user taps their own base, doesn't have a base, or if they're on the "Base" tab.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BaseTapHandler : MonoBehaviour, IPointerClickHandler
{
    private BaseMarker baseMarker;

    private void Awake()
    {
        baseMarker = GetComponent<BaseMarker>();
    }

    /// <summary>
    /// Called by Unity when the user clicks on this collider. 
    /// It handles logic to open the Attack UI, re-center the map, etc.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("BaseTapHandler.OnPointerClick => Called. Attempting to open TroopSelectPanel.");
        if (baseMarker == null) return;

        string enemyOwnerId = baseMarker.PlayerId;
        if (string.IsNullOrEmpty(enemyOwnerId))
        {
            Debug.LogWarning("BaseTapHandler: baseMarker.PlayerId is null or empty. Skipping attack logic.");
            return;
        }

        // Skip if local user's base
        string localUserId = PlayerPrefs.GetString("playerId");
        if (enemyOwnerId == localUserId)
        {
            Debug.Log("Tapped on local base => ignoring attack logic.");
            return;
        }

        // Skip if user has no base
        if (!BaseManager.Instance.HasBase())
        {
            Debug.Log("User has no base, skipping troop panel + re-center.");
            return;
        }

        // Skip if we’re on tab #1 (Base tab)
        var tm = FindObjectOfType<TabManager>();
        if (tm != null && tm.CurrentTabIndex == 1)
        {
            Debug.Log("User is in Base tab => ignoring enemy base tap.");
            return;
        }

        Debug.Log("BaseTapHandler: Tapped base with PlayerId=" + baseMarker.PlayerId);

        // If valid, open the troop selection UI
        AttackManager.Instance.OpenTroopSelectionUI(
            baseMarker.PlayerId,
            baseMarker.Username,
            baseMarker.Health,
            baseMarker.Level
        );

        // Re-center map if we have coords
        var enemyCoords = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (enemyCoords != null)
        {
            BaseManager.Instance.ShowEnemyBaseOnMap(enemyCoords.Value);
        }
    }
}
