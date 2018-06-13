﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AscendingObjectCollider : MonoBehaviour 
{
    private void OnTriggerEnter(Collider other) 
    {
        Vector3 colliding_point = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);

        if (colliding_point.z > 1.99f)
            transform.parent.GetComponent<AscendingObject>().AddObject(other);
    }

    private void OnTriggerExit(Collider other) 
    {
        transform.parent.GetComponent<AscendingObject>().RemoveObject(other);
    }
}
