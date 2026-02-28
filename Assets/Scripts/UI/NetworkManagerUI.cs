using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Units;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]
    private bool spawnEnemiesInSinglePlayer = true;
    [SerializeField]
    private UnityTransport unityTransport;
    [SerializeField]
    private GameObject targetAcquiring;
    [SerializeField]
    private Button singlePlayerButton;
    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button relayButton;
    [SerializeField]
    private Button relayBackButton;
    [SerializeField]
    private GameObject panel;
    [SerializeField] List<NavMeshSurface> surfaces;
    [SerializeField] List<GameObject> playerSpawners;
    [SerializeField] List<GameObject> enemySpawners;
    [SerializeField] Transform mainParent;
    [SerializeField] GameObject relayMenu;
    [SerializeField] GameObject waitingText;
    [SerializeField] WinConditionWindow winConditionWindow;
    [SerializeField] GameObject progressBarCanvas;
    [SerializeField] Toggle autoSpawnPlayerUnits;
    [SerializeField] Toggle autoSpawnEnemyUnits;

    bool isSinglePlayer = false;

    private void Awake()
    {
        relayButton.onClick.AddListener(() =>
        {
            SetRelayMenuActive(true);
        });
        relayBackButton.onClick.AddListener(() =>
        {
            SetRelayMenuActive(false);
        });
        hostButton.onClick.AddListener(() =>
        {
            SwitchUnityTransport();
            SetWaitingMode(true);
            StartHost();
        });
        clientButton.onClick.AddListener(() =>
        {
            SwitchUnityTransport();
            SetWaitingMode(true);
            StartClient();
        });
        singlePlayerButton.onClick.AddListener(() =>
        {
            SwitchUnityTransport();
            StartSinglePlayer();
        });
    }

    public void StartHost()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (isSinglePlayer || clientId != NetworkManager.Singleton.LocalClientId)
            {
                Factory.Instance.CreateElixirManager();
                StartGame();
            }
        };

        NetworkManager.Singleton.StartHost();
        SetButtonsInteractable(false);
    }

    public void StartClient()
    {
        surfaces.ForEach(surf => surf.enabled = false);
        mainParent.Rotate(NetworkClientPositionFlipper.Instance.Angle);

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            StartGame();
        };

        SetButtonsInteractable(false);
        NetworkManager.Singleton.StartClient();
    }

    private void StartSinglePlayer()
    {
        isSinglePlayer = true;
        if (spawnEnemiesInSinglePlayer)
        {
            NetworkManager.Singleton.OnServerStarted += () =>
            {
                enemySpawners.ForEach(spawner => spawner.gameObject.SetActive(autoSpawnEnemyUnits.isOn));
                playerSpawners.ForEach(spawner => spawner.gameObject.SetActive(autoSpawnPlayerUnits.isOn));
            };
        }
        StartHost();
    }

    private void SetRelayMenuActive(bool active)
    {
        SetButtonsInteractable(!active);
        relayMenu.SetActive(active);
    }

    private void StartGame()
    {
        panel.SetActive(true);
        gameObject.SetActive(false);
        WinConditionChecker.Instance.OnWinConditionMet += OnMatchEnded;
        progressBarCanvas.SetActive(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        relayButton.interactable = interactable;
        hostButton.interactable = interactable;
        clientButton.interactable = interactable;
        singlePlayerButton.interactable = interactable;
    }

    public void SetWaitingMode(bool waiting)
    {
        waitingText.SetActive(waiting);
        SetButtonsInteractable(!waiting);
        if (waiting)
            relayMenu.SetActive(false);
    }

    private void OnMatchEnded(Sides losingTeam)
    {
        targetAcquiring.SetActive(false);
        winConditionWindow.gameObject.SetActive(true);
        bool isWinner =
            (NetworkManager.Singleton.IsHost && losingTeam != Sides.Player) ||
            (!NetworkManager.Singleton.IsHost && losingTeam != Sides.Enemy);
        winConditionWindow.SetWinner(isWinner);
        panel.GetComponent<UnitButtonPanel>().SetActive(false);
    }

    private void SwitchUnityTransport()
    {
        // NetworkManager.Singleton.GetComponent<UnityTransport>().enabled = false;
        // NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        // NetworkManager.Singleton.GetComponent<UnityTransport>().enabled = true;
    }
}
