using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;

public class PowerUpsPickUp : MonoBehaviour
{
    public bool isAmmo;
    public bool isHealth;
    public float rotateX, rotateY, rotateZ;

    // Update is called once per frame
    void Update()
    {
        Vector3 newRotation = new Vector3(rotateX, rotateY, rotateZ);
        transform.Rotate(newRotation * Time.deltaTime * 10);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isAmmo)
            {
                other.gameObject.GetComponent<PlayerMovementAdvanced>().RefillAmmo();
            }
            if (isHealth)
            {
                other.gameObject.GetComponent<PlayerMovementAdvanced>().RegenHealth();
            }

            Destroy(gameObject);
        }
    }
}
