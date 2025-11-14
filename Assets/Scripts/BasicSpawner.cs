using Fusion;
using Fusion.Sockets;
using Fusion.Addons.Physics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [Header("Camera (assign or leave empty to use Camera.main)")]
    [SerializeField] private Camera mainCamera;

    [Header("Camera attach settings")]
    [Tooltip("Optional child name on the player prefab to attach the camera to. If not found, camera will parent to player root.")]
    public string cameraAnchorName = "CameraAnchor";

    private NetworkRunner _runner;
    // server-side tracking (keeps original behavior)
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    // ----------------------------------------------------------------
    // INetworkRunnerCallbacks
    // ----------------------------------------------------------------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Server spawns the networked player object
        if (runner.IsServer)
        {
            Vector3 spawnPosition = new Vector3((player.RawEncoded % 4) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }

        // If this join corresponds to the local player on this runner, attach local camera when the local NetworkObject is available
        if (runner.LocalPlayer == player)
        {
            StartCoroutine(AttachMainCameraToLocalPlayerWhenReady(runner, player));
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    // -------------------------
    // Input / update
    // -------------------------
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

    // Empty implementations for the rest of INetworkRunnerCallbacks methods
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress remote, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }

    // ----------------------------------------------------------------
    // Game start / UI
    // ----------------------------------------------------------------
    async void StartGame(GameMode mode)
    {
        // Keep a reference to the runner we create
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var runnerSimulatePhysics3D = gameObject.AddComponent<RunnerSimulatePhysics3D>();
        runnerSimulatePhysics3D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }

    // ----------------------------------------------------------------
    // Camera attach coroutine & helper
    // ----------------------------------------------------------------
    private IEnumerator AttachMainCameraToLocalPlayerWhenReady(NetworkRunner runner, PlayerRef player)
    {
        float timeout = 5f;
        float elapsed = 0f;
        float poll = 0.05f;

        while (elapsed < timeout)
        {
            NetworkObject playerObj = null;

            try
            {
                playerObj = runner.GetPlayerObject(player);
            }
            catch
            {
                playerObj = null;
            }

            if (playerObj != null)
            {
                AttachMainCameraToPlayerObject(playerObj);
                yield break;
            }

            elapsed += poll;
            yield return new WaitForSeconds(poll);
        }

        Debug.LogWarning("[BasicSpawner] Timed out waiting for local player NetworkObject.");
    }

    private void AttachMainCameraToPlayerObject(NetworkObject playerNetworkObject)
{
    if (playerNetworkObject == null)
    {
        Debug.LogWarning("[BasicSpawner] playerNetworkObject is null in AttachMainCameraToPlayerObject.");
        return;
    }

    // Resolve camera
    Camera cam = mainCamera != null ? mainCamera : Camera.main;
    if (cam == null)
    {
        Debug.LogWarning("[BasicSpawner] No main camera assigned and Camera.main is null.");
        return;
    }

    // 🔥 FORCE THE TAG
    cam.tag = "MainCamera";

    // Find anchor on the player (optional)
    Transform parentTransform = null;
    if (!string.IsNullOrEmpty(cameraAnchorName))
    {
        var found = playerNetworkObject.transform.Find(cameraAnchorName);
        if (found != null)
            parentTransform = found;
        else
        {
            foreach (Transform t in playerNetworkObject.transform)
            {
                if (string.Equals(t.name, cameraAnchorName, StringComparison.OrdinalIgnoreCase))
                {
                    parentTransform = t;
                    break;
                }
            }
        }
    }

    if (parentTransform == null)
        parentTransform = playerNetworkObject.transform;

    cam.transform.SetParent(parentTransform, false);

    // Apply your camera transform
    cam.transform.localPosition = new Vector3(0f, 1.17f, -2f);
    cam.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

    Debug.Log("[BasicSpawner] Camera tag forced to MainCamera.");
}

}
