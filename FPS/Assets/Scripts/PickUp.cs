using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    public PlayerMovementAdvanced pm;

    public void RefillAmmo()
    {
        pm.RefillAmmo();
    }

    public void RegenHealth()
    {
        StartCoroutine(HealthPickUpBool());
    }

    public IEnumerator HealthPickUpBool()
    {
        pm.pickedUpHealth = true;

        yield return new WaitForSeconds(1f);

        pm.pickedUpHealth = false;
    }
}
