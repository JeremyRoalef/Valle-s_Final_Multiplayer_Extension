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
        }
        //The server now has final say in the player movement
        if (IsServer)
        {
            thirdPersonController.enabled = true;
        }
    }

    //Take note on the naming convention used for the method, and the Unity Attribute used above the method. Any RPC method must end with Rpc.
    //When using an Rpc in this manner, this networkbehaviour script does exist on both the client and the server, but it will only execute on the server
    [Rpc(SendTo.Server)]
    private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    {
        //Since the server has authority over the network transform, calling the methods that move the player on the server's end will move the player
        //To understand how exactly the movement works, click on one of the methods and press f12 to open the location of the method in the StarterAssetsInputs.cs script
        starterAssetsInputs.MoveInput(move);
        starterAssetsInputs.LookInput(look);
        starterAssetsInputs.JumpInput(jump);
        starterAssetsInputs.SprintInput(sprint);

    }

    private void LateUpdate()
    {
        //If this is called on a client that doesn't own this behaviour, return early
        if (!IsOwner) return;

        //Tell the server to move the player
        UpdateInputServerRpc(
            starterAssetsInputs.move, 
            starterAssetsInputs.look, 
            starterAssetsInputs.jump, 
            starterAssetsInputs.sprint
            );
    }
}
