using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Photon.Pun;

public class Gun : MonoBehaviourPunCallbacks
{
    [Header("Equipped?")]
    public bool isEquipped;

    [Header("Melee stats")]
    public bool isMelee;
    public bool isThrowable;
    public float attackDelay;
    public float lastAttackTime;
    public int attackDamage;
    public float meleeDistance;
    public Animator meleeAnim;

    [Header("Gun stats")]
    public GameObject gunModel;
    public bool isAutomatic;
    public bool isShotgun;
    public float bulletsPerShot;
    public float shootDelay = 0.5f;
    public float lastShootTime;
    public float timeBetweenShots = .1f;
    public int shotDamage;
    public float flyingDieForce;
    public float dieForce;
    public int magSize;
    public int maxAmmo;
    public int maxAmmoSize;
    public int currentAmmo;
    public float adsZoom;

    [Header("Rocket Launcher stats")]
    public GameObject bullet;
    public float shootForce, upwardForce;
    public GameObject[] rocket;

    [Header("Dynamic Crosshair Values")]
    public float retSizePerShot;
    public float reticleDecreaseSpeed;
    public float restingSize;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem cartridgeEffect;
    public TrailRenderer bulletTrail;
    public Transform bulletSpawnPoint;
    public AudioSource shotAudioSource;
    public AudioSource gunActionsAudioSource;
    public AudioClip equipSound;
    public AudioClip shotSound;
    public AudioClip reloadSound;

    [Header("Recoil Values")]
    public float bodyRecoilForce;
    public float recoilX;
    public float recoilY;
    public float recoilZ;

    public static Gun instance;

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        currentAmmo = magSize;

        if (isAutomatic)
        {
            shootDelay = 0;
        }
    }

}