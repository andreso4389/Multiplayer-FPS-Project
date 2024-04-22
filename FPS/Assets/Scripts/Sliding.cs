using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private CapsuleCollider capCollider;
    private PlayerMovementAdvanced pm;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideDelay;
    public float lastSlideTime;
    public float slideForce;
    private float slideTimer;
    public float colliderNormalHeight;


    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovementAdvanced>();
        capCollider = GetComponent<CapsuleCollider>();

        colliderNormalHeight = capCollider.height;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && pm.sprinting && (pm.grounded || pm.isOnSlope) && lastSlideTime + slideDelay < Time.time)
        {
            StartSlide();
        }
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }

    private void StartSlide()
    {
        pm.sliding = true;
        pm.playerAnim.SetBool("sliding", true);

        capCollider.height = colliderNormalHeight * .6f;
        capCollider.center =new Vector3 (0, 1.24f, 0.3252003f);

        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
        lastSlideTime = Time.time;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // sliding normal
        if (!pm.OnSlope())
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
                StopSlide();
        }
        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }
    }

    private void StopSlide()
    {
        pm.sliding = false;
        pm.playerAnim.SetBool("sliding", false);

        capCollider.height = colliderNormalHeight;
        capCollider.center =new Vector3 (0, 1.916982f, 0.3252003f);
    }
}
