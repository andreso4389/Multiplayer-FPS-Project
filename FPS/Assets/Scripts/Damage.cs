using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Damage : MonoBehaviour
{
    public float damageMultiplier;
    public PlayerMovementAdvanced pm;

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor, Vector3 direction, float dieForce, float flyingDieForce, int idNumber, bool isMeleeHit)
    {
        float newDamage = Mathf.Round(damageAmount) * damageMultiplier;

        Debug.Log("New damage as int = " + (int)newDamage + " New Damage as float = " + newDamage);
        
        pm.TakeDamage(damager, (int)newDamage, actor, direction, dieForce, flyingDieForce, idNumber, isMeleeHit);
    }
}
