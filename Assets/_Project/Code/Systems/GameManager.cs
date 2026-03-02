using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lose Condition")]
    [SerializeField] float alertLoseTime = 3f;

    //Slow decay tuning
    [SerializeField] float alertFillRate = 1f; //units per second
    [SerializeField] float alertDecayRate = 0.5f; //units per second

    float alertTimer;
    bool gameOver;

    HashSet<NPCBrain> alertingNPCs = new HashSet<NPCBrain>();
    NPCBrain[] brains;

    public event Action OnGameOver;
    public event Action OnGameWin;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        brains = FindObjectsByType<NPCBrain>(FindObjectsSortMode.None);

        foreach (var b in brains)
        {
            b.OnStateChanged += HandleNPCStateChanged;

            // If an NPC starts in Alert for any reason, reflect it
            if (b.CurrentState == NPCBrain.EnemyState.Alert)
                alertingNPCs.Add(b);
        }
    }

    private void OnDestroy()
    {
        if (brains == null) return;

        foreach (var b in brains)
        {
            if (b != null)
            {
                b.OnStateChanged -= HandleNPCStateChanged;
            }
        }

    }

    private void Update()
    {
        if(gameOver) return;

        bool anyAlert = alertingNPCs.Count > 0;

        if (anyAlert)
            alertTimer += alertFillRate * Time.deltaTime;
        else
            alertTimer -= alertDecayRate * Time.deltaTime;

        alertTimer = Mathf.Clamp(alertTimer, 0f, alertLoseTime);

        if (alertTimer >= alertLoseTime)
            Lose();

    }

    void HandleNPCStateChanged(NPCBrain brain,NPCBrain.EnemyState oldState, NPCBrain.EnemyState newState)
    {
        if (newState == NPCBrain.EnemyState.Alert)
            alertingNPCs.Add(brain);

        if (oldState == NPCBrain.EnemyState.Alert && newState != NPCBrain.EnemyState.Alert)
            alertingNPCs.Remove(brain);
    }


    //Called by NPCs when they spot the player
    public void ReportAlert(bool alerting)
    {
        if (gameOver) return;

        if (alerting)
            alertTimer += Time.deltaTime;
        else
            alertTimer -= Time.deltaTime;

        alertTimer = Mathf.Clamp(alertTimer, 0, alertLoseTime);

        if (alertTimer >= alertLoseTime)
        {
            Lose();
        }
    }

    private void Lose()
    {
        gameOver = true;
        Debug.Log("You Lose!");

        //Restart the level after 2 seconds
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        OnGameOver?.Invoke();
    }

    public void Win()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("You Win!");
        OnGameWin?.Invoke();
    }
}
