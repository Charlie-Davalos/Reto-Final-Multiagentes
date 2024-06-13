using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//controlador de robots
public class RobotController : MonoBehaviour
{
    public float speed = 2.0f;
    private Vector3 targetPosition;
    private bool moving = false;
    public int collectedTrash = 0;

    void Start()
    {
        moving = false;
    }

    void Update()
    {
        if (moving)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                moving = false;
            }
        }
    }

    public void MoveTo(Vector3 newPosition)
    {
        targetPosition = newPosition;
        moving = true;
    }

    public void UpdateTrashCount(int newCount)
    {
        collectedTrash = newCount;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Trash"))
        {
            TrashController trashController = other.GetComponent<TrashController>();
            if (trashController != null)
            {
                collectedTrash += trashController.CollectTrash(5 - collectedTrash);
                if (collectedTrash == 5)
                {
                    // Ir al trashcan
                    Debug.Log("Robot full, heading to trashcan.");
                }
            }
        }
    }
}
