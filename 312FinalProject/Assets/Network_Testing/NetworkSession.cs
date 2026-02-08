using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkSession : MonoBehaviour
{
    static NetworkSession networkSession;
    string joinCode = string.Empty;

    enum InitStatus
    {
        AwaitingInitialization,
        AwaitingSignIn,
        SignedIn
    }

    public static int MAX_CLIENTS_EXCLUDING_HOST = 15;
    static InitStatus initStatus = InitStatus.AwaitingInitialization;

    private void Awake()
    {
        //Singleton pattern
        if (networkSession == null)
        {
            networkSession = this;
            DontDestroyOnLoad(gameObject);
            initStatus = InitStatus.AwaitingSignIn;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        //Handle cases where unity services aren't initialized
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        //Handle cases where already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            HandleAuthServiceSignIn();
        }
        else
        {
            //Update status when signed into auth services
            AuthenticationService.Instance.SignedIn += HandleAuthServiceSignIn;

            //Sign into authn services
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    /// <summary>
    /// Starts a host session for the player. On success, invokes the success method(s) and returns a join code. On fail,
    /// invokes the fail method(s) and returns the error message.
    /// </summary>
    public static async Task StartHostAsync(int numOfClientsInSession, Action<string> OnHostSessionSuccessful, Action<string> OnHostSessionFailed)
    {
        //Check if attmepting to start host before initialization
        if (initStatus != InitStatus.SignedIn)
        {
            OnHostSessionFailed?.Invoke($"Not signed in. Cause: {initStatus.DisplayName()}");
            return;
        }

        //Enforce max client count
        numOfClientsInSession = Mathf.Clamp(numOfClientsInSession, 0, MAX_CLIENTS_EXCLUDING_HOST);

        try
        {
            //Allocation = number of players - the host. Only clients that connect to the host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(numOfClientsInSession);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            
            if (NetworkManager.Singleton == null)
            {
                Debug.Log("Network manager missing!");
                OnHostSessionFailed?.Invoke("Failed to start host. Cause: internal setup with network manager.");
                return;
            }
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            Debug.Log(joinCode);

            if (NetworkManager.Singleton.StartHost())
            {
                //Session started successfully
                OnHostSessionSuccessful?.Invoke(joinCode);
                networkSession.joinCode = joinCode;
            }
            else
            {
                //Failed to join session
                OnHostSessionFailed?.Invoke("Failed to start host. Cause: unknown.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            OnHostSessionFailed?.Invoke(e.Message);
        }
        catch (NullReferenceException e)
        {
            Debug.Log(e);
            OnHostSessionFailed?.Invoke(e.Message);
        }
        catch
        {
            Debug.Log("Unknown start host failure");
            OnHostSessionFailed?.Invoke("Failed to start host. Cause: unknown.");
        }
    }

    public static async Task StartClientAsync(string joinCode, Action<string> OnSessionJoined, Action<string> OnSessionNotFound)
    {
        //Check if attmepting to start client before initialization
        if (initStatus != InitStatus.SignedIn)
        {
            OnSessionNotFound?.Invoke($"Not signed in. Cause: {initStatus.DisplayName()}");
            return;
        }

        try
        {
            Debug.Log("joining relay with code: " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            if (NetworkManager.Singleton == null)
            {
                Debug.Log("Network manager missing!");
                OnSessionNotFound?.Invoke("Failed to start host. Cause: internal setup with network manager.");
                return;
            }
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (NetworkManager.Singleton.StartClient())
            {
                //Session joined successfully
                OnSessionJoined?.Invoke(joinCode);
                networkSession.joinCode = joinCode;
            }
            else
            {
                Debug.Log("Unknown start client failure");
                OnSessionNotFound?.Invoke("Failed to join as client. Cause: unknown.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            OnSessionNotFound?.Invoke(e.Message);
        }
        catch (NullReferenceException e)
        {
            Debug.Log(e);
            OnSessionNotFound?.Invoke(e.Message);
        }
        catch
        {
            Debug.Log("Unknown start client failure");
            OnSessionNotFound?.Invoke("Failed to join as client. Cause: unknown.");
        }
    }

    private void HandleAuthServiceSignIn()
    {
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        initStatus = InitStatus.SignedIn;
        AuthenticationService.Instance.SignedIn -= HandleAuthServiceSignIn;
    }
}
