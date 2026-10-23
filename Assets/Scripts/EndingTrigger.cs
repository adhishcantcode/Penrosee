using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;

public class EndingTrigger : MonoBehaviour
{
    [Header("Player")]
    public FirstPersonController firstPersonController;

    [Header("Ending")]
    public GameObject endingObject;
    public float endingDuration = 15f;

    [Header("Objects to Disable")]
    public GameObject objectToTurnOff;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    private bool endingStarted;

    private void OnTriggerEnter(Collider other)
    {
        if (endingStarted)
            return;

        // Only trigger when the player enters.
        if (firstPersonController != null &&
            other.gameObject != firstPersonController.gameObject &&
            !other.transform.IsChildOf(firstPersonController.transform))
        {
            return;
        }

        if (firstPersonController == null)
        {
            Debug.LogWarning(
                "EndingTrigger: Assign the FirstPersonController in the Inspector."
            );
            return;
        }

        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        endingStarted = true;

        // Turn off the assigned GameObject.
        if (objectToTurnOff != null)
            objectToTurnOff.SetActive(false);

        // Stop player movement and mouse-look.
        firstPersonController.enabled = false;

        // Unlock and show the cursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show the ending GameObject.
        if (endingObject != null)
            endingObject.SetActive(true);

        // Keep the ending visible for the configured duration.
        yield return new WaitForSecondsRealtime(endingDuration);

        // Load the main menu.
        SceneManager.LoadScene(mainMenuSceneName);
    }
}