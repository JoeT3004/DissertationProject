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

        if (baseMarker != null)
        {
            // Attack panel
            AttackManager.Instance.OpenTroopSelectionUI(
                baseMarker.PlayerId,
                baseMarker.Username,
                baseMarker.Health,
                baseMarker.Level
            );
        }

        // Re-center
        string enemyOwnerId = baseMarker.PlayerId; // define it
        var enemyCoords = AllBasesManager.Instance.GetBaseCoordinates(enemyOwnerId);
        if (enemyCoords != null)
        {
            // call your ShowEnemyBaseOnMap method
            BaseManager.Instance.ShowEnemyBaseOnMap(enemyCoords.Value);
        }
    }

}
