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
        if (other.gameObject.layer == 11)
        {
            if (isAmmo && other.gameObject.GetComponent<PickUp>().pm.canPickUp)
            {
                other.gameObject.GetComponent<PickUp>().RefillAmmo();
                Destroy(gameObject);
            }
            if (isHealth && other.gameObject.GetComponent<PickUp>().pm.canPickUp)
            {
                other.gameObject.GetComponent<PickUp>().RegenHealth();
                Destroy(gameObject);
            }
        }
    }

    /*
    public IEnumerator HealthPickUpBool(GameObject other)
    {
        other.GetComponent<PlayerMovementAdvanced>().pickedUpHealth = true;

        yield return new WaitForSeconds(1f);

        other.GetComponent<PlayerMovementAdvanced>().pickedUpHealth = false;

        Destroy(gameObject);
    }

    */
}
