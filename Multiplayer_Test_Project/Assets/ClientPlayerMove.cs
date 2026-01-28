using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

//NetworkBehaviour is a subclass of MonoBehaviour with additional networking capabilities.
//It keeps track of client/server authority, ownership, role, and lifecycle. Also, it participates in synchronization
public class ClientPlayerMove : NetworkBehaviour
{
    //Cache reference to player movement components
    [SerializeField] PlayerInput playerInput;
    [SerializeField] StarterAssetsInputs starterAssetsInputs;
    [SerializeField] ThirdPersonController thirdPersonController;

    private void Awake()
    {
        //Disable player movement by default
        playerInput.enabled = false;
        starterAssetsInputs.enabled = false;
        thirdPersonController.enabled = false;
    }

    //OnNetworkSpawn is called when NetworkBehaviour is initialized and the NetworkObject is spawned on the network.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //IsOwner is true if the local client's ID (from the networkManager) matches the OwnerClientID
        //(from the networkObject) of this component
        if (IsOwner)
        {
            //Enable movement
            playerInput.enabled = true;
            starterAssetsInputs.enabled = true;
            thirdPersonController.enabled = true;
        }
    }
}
