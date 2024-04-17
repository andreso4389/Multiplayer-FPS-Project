using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LBUIController : MonoBehaviour
{
   public static LBUIController instance;
   private void Awake()
    {
        instance = this;
    }
   
    public TMP_Text killsText, deathsText;
    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayerDisplay;

    public GameObject endScreen;

    public TMP_Text timerText;

    public float savedSens;
    public bool sensIsSaved = false;
}
