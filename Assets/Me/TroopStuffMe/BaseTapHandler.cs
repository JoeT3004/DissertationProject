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

        // if user has no base, just skip re-centering
        if (!BaseManager.Instance.HasBase())
        {
            Debug.Log("User has no base, skipping troop panel + re-center.");
            return;
        }

        // Also skip if we’re on tab #1 (Base Tab):
        var tm = FindObjectOfType<TabManager>();
        if (tm != null && tm.CurrentTabIndex == 1)
        {
            Debug.Log("User is in Base tab => ignoring enemy base tap.");
            return;
        }

        // Otherwise proceed:
        AttackManager.Instance.OpenTroopSelectionUI(baseMarker.PlayerId, baseMarker.Username,
                                                   baseMarker.Health, baseMarker.Level);

        // Re-center
        string enemyOwnerId = baseMarker.PlayerId; // define it
        var enemyCoords = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (enemyCoords != null)
        {
            BaseManager.Instance.ShowEnemyBaseOnMap(enemyCoords.Value);
        }
    }


}
