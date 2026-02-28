using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour {


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private Button lobbyNameButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private GameObject waitingText;

    private string lobbyName;


    private void Start()
    {
        Instance = this;

        LobbyManager.Instance.OnJoinedLobby += OnJoinedLobby;

        createButton.onClick.AddListener(() =>
        {
            SetWaitingState(true);
            LobbyManager.Instance.CreateLobby(
                lobbyName
            );
        });

        lobbyNameButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Lobby Name", lobbyName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
            () =>
            {
                // Cancel
            },
            (string lobbyName) =>
            {
                this.lobbyName = lobbyName;
                UpdateText();
            });
        });

        gameObject.SetActive(false);
    }

    private void OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        SetWaitingState(false);
        Hide();
    }
    private void SetWaitingState(bool isWaiting)
    {
        createButton.gameObject.SetActive(!isWaiting);
        lobbyNameButton.gameObject.SetActive(!isWaiting);
        waitingText.SetActive(isWaiting);
    }

    private void UpdateText()
    {
        lobbyNameText.text = lobbyName;
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);

        lobbyName = "MyLobby";

        UpdateText();
    }

}