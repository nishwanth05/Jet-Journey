using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FinishLineManager : MonoBehaviour
{
    [Header("Race Settings")]
    public int totalCars = 100;   // 99 AI + Player
    bool raceFinished = false;

    [Header("UI")]
    public Text crossedCountText;     // always visible
    public Text playerResultText;     // shown only when player finishes

    //List<GameObject> finishOrder = new List<GameObject>();
    int count;
    void Start()
    {
        if (crossedCountText)
            crossedCountText.text = $"Crossed: 0 / {totalCars}";

        if (playerResultText)
            playerResultText.text = "";
    }

    void OnTriggerEnter(Collider other)
    { 
        if (raceFinished) return;

        // Any car crosses finish
        if (other.gameObject.CompareTag("CarBody") || other.gameObject.CompareTag("CarBodyPlayer"))
        {
            count++;
            //finishOrder.Add(other.gameObject);
            UpdateCrossedUI();
        }

        // Player crosses finish
        if (other.CompareTag("CarBodyPlayer"))
        {
            raceFinished = true;

            int playerPosition = count;
            ShowPlayerResult(playerPosition);
            OnRaceFinished();
        }
    }

    /* ================= UI ================= */

    void UpdateCrossedUI()
    {
        if (crossedCountText)
            crossedCountText.text = $"Crossed: {count} / {totalCars}";
    }

    void ShowPlayerResult(int playerPosition)
    {
        string resultText = $"You Finished: {playerPosition} / {totalCars}";

        if (playerResultText)
            playerResultText.text = resultText;

        Debug.Log(resultText);
    }

    /* ================= RACE END ================= */

    void OnRaceFinished()
    {
        Debug.Log("🏁 RACE FINISHED");

        // Optional slow motion
        Time.timeScale = 0.5f;

        // Future extensions:
        // - Disable player input
        // - Show leaderboard
        // - Restart button
    }
}