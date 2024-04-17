using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;

public class ThrowController : MonoBehaviourPunCallbacks
{
    public float throwForce = 10f;
    public float upwardForce = 5f;
    public float pickUpDistance;
    public LayerMask Gun;
    public GameObject gunModel = null;
    public PlayerMovementAdvanced pm;
    public static ThrowController instance;

    void Awake()
    {
        instance = this;
        pm = GetComponent<PlayerMovementAdvanced>();

    }

    public void PickUp()
    {

    }
}
