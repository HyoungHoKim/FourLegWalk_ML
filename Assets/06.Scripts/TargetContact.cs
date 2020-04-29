using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetContact : MonoBehaviour
{
    public bool touchingTarget;

    private void OnCollisionEnter(Collision coll)
    {
        if (coll.transform.CompareTag("target"))
        {
            touchingTarget = true;
        }
    }

    private void OnCollisionExit(Collision coll)
    {
        if (coll.transform.CompareTag("target"))
        {
            touchingTarget = false;
        }
    }
}
