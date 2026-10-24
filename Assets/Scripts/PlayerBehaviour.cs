
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public GameObject Flashlight;
    public KeyCode flashlightKey = KeyCode.F;

    [Header("Battery Settings")]
    public GameObject batterySlider;
    public float battery = 100f;
    public float batteryMax = 100f;
    public float removeBatteryValue = 0.05f;
    public float secondToRemoveBaterry = 5f;

    [Header("Audio Settings")]
    public AudioClip slenderNoise;
    public AudioClip cameraNoise;
    public bool paused;

    private Light flashlightSpotlight;
    private AudioSource audioSource;
    private Coroutine batteryCoroutine;
    private bool flashlightOn = true;
    private Slider batterySliderComponent;

    void Start()
    {
        battery = batteryMax;

        if (batterySlider != null)
        {
            batterySliderComponent = batterySlider.GetComponent<Slider>();

            if (batterySliderComponent != null)
            {
                batterySliderComponent.maxValue = batteryMax;
                batterySliderComponent.value = battery;
            }
        }

        if (Flashlight != null)
        {
            Transform spotlightTransform =
                Flashlight.transform.Find("Spotlight");

            if (spotlightTransform != null)
                flashlightSpotlight = spotlightTransform.GetComponent<Light>();

            flashlightOn = flashlightSpotlight != null &&
                           flashlightSpotlight.gameObject.activeSelf;
        }

        audioSource = GetComponent<AudioSource>();

        batteryCoroutine = StartCoroutine(
            RemoveBaterryCharge(
                removeBatteryValue,
                secondToRemoveBaterry
            )
        );
    }

    void Update()
    {
        // PC flashlight toggle.
        if (!paused && Input.GetKeyDown(flashlightKey))
        {
            ToggleFlashlight();
        }

        // Update battery UI.
        if (batterySliderComponent != null)
            batterySliderComponent.value = battery;

        // Adjust flashlight intensity based on battery.
        UpdateFlashlightIntensity();
    }

    private void ToggleFlashlight()
    {
        if (Flashlight == null || flashlightSpotlight == null)
            return;

        // Do not allow the flashlight to turn on with an empty battery.
        if (!flashlightOn && battery <= 0f)
            return;

        flashlightOn = !flashlightOn;

        // Toggle the actual light, not the torch model.
        flashlightSpotlight.gameObject.SetActive(flashlightOn);
    }

    // Called by the mobile double-tap look area.
    public void ToggleFlashlightMobile()
    {
        if (paused)
            return;

        ToggleFlashlight();
    }

    void UpdateFlashlightIntensity()
    {
        if (flashlightSpotlight == null)
            return;

        if (battery <= 0f)
            flashlightSpotlight.intensity = 0f;
        else if (battery / batteryMax <= 0.1f)
            flashlightSpotlight.intensity = 1.35f;
        else if (battery / batteryMax <= 0.25f)
            flashlightSpotlight.intensity = 2f;
        else if (battery / batteryMax <= 0.5f)
            flashlightSpotlight.intensity = 2f;
        else
            flashlightSpotlight.intensity = 2.5f;
    }

    public IEnumerator RemoveBaterryCharge(float value, float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            if (flashlightOn &&
                flashlightSpotlight != null &&
                flashlightSpotlight.gameObject.activeSelf &&
                battery > 0f)
            {
                battery = Mathf.Max(0f, battery - value);

                Debug.Log("Flashlight battery: " + battery);

                if (battery <= 0f)
                {
                    flashlightOn = false;

                    // Turn off only the actual spotlight.
                    flashlightSpotlight.gameObject.SetActive(false);

                    Debug.Log("Flashlight battery depleted.");
                }
            }
        }
    }

    void OnDisable()
    {
        if (batteryCoroutine != null)
        {
            StopCoroutine(batteryCoroutine);
            batteryCoroutine = null;
        }
    }

    void OnEnable()
    {
        // Restart battery drain if this component is re-enabled.
        if (Application.isPlaying && batteryCoroutine == null)
        {
            batteryCoroutine = StartCoroutine(
                RemoveBaterryCharge(
                    removeBatteryValue,
                    secondToRemoveBaterry
                )
            );
        }
    }
}
