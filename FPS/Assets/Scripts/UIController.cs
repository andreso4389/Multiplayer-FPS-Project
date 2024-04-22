using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    [Header("Indicators")]
    public TMP_Text Health;
    public Image bloodOverlay;
    public TMP_Text Ammo;
    public GameObject[] gunIcons;
    public GameObject killIndicator;
    public Image killIcon;
    public GameObject dmgIndicator;
    public TMP_Text dmgText;
    public GameObject pickUpIndicator;

    [Header("Death UI")]
    public GameObject deathScreen;
    public TMP_Text deathText;

    [Header("Options UI")]
    public GameObject optionsScreen;
    public Slider sensSlider;

    public static UIController instance;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        PlayerMovementAdvanced.instance.sensX = sensSlider.value * 100;
        PlayerMovementAdvanced.instance.sensY = sensSlider.value * 100;

        //Update sensitivity if it is saved
        if (LBUIController.instance.sensIsSaved)
        {
            sensSlider.value = LBUIController.instance.savedSens / 100;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHideOptions();
        }

        if (optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    public void ShowHideOptions()
    {
        if (!optionsScreen.activeInHierarchy)
        {
            optionsScreen.SetActive(true);
        }
        else
        {
            optionsScreen.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
