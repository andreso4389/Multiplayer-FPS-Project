using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.Rendering.HighDefinition;
using Unity.VisualScripting;
using Photon.Pun.Demo.Cockpit;
using System.Linq;
using TMPro;

public class PlayerMovementAdvanced : MonoBehaviourPunCallbacks
{
    [Header("Misc")]
    public Material[] allSkins;
    public bool killSelf = false;
    public GameObject playerOBJ;
    public AudioSource playerAudio;
    public Canvas ui;

    [Header("Ragdoll")]
    public GameObject root;

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchHeight;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;
    public Transform[] groundCheckPoints;

    [Header("Movement")]
    public ParticleSystem sprintEffect;
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;


    public Transform orientation;

    public float horizontalInput;
    public float verticalInput;

    Vector3 moveDirection;

    public Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        air
    }

    public bool sliding;
    public bool sprinting;
    public bool walking;

    [Header("Weapon Variables")]
    public GameObject bulletImpact;
    private float shotCounter;
    public float muzzleDisplayTime = 0.02f;

    [Header("Reticle Variables")]
    public RectTransform reticle;
    public float reticleCurrentSize;

    public GameObject hitMarker;
    public GameObject sniperScope;

    [Header("Gun list")]
    public Gun[] allGuns;
    public int selectedGun;
    public int gunSlot;
    public bool isScopedIn;
    public bool canShoot;
    public GameObject weaponHolder;
    public Animator gunAnim;
    public Animator reloadAnim;
    public Animator playerAnim;
    public GameObject playerHitImpact;

    [Header("Health variables")]
    public int maxHealth = 100;
    public int currentHealth;
    public float lastDMGTime;
    public float regenTimer;

    [Header("Camera variables")]
    public Camera cam;
    public GameObject camRecoil;
    public GameObject viewPoint;
    public Transform camPos;
    public GameObject slidingCamPos;
    public float camPosSpeed = 5f;
    public GameObject canvas;
    public float sensX;
    public float sensY;
    public float scopedSens;
    public float regularSens;
    public GameObject playerModel;

    public float cameraZTilt;
    public float cameraTiltMultiplier;
    public float cameraTiltSmoothTime;

    float xRotation;
    float yRotation;

    public ThrowController tc;
    public static PlayerMovementAdvanced instance;

    private void Awake()
    {
        instance = this;

        if (!photonView.IsMine)
        {
            cam.enabled = false;
            canvas.SetActive(false);
            sprintEffect.gameObject.SetActive(false);
            camRecoil.GetComponent<Recoil>().enabled = false;
        }

        tc = GetComponent<ThrowController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        regularSens = sensX;
        scopedSens = regularSens * .75f;

        canShoot = true;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        startYScale = transform.localScale.y;

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            gunSlot = 1;

            playerModel.SetActive(false);
            UIController.instance.Health.text = currentHealth.ToString();

            SetCorrespondingGun(gunSlot);
        }

        playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];

        // find active guns and set as equippedGun for each gunIcon index for a total of 3 equipped guns.
        for (int i = 0; i < UIController.instance.gunIcons.Length; i++)
        {
            for (int j = 0; j < UIController.instance.gunIcons[i].GetComponent<ImageHolderArray>().gunImage.Length; j++)
            {
                if (UIController.instance.gunIcons[i].GetComponent<ImageHolderArray>().gunImage[j].activeInHierarchy)
                {
                    UIController.instance.gunIcons[i].GetComponent<ImageHolderArray>().equippedGun = UIController.instance.gunIcons[i].GetComponent<ImageHolderArray>().gunImage[j];
                }
            }
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            if (killSelf)
            {
                lastPlayerPosition.position = playerOBJ.transform.position;
                PlayerSpawner.instance.Die(null, lastPlayerPosition);
            }

            // get mouse input
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 74f);

            // calcualte camera sway
            cameraZTilt = Mathf.Lerp(cameraZTilt, -horizontalInput * cameraTiltMultiplier, Time.deltaTime * cameraTiltSmoothTime);

            // ground check
            if (Physics.Raycast(groundCheckPoints[0].position, Vector3.down, .04f, whatIsGround) ||
                Physics.Raycast(groundCheckPoints[1].position, Vector3.down, .04f, whatIsGround) ||
                Physics.Raycast(groundCheckPoints[2].position, Vector3.down, .04f, whatIsGround) ||
                Physics.Raycast(groundCheckPoints[3].position, Vector3.down, .04f, whatIsGround) ||
                Physics.Raycast(groundCheckPoints[4].position, Vector3.down, .04f, whatIsGround))
                grounded = true;
            else
                grounded = false;

            MyInput();
            SpeedControl();
            StateHandler();

            // handle drag
            if (grounded)
                rb.drag = groundDrag;
            else
                rb.drag = 0;

            if (rb.velocity.magnitude >= sprintSpeed - 1)
            {
                if (!sprintEffect.isPlaying)
                {
                    ToggleSprintEffect(true);
                }
            }
            else
            {
                if (sprintEffect.isPlaying)
                {
                    ToggleSprintEffect(false);
                }
            }

            // scope in with sniper if not reloading
            if (Input.GetMouseButtonDown(1) && selectedGun == 2 && !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
            {
                SniperScopeIn();
            }
            if (Input.GetMouseButtonUp(1) || selectedGun != 2)
            {
                SniperScopeOut();
            }


            // if ammo != 0 and gun is not reloading, and gun is not pulling out, allow Shoot()
            if (allGuns[selectedGun].currentAmmo != 0 &&
                !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Pistol Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("AR Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Sniper Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Deagle Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Shotgun Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("RPG Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("M4 Pull Out") &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Pistol Melee"))
            {
                if (Input.GetMouseButtonDown(0) && allGuns[selectedGun].lastShootTime + allGuns[selectedGun].shootDelay < Time.time && canShoot && allGuns[selectedGun].isEquipped)
                {
                    if (allGuns[selectedGun].isShotgun) //Shotgun
                    {
                        photonView.RPC("ShotgunShoot", RpcTarget.All);
                    }
                    else if (selectedGun == 5) //RPG
                    {
                        photonView.RPC("ProjectileShoot", RpcTarget.All);
                    }
                    else //All other weapons
                    {
                        photonView.RPC("Shoot", RpcTarget.All);
                    }
                    camRecoil.GetPhotonView().RPC("RecoilFire", RpcTarget.All, allGuns[selectedGun].recoilX, allGuns[selectedGun].recoilY, allGuns[selectedGun].recoilZ);
                    gunAnim.SetTrigger("Shoot");
                    gunAnim.SetInteger("Gun", selectedGun);
                }

                // Automatic shooting on hold mouse button 0
                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic && canShoot && allGuns[selectedGun].isEquipped)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        photonView.RPC("Shoot", RpcTarget.All);
                        camRecoil.GetPhotonView().RPC("RecoilFire", RpcTarget.All, allGuns[selectedGun].recoilX, allGuns[selectedGun].recoilY, allGuns[selectedGun].recoilZ);
                        gunAnim.SetTrigger("Shoot");
                    }
                }
            }

            // melee attack with gun
            if (Input.GetKeyDown(KeyCode.V) && !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
            {
                GunMelee();
            }

            // reload on key 'R'
            if (Input.GetKey(KeyCode.R) &&
                allGuns[selectedGun].currentAmmo < allGuns[selectedGun].magSize && allGuns[selectedGun].maxAmmo > 0 &&
                !gunAnim.GetCurrentAnimatorStateInfo(0).IsName("Pistol Melee"))
            {
                Reload();
            }

            // Throw weapon on Key 'Q'
            if (Input.GetKeyDown(KeyCode.Q) && allGuns[selectedGun].isEquipped)
            {
                photonView.RPC("Throw", RpcTarget.All, selectedGun);

                // Unequip weapon thrown, and switch to an available weapon.
                allGuns[selectedGun].isEquipped = false;

                // Deactivate corresponding gun image
                UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage[selectedGun].SetActive(false);

                // NEW!!! switch weapons to very next weapon or first weapon in stack
                if (gunSlot == 3)
                {
                    gunSlot = 1;
                    SetCorrespondingGun(gunSlot);
                }
                else
                {
                    gunSlot++;
                    SetCorrespondingGun(gunSlot);
                }
            }

            // Pick up weapon on Key 'E'
            PickUpInput();

            // display current ammo
            UIController.instance.Ammo.text = allGuns[selectedGun].currentAmmo.ToString() + " | " + allGuns[selectedGun].maxAmmo.ToString();

            // display current health
            UIController.instance.Health.text = currentHealth.ToString();
            UIController.instance.bloodOverlay.color = new Color(UIController.instance.bloodOverlay.color.r,
                                                                 UIController.instance.bloodOverlay.color.g,
                                                                 UIController.instance.bloodOverlay.color.b,
                                                                 -(currentHealth - 100) * .01f);

            // Regen health
            if (currentHealth < maxHealth)
            {
                if (lastDMGTime + regenTimer < Time.time)
                {
                    RegenHealth();
                }
            }

            //  NEW   Switch weapons based on key pressed  
            if (Input.GetKeyDown("1") && gunSlot != 1 && !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
            {

                gunSlot = 1;

                SetCorrespondingGun(gunSlot);

            }
            if (Input.GetKeyDown("2") && gunSlot != 2 && !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
            {
                gunSlot = 2;

                SetCorrespondingGun(gunSlot);
            }
            if (Input.GetKeyDown("3") && gunSlot != 3 && !reloadAnim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
            {
                gunSlot = 3;

                SetCorrespondingGun(gunSlot);
            }


            // return reticle to resting size constantly.
            if (reticleCurrentSize != allGuns[selectedGun].restingSize)
            {
                reticleCurrentSize = Mathf.Lerp(reticleCurrentSize, allGuns[selectedGun].restingSize, Time.deltaTime * allGuns[selectedGun].reticleDecreaseSpeed);
            }
            reticle.sizeDelta = new Vector2(reticleCurrentSize, reticleCurrentSize);


            playerAnim.SetBool("grounded", grounded);
            playerAnim.SetFloat("speed", rb.velocity.magnitude);


            // free up cursor on escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
            MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey) && readyToJump && (grounded || isOnSlope))
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Mode - Sliding
        if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;

            else
                desiredMoveSpeed = sprintSpeed;
        }

        // Mode - Crouching
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        // Mode - Sprinting
        else if (grounded && Input.GetKey(sprintKey))
        {
            sprinting = true;
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        else if (Input.GetKeyUp(sprintKey))
        {
            sprinting = false;

        }

        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

        // Mode - Air
        else
        {
            state = MovementState.air;
        }

        // check if desiredMoveSpeed has changed drastically
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            {
                time += Time.deltaTime * speedIncreaseMultiplier;
            }
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(20f * moveSpeed * GetSlopeMoveDirection(moveDirection), ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // on ground
        else if (grounded)
            rb.AddForce(10f * moveSpeed * moveDirection.normalized, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(10f * airMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Force);

        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }


    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }


    private void Jump()
    {
        exitingSlope = true;

        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }


    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool isOnSlope;

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            isOnSlope = true;
            return angle < maxSlopeAngle && angle != 0;
        }
        else
        {
            isOnSlope = false;
        }

        return false;
    }


    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void ToggleSprintEffect(bool isActive)
    {
        if (isActive)
        {
            sprintEffect.Play();
        }
        else
        {
            sprintEffect.Stop();
        }
    }


    [PunRPC]
    private void Shoot()
    {
        bool isMeleeHit = false;

        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        ray.origin = viewPoint.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            TrailRenderer trail = Instantiate(allGuns[selectedGun].bulletTrail, allGuns[selectedGun].bulletSpawnPoint.position, Quaternion.identity);

            StartCoroutine(SpawnTrail(trail, hit));

            // Create bullet impact effect
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                int idNumber = photonView.ViewID;

                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.LookRotation(hit.normal, Vector3.up));
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage",
                                                             RpcTarget.Others,
                                                             photonView.Owner.NickName,
                                                             allGuns[selectedGun].shotDamage,
                                                             PhotonNetwork.LocalPlayer.ActorNumber,
                                                             ray.direction,
                                                             allGuns[selectedGun].dieForce,
                                                             allGuns[selectedGun].flyingDieForce,
                                                             idNumber,
                                                             isMeleeHit);

                StartCoroutine(ShowDMGIndicator(hit.collider.gameObject, allGuns[selectedGun].shotDamage));
                StartCoroutine(TempHitMarker(.07f));

            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }
        shotCounter = allGuns[selectedGun].timeBetweenShots;

        // Increase reticle size on shot
        reticleCurrentSize += allGuns[selectedGun].retSizePerShot;

        allGuns[selectedGun].currentAmmo -= 1;
        if (allGuns[selectedGun].currentAmmo <= 0)
        {
            allGuns[selectedGun].currentAmmo = 0;
        }

        allGuns[selectedGun].muzzleFlash.Play();

        if (allGuns[selectedGun].cartridgeEffect != null)
        {
            allGuns[selectedGun].cartridgeEffect.Play();
        }
        allGuns[selectedGun].lastShootTime = Time.time;

        allGuns[selectedGun].shotAudioSource.Play();
    }

    [PunRPC]
    private void ShotgunShoot()
    {
        bool isMeleeHit = false;
        Ray[] rays = new Ray[7];

        rays[0] = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0)); // middle ray
        rays[1] = cam.ViewportPointToRay(new Vector3(.5f, .52f, 0)); // top mid ray
        rays[2] = cam.ViewportPointToRay(new Vector3(.5f, .48f, 0)); // bottom mid ray
        rays[3] = cam.ViewportPointToRay(new Vector3(.515f, .51f, 0)); // top right
        rays[4] = cam.ViewportPointToRay(new Vector3(.515f, .49f, 0)); // bottom right
        rays[5] = cam.ViewportPointToRay(new Vector3(.485f, .51f, 0)); //top left
        rays[6] = cam.ViewportPointToRay(new Vector3(.485f, .49f, 0)); //bottom left

        for (int i = 0; i < rays.Length; i++)
        {
            if (Physics.Raycast(rays[i], out RaycastHit hit))
            {
                TrailRenderer trail = Instantiate(allGuns[selectedGun].bulletTrail, allGuns[selectedGun].bulletSpawnPoint.position, Quaternion.identity);

                StartCoroutine(SpawnTrail(trail, hit));

                // Create bullet impact effect
                if (hit.collider.gameObject.CompareTag("Player"))
                {
                    int idNumber = photonView.ViewID;

                    PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.LookRotation(hit.normal, Vector3.up));
                    hit.collider.gameObject.GetPhotonView().RPC("DealDamage",
                                                                 RpcTarget.Others,
                                                                 photonView.Owner.NickName,
                                                                 allGuns[selectedGun].shotDamage,
                                                                 PhotonNetwork.LocalPlayer.ActorNumber,
                                                                 rays[i].direction,
                                                                 allGuns[selectedGun].dieForce,
                                                                 allGuns[selectedGun].flyingDieForce,
                                                                 idNumber,
                                                                 isMeleeHit);

                    StartCoroutine(ShowDMGIndicator(hit.collider.gameObject, allGuns[selectedGun].shotDamage));
                    StartCoroutine(TempHitMarker(.08f));
                }
                else
                {
                    GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                    Destroy(bulletImpactObject, 10f);
                }
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        // Increase reticle size on shot
        reticleCurrentSize += allGuns[selectedGun].retSizePerShot;

        allGuns[selectedGun].currentAmmo -= 1;
        if (allGuns[selectedGun].currentAmmo <= 0)
        {
            allGuns[selectedGun].currentAmmo = 0;
        }

        allGuns[selectedGun].muzzleFlash.Play();
        allGuns[selectedGun].lastShootTime = Time.time;

        allGuns[selectedGun].shotAudioSource.Play();
    }

    [PunRPC]
    private void ProjectileShoot()
    {
        // Find exact hit position using raycast
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        RaycastHit hit;

        // Check if ray hits something
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(75); // Just a point far away from the player
        }

        // Calculate direction from bulletSpawnPoint to targetPoint
        Vector3 direction = targetPoint - allGuns[selectedGun].bulletSpawnPoint.position;

        // Make rocket on RPG disappear
        for (int i = 0; i < allGuns[selectedGun].rocket.Length; i++)
        {
            allGuns[selectedGun].rocket[i].SetActive(false);
        }

        // Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(allGuns[selectedGun].bullet, allGuns[selectedGun].bulletSpawnPoint.position, Quaternion.identity);

        // Rotate bullet towards shoot direction
        currentBullet.transform.forward = direction.normalized;
        currentBullet.GetComponent<Rigidbody>().AddForce(direction.normalized * allGuns[selectedGun].shootForce, ForceMode.Impulse);

        rb.AddForce(-direction.normalized * allGuns[selectedGun].bodyRecoilForce, ForceMode.Impulse);

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        // Increase reticle size on shot
        reticleCurrentSize += allGuns[selectedGun].retSizePerShot;

        allGuns[selectedGun].currentAmmo -= 1;
        if (allGuns[selectedGun].currentAmmo <= 0)
        {
            allGuns[selectedGun].currentAmmo = 0;
        }

        allGuns[selectedGun].muzzleFlash.Play();
        allGuns[selectedGun].lastShootTime = Time.time;

        allGuns[selectedGun].shotAudioSource.Play();
    }

    private void GunMelee()
    {
        gunAnim.SetTrigger("Melee");
        bool isMeleeHit = true;
        Debug.DrawRay(cam.transform.position, cam.transform.forward * allGuns[selectedGun].meleeDistance, Color.green);

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, allGuns[selectedGun].meleeDistance))
        {

            if (hit.collider.gameObject.CompareTag("Player"))
            {
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage",
                                                             RpcTarget.All,
                                                             photonView.Owner.NickName,
                                                             allGuns[selectedGun].attackDamage,
                                                             PhotonNetwork.LocalPlayer.ActorNumber,
                                                             cam.transform.position,
                                                             allGuns[selectedGun].dieForce,
                                                             allGuns[selectedGun].flyingDieForce,
                                                             isMeleeHit);
            }
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < .5f)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }
        trail.transform.position = hit.point;

        Destroy(trail.gameObject, trail.time);
    }

    private void Reload()
    {
        if (isScopedIn)
        {
            SniperScopeOut();
        }

        int difference = allGuns[selectedGun].magSize - allGuns[selectedGun].currentAmmo; // Calculate the diff between mag size and current ammo
        if (allGuns[selectedGun].maxAmmo <= difference)
        {
            allGuns[selectedGun].currentAmmo += allGuns[selectedGun].maxAmmo;
            allGuns[selectedGun].maxAmmo = 0;
        }
        else
        {
            allGuns[selectedGun].maxAmmo -= difference; // Subtract difference from maxAmmo, then add it to currentAmmo
            allGuns[selectedGun].currentAmmo += difference;
        }

        if (allGuns[selectedGun].rocket != null)
        {
            for (int i = 0; i < allGuns[selectedGun].rocket.Length; i++)
            {
                allGuns[selectedGun].rocket[i].SetActive(true);
            }
        }

        if (allGuns[selectedGun].gunActionsAudioSource.clip != allGuns[selectedGun].reloadSound)
            allGuns[selectedGun].gunActionsAudioSource.clip = allGuns[selectedGun].reloadSound;
        //allGuns[selectedGun].audioSource.Stop();
        allGuns[selectedGun].gunActionsAudioSource.Play();
        reloadAnim.SetTrigger("Reload");
    }

    [PunRPC]
    public void Throw(int gunToThrow)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        RaycastHit hit;

        // Check if ray hits something
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(75); // Just a point far away from the player

        // Calculate direction from bulletSpawnPoint to targetPoint
        Vector3 direction = targetPoint - allGuns[gunToThrow].gunModel.transform.position;

        GameObject gun = Instantiate(allGuns[gunToThrow].gunModel, allGuns[gunToThrow].gunModel.transform.position, Quaternion.identity);

        gun.transform.forward = direction.normalized;

        // Set up rigidbody to add force
        gun.AddComponent<Rigidbody>();
        gun.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
        gun.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Add collider to keep gun from falling through the ground
        gun.AddComponent<BoxCollider>();

        // Enable outline
        gun.GetComponent<Outline>().enabled = true;

        // Throw object forwards an upwards
        gun.GetComponent<Rigidbody>().AddForce(direction.normalized * tc.throwForce, ForceMode.Impulse);
        gun.GetComponent<Rigidbody>().AddForce(cam.transform.up * tc.upwardForce, ForceMode.Impulse);

        allGuns[gunToThrow].gameObject.SetActive(false);
    }

    public void PickUpInput()
    {
        // if looking at pick up object and pick up object !isEquipped in my allGuns[], display pickUpIndicator and allow for PickUp()
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hitDetect, tc.pickUpDistance, tc.Gun))
        {
            GameObject pickUpObject = hitDetect.collider.gameObject;

            bool pickUpObjectEquipped = true;
            int j = 0;
            for (int i = 0; i < allGuns.Length; i++)
            {
                if (pickUpObject.name.Contains(allGuns[i].name) && !allGuns[i].isEquipped)
                {
                    j = i;
                    pickUpObjectEquipped = false;
                }
                else if (pickUpObject.CompareTag("Pick Up Spawner") && pickUpObject.GetComponent<PickUpController>().gun.gameObject.name.Contains(allGuns[i].name) && !allGuns[i].isEquipped)
                {
                    j = i;
                    pickUpObjectEquipped = false;
                }
            }
            if (!pickUpObjectEquipped)
            {
                UIController.instance.pickUpIndicator.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    //throw out any equipped weapon to replace for newly equipped weapon
                    for (int i = 0; i < UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage.Length; i++)
                    {
                        if (UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage[i].activeInHierarchy)
                        {
                            UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage[i].SetActive(false);
                            photonView.RPC("Throw", RpcTarget.All, i);
                            allGuns[i].isEquipped = false;
                        }
                    }

                    allGuns[j].isEquipped = true;
                    UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage[j].SetActive(true);

                    SetCorrespondingGun(gunSlot);

                    Destroy(pickUpObject);
                }
            }
            else
            {
                UIController.instance.pickUpIndicator.SetActive(false);
            }
        }
        else
        {
            if (UIController.instance.pickUpIndicator.activeInHierarchy)
                UIController.instance.pickUpIndicator.SetActive(false);
        }
    }

    private void SniperScopeIn()
    {
        isScopedIn = true;
        cam.fieldOfView = 30;
        sensX = scopedSens;
        sensY = scopedSens;

        sniperScope.SetActive(true);
        reticle.gameObject.SetActive(false);
    }

    private void SniperScopeOut()
    {
        isScopedIn = false;
        cam.fieldOfView = 90;
        sensX = regularSens;
        sensY = regularSens;

        sniperScope.SetActive(false);
        reticle.gameObject.SetActive(true);
    }

    private IEnumerator TempHitMarker(float duration)
    {
        if (photonView.IsMine)
        {
            playerAudio.Play();
            hitMarker.SetActive(true);

            Vector2 originalSize = new Vector2(95, 95);
            Vector2 newSize = new Vector2(135, 135);

            float time = 0f;

            while (time < .09f)
            {
                hitMarker.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(originalSize, newSize, 10f * time);

                time += Time.deltaTime;

                yield return null;
            }

            hitMarker.SetActive(false);
            hitMarker.GetComponent<RectTransform>().sizeDelta = originalSize;
        }
    }

    [PunRPC]
    public void WeaponHolderOn(bool isActive)
    {
        weaponHolder.SetActive(isActive);
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor, Vector3 direction, float dieForce, float flyingDieForce, int idNumber, bool isMeleeHit)
    {
        TakeDamage(damager, damageAmount, actor, direction, dieForce, flyingDieForce, idNumber, isMeleeHit);
    }

    public Transform lastPlayerPosition;
    public void TakeDamage(string damager, int damageAmount, int actor, Vector3 direction, float dieForce, float flyingDieForce, int idNumber, bool isMeleeHit)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damageAmount;
            lastDMGTime = Time.time;

            if (isMeleeHit && currentHealth > 0)
                root.GetPhotonView().RPC("ApplyForce", RpcTarget.All, direction * dieForce / 2);

            // if health reaches 0, ragdoll, explode in blood, and respawn
            if (currentHealth <= 0)
            {
                PhotonNetwork.GetPhotonView(idNumber).RPC("ShowKillIndicator", RpcTarget.Others); // Call ShowKillIndicator() on whoever killed me 

                direction.y = flyingDieForce; // add upward force on ragdoll

                for (int i = 2; i < allGuns.Length; i++)
                {
                    if (allGuns[i].isEquipped)
                    {
                        photonView.RPC("Throw", RpcTarget.All, i);
                    }
                }

                playerModel.SetActive(true);

                root.GetPhotonView().RPC("ActivateRagdoll", RpcTarget.All);
                root.GetPhotonView().RPC("ApplyForce", RpcTarget.All, direction * dieForce);

                photonView.RPC("WeaponHolderOn", RpcTarget.All, false); //disable weapons on player death: 
                canShoot = false;

                currentHealth = 1000; // avoid players "dying" multiple times on the same death

                lastPlayerPosition.position = playerOBJ.transform.position; //store position for death particle effect

                PlayerSpawner.instance.Die(damager, lastPlayerPosition);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1); //update leaderboard
            }

            UIController.instance.Health.text = currentHealth.ToString();
        }
    }

    public void TakeExplosionDamage(string damager, int damageAmount, int actor, float explosionForce)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damageAmount;

            if (currentHealth <= 0)
            {
                root.GetPhotonView().RPC("ActivateRagdoll", RpcTarget.All);

                photonView.RPC("WeaponHolderOn", RpcTarget.All, false);
                canShoot = false;

                currentHealth = 1000;

                lastPlayerPosition.position = playerOBJ.transform.position;

                PlayerSpawner.instance.Die(damager, lastPlayerPosition);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }

            UIController.instance.Health.text = currentHealth.ToString(); ;
        }
    }

    public GameObject camRotation;
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (MatchManager.instance.state == MatchManager.GameState.Playing)
                MatchManager.instance.mapCamera.SetActive(false);
            else
                MatchManager.instance.mapCamera.SetActive(true);

            // rotate camera according to mouse input and add camera tilt
            camRotation.transform.rotation = Quaternion.Euler(xRotation, yRotation, cameraZTilt);
            orientation.rotation = Quaternion.Euler(0, yRotation, 0);

            if (sliding)
                cam.transform.position = Vector3.Lerp(cam.transform.position, slidingCamPos.transform.position, camPosSpeed * Time.deltaTime);
            else
                cam.transform.position = Vector3.Lerp(cam.transform.position, camRotation.transform.position, camPosSpeed * Time.deltaTime);

            rb.MoveRotation(Quaternion.Euler(0, yRotation, 0));
        }
    }

    void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        if (photonView.IsMine)
            tc.gunModel = allGuns[selectedGun].gunModel;

        gunAnim.SetTrigger("Pull out");
        gunAnim.SetInteger("Gun", selectedGun);

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].gunActionsAudioSource.clip = allGuns[selectedGun].equipSound;
        allGuns[selectedGun].gunActionsAudioSource.Play();

        allGuns[selectedGun].muzzleFlash.Stop();
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if (gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

    public void SetSensitivity()
    {
        sensX = UIController.instance.sensSlider.value * 100;
        sensY = UIController.instance.sensSlider.value * 100;

        regularSens = sensX;
        scopedSens = regularSens * .75f;

        LBUIController.instance.savedSens = sensX;
        LBUIController.instance.sensIsSaved = true;
    }

    [PunRPC]
    public IEnumerator ShowKillIndicator()
    {
        if (photonView.IsMine)
        {
            UIController.instance.killIndicator.SetActive(true);

            var tempColor = UIController.instance.killIcon.color; // store killIcon.color values

            float time = 0f;

            yield return new WaitForSeconds(.35f);

            while (time < 1f)
            {
                tempColor.a = Mathf.Lerp(1, 0, 2.5f * time); // change opacity of stored color values to a lerp value from 1 to 0

                UIController.instance.killIcon.color = tempColor; // change killIcon color to tempColor lerp value

                time += Time.deltaTime / 2;

                yield return null;
            }

            UIController.instance.killIndicator.SetActive(false);
        }
    }

    public IEnumerator ShowDMGIndicator(GameObject other, int damage)
    {
        if (photonView.IsMine)
        {
            GameObject dmgIndicator = Instantiate(UIController.instance.dmgIndicatorGO, ui.transform);
            dmgIndicator.SetActive(false);

            dmgIndicator.GetComponentInChildren<Animator>().Play("dmgIndicator", 0);

            dmgIndicator.GetComponent<TrackUI>().playerCamera = cam;
            dmgIndicator.GetComponent<TrackUI>().Subject = other.transform;
            dmgIndicator.GetComponent<TrackUI>().mCanvas = ui.GetComponent<RectTransform>();

            dmgIndicator.GetComponentInChildren<TextMeshProUGUI>().text = damage.ToString();

            float time = 0f;

            while (time < 1f)
            {
                dmgIndicator.SetActive(true);
                dmgIndicator.GetComponentInChildren<TextMeshProUGUI>().alpha = Mathf.Lerp(1, 0, time);

                time += Time.deltaTime;

                yield return null;
            }

            Destroy(dmgIndicator);
        }
    }
    private void RegenHealth()
    {
        currentHealth += 1;

        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
            return;
        }
    }

    private void SetCorrespondingGun(int gunSlot)
    {
        UIController.instance.gunIcons[0].GetComponent<Image>().enabled = false;
        UIController.instance.gunIcons[1].GetComponent<Image>().enabled = false;
        UIController.instance.gunIcons[2].GetComponent<Image>().enabled = false;

        for (int i = 0; i < UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage.Length; i++)
        {
            if (UIController.instance.gunIcons[gunSlot - 1].GetComponent<ImageHolderArray>().gunImage[i].activeInHierarchy)
            {
                selectedGun = i;
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                break;
            }
        }
        UIController.instance.gunIcons[gunSlot - 1].GetComponent<Image>().enabled = true;

    }


}