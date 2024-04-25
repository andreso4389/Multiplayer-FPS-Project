using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Photon.Pun;
using UnityEngine;

public class Ragdoll : MonoBehaviourPunCallbacks
{
    public Rigidbody[] rigidbodies;
    public Animator animator;
    public Rigidbody forcedBody;

    public static Ragdoll instance;

    private void Awake()
    {
        instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();

        DeactivateRagdoll();
    }

    [PunRPC]
    public void DeactivateRagdoll()
    {
        foreach (var rigidBody in rigidbodies)
        {
            rigidBody.isKinematic = true;
        }
        animator.enabled = true;
    }

    [PunRPC]
    public void ActivateRagdoll()
    {
        foreach (var rigidBody in rigidbodies)
        {
            rigidBody.isKinematic = false;
        }
        animator.enabled = false;
    }

    [PunRPC]
    public void ApplyForce(Vector3 force)
    {
        forcedBody.AddForce(force, ForceMode.VelocityChange);
    }

    [PunRPC]
    public void ApplyExplosionForce(float force, Vector3 rocketPosition, float explosionRange)
    {
        forcedBody.AddExplosionForce(force, rocketPosition, explosionRange);
    }

}
