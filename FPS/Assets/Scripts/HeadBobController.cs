using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HeadBobController : MonoBehaviour
{
    [Range(0.001f, 0.01f)]
    public float amount = 0.01f;

    [Range(1f, 30f)]
    public float frequency = 15.0f;

    [Range(10f, 100f)]
    public float smooth = 100f;
    public float returnSpeed = 1;

    public Vector3 originalPosition;

    public PlayerMovementAdvanced pm;

    public static HeadBobController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        CheckForHeadBobTrigger();
        if (pm.rb.velocity.magnitude >= pm.sprintSpeed - 1)
            frequency = 25f;
        else
            frequency = 15f;
    }

    private void CheckForHeadBobTrigger()
    {
        float inputMagnitude = new Vector3(pm.horizontalInput, 0, pm.verticalInput).magnitude;

        if (inputMagnitude > 0 && pm.grounded)
            StartHeadBob();
        else
            StopHeadBob();
    }

    private Vector3 StartHeadBob()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * frequency) * amount * 1.4f, smooth * Time.deltaTime);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * frequency / 2f) * amount * 1.6f, smooth * Time.deltaTime);
        transform.localPosition += pos;

        return pos;
    }

    private void StopHeadBob()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, returnSpeed * Time.deltaTime);
    }
}
