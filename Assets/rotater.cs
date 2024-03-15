using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class rotater : NetworkBehaviour
{
    public Rigidbody rb;

    [SerializeField] private float torque;

    // Start is called before the first frame update
    void Start()
    { //if not host disable script
        
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsHost)
            rb.AddTorque(transform.up * (torque));
    }
}
