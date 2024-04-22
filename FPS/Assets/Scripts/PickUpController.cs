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
        selected = Random.Range(0, gunsList.Length);

        photonView.RPC("SetPickUpWeapon", RpcTarget.All, selected);
    }

    void Update()
    {
        Vector3 newRotation = new Vector3(rotateX, rotateY, rotateZ);
        gunHolder.transform.Rotate(newRotation * Time.deltaTime * 10);
    }

    [PunRPC]
    private void SetPickUpWeapon(int selectedWeapon)
    {
        
        for (int i = 0; i < gunsList.Length; i++)
        {
            gunsList[i].SetActive(false);
        }
        
        gunsList[selectedWeapon].SetActive(true);

        gun = gunsList[selectedWeapon];
    }
}
