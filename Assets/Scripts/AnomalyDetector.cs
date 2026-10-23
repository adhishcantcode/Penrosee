
using UnityEngine;
using UnityEngine.UI;

public class AnomalyDetector : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject flashlightSpotlight;
    public AnomalyManager anomalyManager;

    [Header("Detection Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float detectionTime = 1.5f;
    public float detectionDistance = 50f;

    [Header("Absorption Progress Bar")]
    public GameObject progressBarContainer;
    public Image progressBar;

    private float holdTimer;
    private bool mobileHoldInput;

    private void Start()
    {
        ResetProgress();

        if (progressBarContainer != null)
            progressBarContainer.SetActive(true);
    }

    private void Update()
    {
        if (playerCamera == null || anomalyManager == null)
        {
            ResetProgress();
            return;
        }

        bool flashlightOn =
            flashlightSpotlight != null &&
            flashlightSpotlight.activeInHierarchy;

        if (!flashlightOn ||
            anomalyManager.GameOver ||
            !anomalyManager.HasActiveAnomaly ||
            anomalyManager.CurrentAnomalyDetected)
        {
            ResetProgress();
            return;
        }

        // PC: hold E. Mobile: hold the look-around area.
        bool isHolding = Input.GetKey(interactKey) || mobileHoldInput;

        if (!isHolding)
        {
            ResetProgress();
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            detectionDistance))
        {
            ResetProgress();
            return;
        }

        GameObject target = hit.collider.gameObject;

        // Only absorb the currently active anomaly.
        if (!anomalyManager.IsActiveAnomalyObject(target))
        {
            ResetProgress();
            return;
        }

        holdTimer += Time.deltaTime;

        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(
                holdTimer / Mathf.Max(0.01f, detectionTime)
            );
        }

        if (holdTimer >= detectionTime)
        {
            bool collected = anomalyManager.TryDetectAnomaly(target);

            if (collected)
                Debug.Log("Anomaly successfully collected!");

            ResetProgress();
        }
    }

    // Called by MobileLookArea when the player starts holding.
    public void StartMobileAbsorption()
    {
        mobileHoldInput = true;
    }

    // Called when the player releases their finger.
    public void StopMobileAbsorption()
    {
        mobileHoldInput = false;
        ResetProgress();
    }

    private void ResetProgress()
    {
        holdTimer = 0f;

        if (progressBar != null)
            progressBar.fillAmount = 0f;
    }

    private void OnDisable()
    {
        mobileHoldInput = false;
        holdTimer = 0f;
    }
}
