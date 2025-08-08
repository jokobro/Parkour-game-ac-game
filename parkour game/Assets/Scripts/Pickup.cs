using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] private int id;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.PickupedObjects(id, this.gameObject);
            }
        }
    }
}
