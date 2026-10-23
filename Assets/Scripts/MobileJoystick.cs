
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("References")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;

    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController player;

    [Header("Joystick Settings")]
    public float handleRange = 60f;

    [Range(0f, 1f)]
    public float sprintThreshold = 0.85f;

    private RectTransform canvasRect;
    private Canvas canvas;
    private Camera uiCamera;

    private bool isDragging;
    private int activeFingerId = -1;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvasRect = canvas.transform as RectTransform;

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;
        }

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging || joystickBackground == null ||
            joystickHandle == null || player == null ||
            canvasRect == null)
            return;

        isDragging = true;
        activeFingerId = eventData.pointerId;

        // Convert touch position into Canvas-local coordinates.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
        {
            isDragging = false;
            activeFingerId = -1;
            return;
        }

        // Clamp the joystick base so its entire circle stays on screen.
        Rect screenRect = canvasRect.rect;

        float halfWidth = joystickBackground.rect.width * 0.5f;
        float halfHeight = joystickBackground.rect.height * 0.5f;

        localPoint.x = Mathf.Clamp(
            localPoint.x,
            screenRect.xMin + halfWidth,
            screenRect.xMax - halfWidth
        );

        localPoint.y = Mathf.Clamp(
            localPoint.y,
            screenRect.yMin + halfHeight,
            screenRect.yMax - halfHeight
        );

        // Convert Canvas-local coordinates into the background's parent space.
        RectTransform backgroundParent =
            joystickBackground.parent as RectTransform;

        if (backgroundParent != null)
        {
            Vector3 worldPosition = canvasRect.TransformPoint(localPoint);

            Vector3 parentLocalPosition =
                backgroundParent.InverseTransformPoint(worldPosition);

            joystickBackground.anchoredPosition =
                new Vector2(parentLocalPosition.x, parentLocalPosition.y);
        }
        else
        {
            joystickBackground.anchoredPosition = localPoint;
        }

        joystickBackground.gameObject.SetActive(true);
        joystickHandle.anchoredPosition = Vector2.zero;

        UpdateJoystick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activeFingerId)
            return;

        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activeFingerId)
            return;

        isDragging = false;
        activeFingerId = -1;

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);

        if (player != null)
        {
            player.SetMobileMoveInput(Vector2.zero);
            player.SetMobileSprintAmount(0f, false);
        }
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (joystickBackground == null ||
            joystickHandle == null ||
            player == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
            return;

        Rect joystickRect = joystickBackground.rect;

        float radiusX = joystickRect.width * 0.5f;
        float radiusY = joystickRect.height * 0.5f;

        Vector2 input = new Vector2(
            radiusX > 0f ? localPoint.x / radiusX : 0f,
            radiusY > 0f ? localPoint.y / radiusY : 0f
        );

        input = Vector2.ClampMagnitude(input, 1f);

        joystickHandle.anchoredPosition = input * handleRange;

        // Preserve existing movement and progressive sprint behavior.
        player.SetMobileMoveInput(input);

        bool sprinting = input.y >= sprintThreshold;
        player.SetMobileSprintAmount(input.y, sprinting);
    }

    private void OnDisable()
    {
        isDragging = false;
        activeFingerId = -1;

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);

        if (player != null)
        {
            player.SetMobileMoveInput(Vector2.zero);
            player.SetMobileSprintAmount(0f, false);
        }
    }
}
