
using UnityEngine;

public class StaircaseLoopTrigger : MonoBehaviour
{
    public StaircaseLoopManager manager;
    public bool isOpening;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root == manager.player.transform.root)
            manager.EnterEnd(isOpening);
    }
}
