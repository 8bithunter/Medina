using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinOnTouch : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the other object has the same script
        JoinOnTouch other = collision.gameObject.GetComponent<JoinOnTouch>();
        if (other != null)
        {
            // Combine: Make the other object a child of this one
            collision.transform.SetParent(transform);

            // Optionally, snap their positions together
            collision.transform.position = transform.position;
        }
    }
}
