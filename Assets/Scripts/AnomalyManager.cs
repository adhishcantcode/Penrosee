using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Anomaly
{
    public string anomalyName;
    public GameObject[] normalObjects;
    public GameObject[] alteredObjects;
}

public class AnomalyManager : MonoBehaviour
{
    [Header("Floor A Anomalies")]
    public List<Anomaly> floorAAnomalies = new List<Anomaly>();

    [Header("Floor B Anomalies")]
    public List<Anomaly> floorBAnomalies = new List<Anomaly>();

    [Range(0f, 1f)]
    public float noAnomalyChance = 0.25f;

    [Header("Mistakes")]
    public int maxMistakes = 3;
    public int mistakesRemaining = 3;
    public GameObject[] mistakeIndicators = new GameObject[3];
    public Material redMaterial;
    public Material whiteMaterial;

    [Header("Anomaly Progress")]
    public int totalAnomaliesToCollect = 6;
    public int anomaliesCollected = 0;
    public Image mainProgressBar;

    [Header("Glitch Effect")]
    // Assign the GlitchEffect component, NOT the Camera component.
    public GlitchEffect glitchEffect;
    public float mistakeGlitchDuration = 1.5f;
    public float collectionGlitchDuration = 0.25f;
    public float permanentGlitchIntensity = 1f;

    [Header("Game Over - Enemy Reveal")]
    public GameObject enemyObject;
    public float enemyRevealDuration = 1.5f;
    public AnimationCurve enemyRevealCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Game Over - Black Screen")]
    public GameObject blackScreenObject;
    public float restartDelay = 1f;

    [Header("Win - Activate Objects")]
    public GameObject[] objectsToActivateOnWin;

    [Header("Win - Deactivate Objects")]
    public GameObject[] objectsToDeactivateOnWin;

    public event System.Action onGameOver;

    public bool HasActiveAnomaly { get; private set; }
    public bool CurrentAnomalyDetected { get; private set; }
    public bool GameOver { get; private set; }
    public bool HasWon { get; private set; }

    [Header("Anomaly Status UI")]
    public TMP_Text anomalyStatusText;
    public float statusMessageDuration = 3f;

    private Coroutine statusMessageCoroutine;

    private Anomaly currentAnomaly;

    private Coroutine glitchCoroutine;
    private Coroutine collectionCoroutine;
    private Coroutine gameOverCoroutine;

    [Header("Win Glitch")]
    public float winGlitchDuration = 2f;

    private readonly Dictionary<List<Anomaly>, List<Anomaly>> anomalyBags =
        new Dictionary<List<Anomaly>, List<Anomaly>>();

    private void Start()
    {
        maxMistakes = Mathf.Max(1, maxMistakes);
        totalAnomaliesToCollect = Mathf.Max(1, totalAnomaliesToCollect);

        mistakesRemaining = maxMistakes;
        anomaliesCollected = 0;

        HasActiveAnomaly = false;
        CurrentAnomalyDetected = false;
        GameOver = false;
        HasWon = false;

        if (anomalyStatusText != null)
            anomalyStatusText.text = "";

        // Only disable the effect component. Never disable the camera.
        if (glitchEffect != null)
            glitchEffect.enabled = false;

        if (enemyObject != null)
            enemyObject.SetActive(false);

        if (blackScreenObject != null)
            blackScreenObject.SetActive(false);

        ResetMistakeIndicators();
        UpdateProgressBar();
        ResetAllAnomalyObjects();
    }

    // --------------------------------------------------
    // STATUS MESSAGE
    // --------------------------------------------------

    private void DisplayStatusMessage(string message)
    {
        if (anomalyStatusText == null)
            return;

        if (statusMessageCoroutine != null)
            StopCoroutine(statusMessageCoroutine);

        statusMessageCoroutine = StartCoroutine(
            ShowStatusMessage(message)
        );
    }

    private IEnumerator ShowStatusMessage(string message)
    {
        if (anomalyStatusText == null)
        {
            statusMessageCoroutine = null;
            yield break;
        }

        anomalyStatusText.gameObject.SetActive(true);
        anomalyStatusText.text = message;

        yield return new WaitForSecondsRealtime(
            statusMessageDuration
        );

        if (anomalyStatusText != null)
            anomalyStatusText.text = "";

        statusMessageCoroutine = null;
    }

    // --------------------------------------------------
    // FLOOR TRIGGERS
    // --------------------------------------------------

    public void TriggerFloorA()
    {
        StartNextLoop(floorAAnomalies);
    }

    public void TriggerFloorB()
    {
        StartNextLoop(floorBAnomalies);
    }

    public void StartNextLoop(List<Anomaly> floorAnomalies)
    {
        if (GameOver || HasWon)
            return;

        ResolvePreviousLoop();

        if (GameOver || HasWon)
            return;

        SelectAnomaly(floorAnomalies);
    }

    // --------------------------------------------------
    // ANOMALY SELECTION
    // --------------------------------------------------

    private void SelectAnomaly(List<Anomaly> floorAnomalies)
    {
        HasActiveAnomaly = false;
        CurrentAnomalyDetected = false;
        currentAnomaly = null;

        ResetAllAnomalyObjects();

        if (floorAnomalies == null || floorAnomalies.Count == 0)
            return;

        // An empty loop is not a mistake.
        if (Random.value < noAnomalyChance)
        {
            Debug.Log("This loop has no anomaly.");
            return;
        }

        if (!anomalyBags.TryGetValue(
                floorAnomalies,
                out List<Anomaly> bag))
        {
            bag = new List<Anomaly>();
            anomalyBags.Add(floorAnomalies, bag);
        }

        if (bag.Count == 0)
            RefillBag(floorAnomalies, bag);

        if (bag.Count == 0)
            return;

        int lastIndex = bag.Count - 1;

        currentAnomaly = bag[lastIndex];
        bag.RemoveAt(lastIndex);

        SetAnomalyObjects(currentAnomaly, true);
        HasActiveAnomaly = true;

        Debug.Log("Active anomaly: " + currentAnomaly.anomalyName);
    }

    private void RefillBag(List<Anomaly> source, List<Anomaly> bag)
    {
        bag.Clear();

        foreach (Anomaly anomaly in source)
        {
            if (anomaly != null)
                bag.Add(anomaly);
        }

        // Shuffle the bag.
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            Anomaly temp = bag[i];
            bag[i] = bag[j];
            bag[j] = temp;
        }
    }

    private void SetAnomalyObjects(Anomaly anomaly, bool showAltered)
    {
        if (anomaly == null)
            return;

        SetObjectsActive(anomaly.normalObjects, !showAltered);
        SetObjectsActive(anomaly.alteredObjects, showAltered);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void ResetAllAnomalyObjects()
    {
        ResetAnomalyList(floorAAnomalies);
        ResetAnomalyList(floorBAnomalies);
    }

    private void ResetAnomalyList(List<Anomaly> anomalies)
    {
        if (anomalies == null)
            return;

        foreach (Anomaly anomaly in anomalies)
        {
            if (anomaly == null)
                continue;

            SetObjectsActive(anomaly.normalObjects, true);
            SetObjectsActive(anomaly.alteredObjects, false);
        }
    }

    // --------------------------------------------------
    // DETECTION
    // --------------------------------------------------

    public bool IsActiveAnomalyObject(GameObject target)
    {
        if (target == null ||
            !HasActiveAnomaly ||
            CurrentAnomalyDetected ||
            GameOver ||
            HasWon ||
            currentAnomaly == null ||
            currentAnomaly.alteredObjects == null)
        {
            return false;
        }

        foreach (GameObject obj in currentAnomaly.alteredObjects)
        {
            if (obj == null)
                continue;

            if (target == obj ||
                target.transform.IsChildOf(obj.transform) ||
                obj.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    // Keep this signature compatible with AnomalyDetector.cs.
    public bool TryDetectAnomaly(GameObject target)
    {
        if (!IsActiveAnomalyObject(target))
            return false;

        CurrentAnomalyDetected = true;
        HasActiveAnomaly = false;

        DisplayStatusMessage("TIMELINE RESTORED");

        // Restore the detected anomaly to its normal appearance.
        SetAnomalyObjects(currentAnomaly, false);

        if (collectionCoroutine != null)
            StopCoroutine(collectionCoroutine);

        collectionCoroutine = StartCoroutine(CollectionRoutine());

        return true;
    }

    // --------------------------------------------------
    // COLLECTION AND IMAGE PROGRESS
    // --------------------------------------------------

    private IEnumerator CollectionRoutine()
    {
        anomaliesCollected = Mathf.Min(
            anomaliesCollected + 1,
            totalAnomaliesToCollect
        );

        UpdateProgressBar();

        Debug.Log(
            "Anomalies collected: " +
            anomaliesCollected + " / " +
            totalAnomaliesToCollect
        );

        if (anomaliesCollected >= totalAnomaliesToCollect)
        {
            collectionCoroutine = null;
            TriggerWin();
            yield break;
        }

        if (glitchEffect != null && !GameOver && !HasWon)
        {
            glitchEffect.enabled = true;

            yield return new WaitForSecondsRealtime(
                collectionGlitchDuration
            );

            if (!GameOver && !HasWon && glitchEffect != null)
                glitchEffect.enabled = false;
        }

        collectionCoroutine = null;
    }

    private void UpdateProgressBar()
    {
        if (mainProgressBar == null)
            return;

        // Image component, not Slider.
        mainProgressBar.fillAmount = Mathf.Clamp01(
            (float)anomaliesCollected /
            Mathf.Max(1, totalAnomaliesToCollect)
        );
    }

    // --------------------------------------------------
    // WIN
    // --------------------------------------------------

    private void TriggerWin()
    {
        if (HasWon || GameOver)
            return;

        HasWon = true;
        HasActiveAnomaly = false;
        CurrentAnomalyDetected = false;

        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
            collectionCoroutine = null;
        }

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        // Play the glitch when the final anomaly is collected.
        if (glitchEffect != null)
            glitchEffect.enabled = true;

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        // Keep the glitch active for the configured duration.
        yield return new WaitForSecondsRealtime(winGlitchDuration);

        // Disable the glitch after the effect finishes.
        if (glitchEffect != null)
            glitchEffect.enabled = false;

        // Activate the assigned win objects.
        if (objectsToActivateOnWin != null)
        {
            foreach (GameObject obj in objectsToActivateOnWin)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        // Deactivate the assigned objects.
        if (objectsToDeactivateOnWin != null)
        {
            foreach (GameObject obj in objectsToDeactivateOnWin)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        Debug.Log("YOU WIN! All anomalies collected.");
    }

    // --------------------------------------------------
    // MISSED ANOMALIES
    // --------------------------------------------------

    private void ResolvePreviousLoop()
    {
        // No active anomaly means nothing was missed.
        if (!HasActiveAnomaly || CurrentAnomalyDetected)
            return;

        HasActiveAnomaly = false;
        CurrentAnomalyDetected = false;

        DisplayStatusMessage("TIMELINE CORRUPTED");

        // Reset collected progress after a missed anomaly.
        anomaliesCollected = 0;
        UpdateProgressBar();

        mistakesRemaining = Mathf.Max(0, mistakesRemaining - 1);
        UpdateMistakeIndicators();

        Debug.Log(
            "Anomaly missed! Mistakes remaining: " +
            mistakesRemaining
        );

        if (mistakesRemaining <= 0)
        {
            TriggerGameOver();
            return;
        }

        TriggerTemporaryGlitch(mistakeGlitchDuration);
    }

    // --------------------------------------------------
    // MISTAKE INDICATORS
    // --------------------------------------------------

    private void ResetMistakeIndicators()
    {
        if (mistakeIndicators == null)
            return;

        foreach (GameObject indicator in mistakeIndicators)
            SetIndicatorMaterial(indicator, redMaterial);
    }

    private void UpdateMistakeIndicators()
    {
        if (mistakeIndicators == null)
            return;

        int mistakesUsed = maxMistakes - mistakesRemaining;

        for (int i = 0; i < mistakeIndicators.Length; i++)
        {
            Material material = i < mistakesUsed
                ? whiteMaterial
                : redMaterial;

            SetIndicatorMaterial(mistakeIndicators[i], material);
        }
    }

    private void SetIndicatorMaterial(
        GameObject indicator,
        Material material)
    {
        if (indicator == null || material == null)
            return;

        Renderer rend = indicator.GetComponent<Renderer>();

        if (rend == null)
            rend = indicator.GetComponentInChildren<Renderer>();

        if (rend != null)
            rend.sharedMaterial = material;
    }

    // --------------------------------------------------
    // GAME OVER
    // --------------------------------------------------

    private void TriggerGameOver()
    {
        if (GameOver || HasWon)
            return;

        GameOver = true;
        HasActiveAnomaly = false;
        CurrentAnomalyDetected = false;

        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
            collectionCoroutine = null;
        }

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        // Only enable the effect, never the Camera component itself.
        if (glitchEffect != null)
            glitchEffect.enabled = true;

        if (gameOverCoroutine != null)
            StopCoroutine(gameOverCoroutine);

        gameOverCoroutine = StartCoroutine(RevealEnemyAndRestart());
    }

    private IEnumerator RevealEnemyAndRestart()
    {
        if (enemyObject != null)
        {
            Vector3 targetScale = enemyObject.transform.localScale;

            enemyObject.SetActive(true);
            enemyObject.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, enemyRevealDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                float curveValue = enemyRevealCurve != null
                    ? enemyRevealCurve.Evaluate(t)
                    : t;

                enemyObject.transform.localScale = Vector3.LerpUnclamped(
                    Vector3.zero,
                    targetScale,
                    curveValue
                );

                yield return null;
            }

            enemyObject.transform.localScale = targetScale;
        }

        if (blackScreenObject != null)
            blackScreenObject.SetActive(true);

        onGameOver?.Invoke();

        yield return StartCoroutine(RestartSceneAfterDelay());
    }

    private IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(restartDelay);

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // --------------------------------------------------
    // TEMPORARY GLITCH
    // --------------------------------------------------

    private void TriggerTemporaryGlitch(float duration)
    {
        if (GameOver || HasWon || glitchEffect == null)
            return;

        if (glitchCoroutine != null)
            StopCoroutine(glitchCoroutine);

        glitchCoroutine = StartCoroutine(
            TemporaryGlitchRoutine(duration)
        );
    }

    private IEnumerator TemporaryGlitchRoutine(float duration)
    {
        if (glitchEffect != null)
            glitchEffect.enabled = true;

        yield return new WaitForSecondsRealtime(duration);

        if (!GameOver && !HasWon && glitchEffect != null)
            glitchEffect.enabled = false;

        glitchCoroutine = null;
    }
}