
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.Characters.FirstPerson;

public class MobileLookArea : MonoBehaviour,
    IDragHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private FirstPersonController player;
    [SerializeField] private PlayerBehaviour playerBehaviour;
    [SerializeField] private AnomalyDetector anomalyDetector;
    [SerializeField] private WeaponMotion weaponMotion;

    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTime = 0.3f;
    [SerializeField] private float tapMovementThreshold = 20f;

    private float lastTapTime = -1f;
    private Vector2 pointerDownPosition;
    private bool movedFinger;
    private int activePointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
            return;

        activePointerId = eventData.pointerId;
        pointerDownPosition = eventData.position;
        movedFinger = false;

        if (anomalyDetector != null)
            anomalyDetector.StartMobileAbsorption();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        if (Vector2.Distance(
                pointerDownPosition,
                eventData.position) > tapMovementThreshold)
        {
            movedFinger = true;
        }

        if (player != null)
            player.AddMobileLookInput(eventData.delta);

        if (weaponMotion != null)
            weaponMotion.AddMobileLookInput(eventData.delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        float movement = Vector2.Distance(
            pointerDownPosition,
            eventData.position
        );

        if (anomalyDetector != null)
            anomalyDetector.StopMobileAbsorption();

        if (!movedFinger && movement <= tapMovementThreshold)
        {
            float currentTime = Time.unscaledTime;

            if (lastTapTime >= 0f &&
                currentTime - lastTapTime <= doubleTapTime)
            {
                if (playerBehaviour != null)
                    playerBehaviour.ToggleFlashlightMobile();

                lastTapTime = -1f;
            }
            else
            {
                lastTapTime = currentTime;
            }
        }
        else
        {
            lastTapTime = -1f;
        }

        activePointerId = int.MinValue;
    }

    private void OnDisable()
    {
        if (anomalyDetector != null)
            anomalyDetector.StopMobileAbsorption();

        activePointerId = int.MinValue;
        lastTapTime = -1f;
    }
}
