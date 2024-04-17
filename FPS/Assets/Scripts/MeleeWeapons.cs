using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapons : MonoBehaviour
{
    [Header("Weapon Stats")]
    public bool isThrowable;
    public float attackDelay;
    public float lastAttackTime;
    public int attackDamage;
    public Animator meleeAnim;


    public static MeleeWeapons instance;

    private void Awake()
    {
        instance = this;
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
