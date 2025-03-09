using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class BaseTapHandler : MonoBehaviour, IPointerClickHandler
{
    private BaseMarker baseMarker;

    private void Awake()
    {
        baseMarker = GetComponent<BaseMarker>();
    }

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

        // Also skip if this is the local player's base
        string localUserId = PlayerPrefs.GetString("playerId");
        if (enemyOwnerId == localUserId)
        {
            Debug.Log("Tapped on local base => ignoring attack logic.");
            return;
        }

        // if user has no base, skip
        if (!BaseManager.Instance.HasBase())
        {
            Debug.Log("User has no base, skipping troop panel + re-center.");
            return;
        }

        // Also skip if we’re on tab #1 (Base Tab)
        var tm = FindObjectOfType<TabManager>();
        if (tm != null && tm.CurrentTabIndex == 1)
        {
            Debug.Log("User is in Base tab => ignoring enemy base tap.");
            return;
        }

        Debug.Log("BaseTapHandler: Tapped base with PlayerId=" + baseMarker.PlayerId);


        // If we get this far, open the troop UI
        AttackManager.Instance.OpenTroopSelectionUI(
            baseMarker.PlayerId,
            baseMarker.Username,
            baseMarker.Health,
            baseMarker.Level
        );

        // Re-center if the coords exist
        var enemyCoords = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (enemyCoords != null)
        {
            BaseManager.Instance.ShowEnemyBaseOnMap(enemyCoords.Value);
        }
    }


}

