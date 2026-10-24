using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class StaircaseLoopManager : MonoBehaviour
{
    [Header("Player")]
    public FirstPersonController player;

    [Header("Teleport Points")]
    public Transform openingCube;
    public Transform endingCube;

    [Header("Teleport Triggers")]
    public Collider openingTrigger;
    public Collider endingTrigger;

    [Header("Anomalies")]
    public AnomalyManager anomalyManager;

    private bool locked;

    public void EnterEnd(bool enteredOpening)
    {
        if (locked || player == null ||
            openingCube == null || endingCube == null)
            return;

        locked = true;


        if (anomalyManager != null)
        {
            if (enteredOpening)
                anomalyManager.TriggerFloorA();
            else
                anomalyManager.TriggerFloorB();
        }

        Vector3 offset = enteredOpening
            ? endingCube.position - openingCube.position
            : openingCube.position - endingCube.position;

        player.TeleportTo(player.transform.position + offset);

        Physics.SyncTransforms();

        InvokeRepeating(nameof(CheckUnlock), 0.1f, 0.1f);
    }

    private void CheckUnlock()
    {
        if (player == null ||
            openingTrigger == null ||
            endingTrigger == null)
            return;

        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller == null)
            return;

        Bounds playerBounds = controller.bounds;

        bool inOpening =
            openingTrigger.bounds.Intersects(playerBounds);

        bool inEnding =
            endingTrigger.bounds.Intersects(playerBounds);

        if (!inOpening && !inEnding)
        {
            CancelInvoke(nameof(CheckUnlock));
            locked = false;
        }
    }
}