using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashController : MonoBehaviour
{
    public int amount = 1;

    public int CollectTrash(int maxCollect)
    {
        int collected = Mathf.Min(amount, maxCollect);
        amount -= collected;
        if (amount <= 0)
        {
            Destroy(gameObject);
        }
        return collected;
    }
}
