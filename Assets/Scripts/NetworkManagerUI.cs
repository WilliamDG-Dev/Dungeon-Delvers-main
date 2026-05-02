using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Relay relay;
    [SerializeField] private TMP_InputField enterlobbyName;
    [SerializeField] private Button hostLobbyBtn;
    [SerializeField] private Button leaveLobbyBtn;
    [SerializeField] private GameObject playerHealthBar;
    [SerializeField] private List<GameObject> lobbyButtonList = new List<GameObject>();

    private void Awake()
    {
        UIVisibility(true);
        hostLobbyBtn.onClick.AddListener(() => { LobbyChecks(); UIVisibility(false); });
        leaveLobbyBtn.onClick.AddListener(() => { relay.LeaveGame(); });

        for (int index = 0; index < lobbyButtonList.Count; index++)
        {
            int capturedIndex = index;

            LobbyButtonText(index).text = "";
            lobbyButtonList[index].GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    relay.JoinLobbyByID(relay.lobbyIDList[capturedIndex]);
                    UIVisibility(false);
                }
                catch
                {
                    Debug.Log("No lobbies found");
                    UIVisibility(true);
                }
            });
        }
    }

    private void LobbyChecks()
    {
        if (enterlobbyName.text == null || enterlobbyName.text.Trim() == "") 
        {
            relay.CreateLobby("Lobby" + Random.Range(10,99)); 
        }
        else
        {
            relay.CreateLobby(enterlobbyName.text);
        }
    }

    private void Start()
    {
        InvokeRepeating("RefreshLobbyList", 0, 2);
    }

    private void RefreshLobbyList()
    {
        relay.ListLobbies();
        for (int index = 0; index < lobbyButtonList.Count; index++)
        {
            if (lobbyButtonList[index].activeSelf)
            {
                try
                {
                    LobbyButtonText(index).text = relay.lobbyDetailsList[index];
                }
                catch
                {
                    LobbyButtonText(index).text = "";
                }
            }
        }
    }

    private TextMeshProUGUI LobbyButtonText(int index)
    {
        return lobbyButtonList[index].GetComponentInChildren<TextMeshProUGUI>();
    }

    private void UIVisibility(bool visible)
    {
        Transform[] uiElements = gameObject.GetComponentsInChildren<Transform>();
        
        foreach (Transform element in uiElements)
        {
            element.gameObject.SetActive(visible);
        }
        leaveLobbyBtn.gameObject.SetActive(!visible);
        playerHealthBar.gameObject.SetActive(!visible);
        
    }
}




