using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CustomBullet : MonoBehaviourPunCallbacks
{
    public Rigidbody rb;
    public GameObject explosion;
    public LayerMask whatIsEnemies;
    public int idNumber;
    public string damager;
    public int actor;

    [Header("Stats")]
    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    [Header("Damage")]
    public int explosionDamage;
    public float explosionRange;
    public float explosionForce;

    [Header("Lifetime")]
    public int maxCollisions;
    public float maxLifeTime;
    public bool explodeOnTouch = true;

    PhysicMaterial physics_mat;
    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }

    private void Update()
    {
        // Countdown to lifetime
        maxLifeTime -= Time.deltaTime;
        if (maxLifeTime <= 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        //if (explosion != null)
        GameObject explosionEffect = Instantiate(explosion, transform.position, Quaternion.identity);

        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, whatIsEnemies);
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].GetComponent<Rigidbody>().AddExplosionForce(explosionForce, transform.position, explosionRange);

            Debug.Log("Colliders hit: " + enemies[i].gameObject.name);

            if (enemies[i].GetComponent<PlayerMovementAdvanced>())
            {
                enemies[i].GetComponent<PlayerMovementAdvanced>().TakeExplosionDamage(damager, explosionDamage, actor, explosionForce, transform.position, explosionRange, idNumber);
            }
            else if (enemies[i].GetComponent<Damage>())
            {
                enemies[i].GetComponent<Damage>().pm.TakeExplosionDamage(damager, explosionDamage, actor, explosionForce, transform.position, explosionRange, idNumber);
            }

        }

        Destroy(explosionEffect, 2f);
        Invoke("Delay", .05f);
    }

    private void Delay()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Setup()
    {
        // Create a new physic material
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;

        //Assign material to collider
        GetComponent<CapsuleCollider>().material = physics_mat;

        rb.useGravity = useGravity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
