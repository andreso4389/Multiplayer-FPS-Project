using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject playerPrefab;
    private GameObject player;

    public GameObject deathEffect;

    public float respawnTime = 5f;


    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void Die(string damager, Transform playerPos)
    {
        PlayerMovementAdvanced.instance.killSelf = false;
        UIController.instance.deathText.text = "You got merked by " + damager;

        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if (player != null)
        {
            StartCoroutine(DieCo(playerPos));
        }
    }

    public IEnumerator DieCo(Transform playerPos)
    {
        PhotonNetwork.Instantiate(deathEffect.name, playerPos.position, Quaternion.identity);

        UIController.instance.deathScreen.SetActive(true);

        yield return new WaitForSeconds(respawnTime);
        
        PhotonNetwork.Destroy(player);
        player = null;

        UIController.instance.deathScreen.SetActive(false);


        if (MatchManager.instance.state == MatchManager.GameState.Playing && player == null)
        {
            SpawnPlayer();
        }
    }
}
