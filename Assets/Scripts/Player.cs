using Fusion;
using UnityEngine;
using TMPro; // TextMeshPro

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;
    [SerializeField] private PhysxBall _prefabPhysxBall;

    [Networked] private TickTimer delay { get; set; }
    [Networked] public byte spawnedProjectileCounter { get; set; }

    private NetworkCharacterController _cc;
    private Vector3 _forward;

    // Visuals
    private ChangeDetector _changeDetector;
    private Material _instanceMaterial;

    // UI (TextMeshPro) for message display
    private TMP_Text _messages;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        // Grab the MeshRenderer in children and create an instance material for this player
        var mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null)
            _instanceMaterial = mr.material;
    }

    public override void Spawned()
    {
        // Initialize the change detector that watches simulation state networked props
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Ensure starting color (optional)
        if (_instanceMaterial != null)
            _instanceMaterial.color = Color.blue;

        // Try to find the TMP text in-scene (falls back to Find in RPC if null)
        _messages = FindObjectOfType<TMP_Text>();
    }

    private void Update()
    {
        // Only the client that owns this player should call the RPC.
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
        {
            // Example message — could be any string
            RPC_SendMessage("Hey Mate!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(
                        _prefabBall,
                        transform.position + _forward,
                        Quaternion.LookRotation(_forward),
                        Object.InputAuthority,
                        (runner, o) =>
                        {
                            o.GetComponent<Ball>().Init();
                        });

                    // increment the counter so clients see a change
                    spawnedProjectileCounter++;
                }
                else if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(
                        _prefabPhysxBall,
                        transform.position + _forward,
                        Quaternion.LookRotation(_forward),
                        Object.InputAuthority,
                        (runner, o) =>
                        {
                            o.GetComponent<PhysxBall>().Init(10 * _forward);
                        });

                    // increment the counter so clients see a change
                    spawnedProjectileCounter++;
                }
            }
        }
    }

    public override void Render()
    {
        if (_changeDetector != null && _instanceMaterial != null)
        {
            // Detect which networked properties changed since last Render()
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(spawnedProjectileCounter))
                {
                    // When a spawn is detected, flash white immediately
                    _instanceMaterial.color = Color.white;
                }
            }

            // Smoothly fade toward blue each render frame
            _instanceMaterial.color = Color.Lerp(_instanceMaterial.color, Color.blue, Time.deltaTime);
        }
    }

    //
    // RPCs
    //

    // Called by the client who has input authority. This goes to the StateAuthority (host).
    // RpcSources.InputAuthority + RpcTargets.StateAuthority ensures only the player who owns this object can call it,
    // and it is delivered to the state authority to be relayed.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        // Relay from the state authority to all clients, passing the original message source
        // (info.Source is the PlayerRef of the caller)
        RPC_RelayMessage(message, info.Source);
    }

    // Executed on StateAuthority, relays to All clients.
    // HostMode = SourceIsServer means the server/state-authority is considered the source here
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        // Ensure we have a TMP_Text reference (in case scene wasn't scanned in Spawned)
        if (_messages == null)
            _messages = FindObjectOfType<TMP_Text>();

        // If still null, bail out gracefully
        if (_messages == null)
            return;

        // Show different text if this client was the original sender
        if (messageSource == Runner.LocalPlayer)
        {
            _messages.text += $"You said: {message}\n";
        }
        else
        {
            _messages.text += $"Some other player said: {message}\n";
        }
    }
}
