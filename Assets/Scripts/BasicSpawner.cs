using Fusion;
using Fusion.Sockets;
using Fusion.Addons.Physics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    // The camera we will re-parent to the local player
    [SerializeField] private Camera mainCamera;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner _runner;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Unique position
            Vector3 spawnPosition = new Vector3((player.RawEncoded % 4) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

            _spawnedCharacters.Add(player, networkPlayerObject);
        }

        // Attach camera for local player only
        if (runner.LocalPlayer == player)
            TryAttachCamera(runner, player);
    }

    private async void TryAttachCamera(NetworkRunner runner, PlayerRef player)
    {
        // Wait until Fusion has fully spawned the player object
        await System.Threading.Tasks.Task.Delay(100);

        if (!_spawnedCharacters.TryGetValue(player, out NetworkObject playerObj))
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Re-parent camera under player
        mainCamera.transform.SetParent(playerObj.transform);

        // Set new camera local position
        mainCamera.transform.localPosition = new Vector3(
            0f,        // X
            1.17f,     // Y
            -2f        // Z
        );

        // Set rotation
        mainCamera.transform.localRotation = Quaternion.Euler(
            14f,   // X
            0f,    // Y
            0f     // Z
        );
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    private bool _mouseButton0;
    private bool _mouseButton1;

    private void Update()
    {
        _mouseButton0 = _mouseButton0 || Input.GetMouseButton(0);
        _mouseButton1 = _mouseButton1 || Input.GetMouseButton(1);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W)) data.direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) data.direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) data.direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) data.direction += Vector3.right;

        data.buttons.Set(NetworkInputData.MOUSEBUTTON0, _mouseButton0);
        _mouseButton0 = false;

        data.buttons.Set(NetworkInputData.MOUSEBUTTON1, _mouseButton1);
        _mouseButton1 = false;

        input.Set(data);
    }

    // Empty Fusion callbacks
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress remote, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }

    // Start button UI
    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
                StartGame(GameMode.Host);

            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
                StartGame(GameMode.Client);
        }
    }

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var physicsSim = gameObject.AddComponent<RunnerSimulatePhysics3D>();
        physicsSim.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
}
