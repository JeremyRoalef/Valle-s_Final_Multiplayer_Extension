using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;


public class NetworkSession : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI outputText;

    [SerializeField]
    TMP_InputField joinCodeInput;

    [SerializeField]
    GameObject container;

    private async void Start()
    {
        //Start unity services
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
            outputText.text = "Signed in " + AuthenticationService.Instance.PlayerId;
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartHost()
    {
        try
        {
            //Allocation = number of players - the host. Only clients
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            Debug.Log(joinCode);
            NetworkManager.Singleton.StartHost();
            outputText.text = joinCode;
            container.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            outputText.text = e.Message;
        }

    }

    public async void StartClient()
    {
        string joinCode = joinCodeInput.text;
        Debug.Log("join code from input field: " + joinCode);

        try
        {
            Debug.Log("joining relay with code: " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            NetworkManager.Singleton.StartClient();

            outputText.text = joinCode;
            container.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            outputText.text = e.Message;
        }
    }
}
