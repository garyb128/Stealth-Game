using System;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject lostText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable ()
    {
        if (GameManager.Instance == null) return; // In case GameManager was destroyed first
        GameManager.Instance.OnGameWin += HandleGameWin;
        GameManager.Instance.OnGameOver += HandleGameOver;
    }

    void OnDestroy()
    {
        if(GameManager.Instance == null) return; // In case GameManager was destroyed first
        GameManager.Instance.OnGameWin -= HandleGameWin;
        GameManager.Instance.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        Debug.Log("You Lose!");
        lostText.SetActive(true);
        winText.SetActive(false);
    }

    private void HandleGameWin()
    {
        Debug.Log("You Win!");
        winText.SetActive(true);
        lostText.SetActive(false);
    }
}
