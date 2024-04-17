using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UIElements;

public class PickUpController : MonoBehaviourPunCallbacks
{
    public BoxCollider coll;
    public GameObject gun;
    public GameObject[] gunsList;
    public Transform gunHolder;
    public float rotateX, rotateY, rotateZ;
    public GameObject pickUpEffect;

    private int selected;
    void Start()
    {
        // Randomly choose which gun to use as a pick up weapon
        //selected = Random.Range(0, gunsList.Length);
        //gunsList[selected].SetActive(true);

        //gun = gunsList[selected];
        selected = Random.Range(0, gunsList.Length);

        photonView.RPC("SetPickUpWeapon", RpcTarget.All, selected);
    }

    void Update()
    {
        Vector3 newRotation = new Vector3(rotateX, rotateY, rotateZ);
        gunHolder.transform.Rotate(newRotation * Time.deltaTime * 10);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckGun(other);
    }

    private void CheckGun(Collider other)
    {
        if (other.gameObject.tag == "Player") // Check if collision is from a player
        {
            for (int i = 0; i < other.gameObject.GetComponent<PlayerMovementAdvanced>().allGuns.Length; i++) // Iterate through players guns array allGuns[]
            {
                // if allGuns[i] == this gun and allGuns[i] is not equipped, equip.
                if (other.gameObject.GetComponent<PlayerMovementAdvanced>().allGuns[i].gameObject.name == gun.name &&
                    !other.gameObject.GetComponent<PlayerMovementAdvanced>().allGuns[i].isEquipped)
                {
                    other.gameObject.GetComponent<PlayerMovementAdvanced>().allGuns[i].isEquipped = true;
                    PickedUp();
                }
            }
        }
    }

    [PunRPC]
    private void PickedUp()
    {
        Destroy(gameObject, .5f);
    }

    [PunRPC]
    private void SetPickUpWeapon(int selectedWeapon)
    {
        
        for (int i = 0; i < gunsList.Length; i++)
        {
            gunsList[i].SetActive(false);
        }
        
        gunsList[selectedWeapon].SetActive(true);

        //gunsList[weaponToBeSet].SetActive(true);
        //gun = gunsList[weaponToBeSet];

        gun = gunsList[selectedWeapon];
    }
}
