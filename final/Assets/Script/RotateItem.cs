using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateItem : MonoBehaviour
{
    Transform item;
    float rotateSpeed = 80.0f;

    // Start is called before the first frame update
    void Start()
    {
        item = GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        item.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
}
