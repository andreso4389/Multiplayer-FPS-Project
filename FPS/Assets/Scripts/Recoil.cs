using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Recoil : MonoBehaviourPunCallbacks

{
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    /*
    [SerializeField] private float recoilX;
    [SerializeField] private float recoilY;
    [SerializeField] private float recoilZ;
    */

    [SerializeField] private float snappiness;
    [SerializeField] private float returnSpeed;

    public static Recoil instance;

    private void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        //recoil logic
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    [PunRPC]
    public void RecoilFire(float recoilX, float recoilY, float recoilZ)
    {
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }
}
