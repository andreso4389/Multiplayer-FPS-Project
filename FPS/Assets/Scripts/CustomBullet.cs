using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CustomBullet : MonoBehaviourPunCallbacks
{
    public Rigidbody rb;
    public GameObject explosion;
    public bool isExplosive;
    public LayerMask whatIsEnemies;

    [Header("Passed variables")]
    public int idNumber;
    public string damager;
    public int actor;
    public Vector3 direction;
    public float dieForce;
    public float flyingDieForce;

    [Header("Stats")]
    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    [Header("Damage")]
    public int damage;
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
            if (isExplosive)
                Explode();
        }
        if (!isExplosive && rb.velocity.magnitude > 5f)
        {
            transform.Rotate(10, 0, 0);
            gameObject.GetComponentInChildren<Outline>().enabled = false;
        }
        else if (!isExplosive && rb.velocity.magnitude < 1f)
        {
            gameObject.GetComponentInChildren<Outline>().enabled = true;
            gameObject.layer = 10;
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

            if (enemies[i].GetComponent<PlayerMovementAdvanced>())
            {
                enemies[i].GetComponent<PlayerMovementAdvanced>().TakeExplosionDamage(damager, damage, actor, explosionForce, transform.position, explosionRange, idNumber);
            }
            else if (enemies[i].GetComponent<Damage>())
            {
                enemies[i].GetComponent<Damage>().pm.TakeExplosionDamage(damager, damage, actor, explosionForce, transform.position, explosionRange, idNumber);
            }

        }

        Destroy(explosionEffect, 2f);
        Invoke("Delay", .05f);
    }

    private void Delay()
    {
        Destroy(gameObject);
    }

    private void Damage(Collision collision)
    {
        if (collision.gameObject.GetComponent<Damage>())
        {
            collision.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.Others, damager,
                                                                                     damage,
                                                                                     actor,
                                                                                     direction,
                                                                                     dieForce,
                                                                                     flyingDieForce,
                                                                                     idNumber,
                                                                                     false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isExplosive)
        {
            Explode();
        }
        else
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Damage(collision);
            }
        }
    }

    private void Setup()
    {
        // Create a new physic material
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;

        //Assign material to collider
        if (GetComponent<CapsuleCollider>())
        {
            GetComponent<CapsuleCollider>().material = physics_mat;
        }
        else if (GetComponent<BoxCollider>())
        {
            GetComponent<BoxCollider>().material = physics_mat;
        }

        rb.useGravity = useGravity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
