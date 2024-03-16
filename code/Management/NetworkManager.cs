using System.Linq;
using System.Threading.Tasks;

using Sandbox.Network;

namespace TowerDefense.Management;

[Category("Tower Defense")]
[Icon("rss_feed")]
internal sealed class NetworkManager : Component, Component.INetworkListener {
    // [Property] private readonly TowerManager Towers;
    [Property] private readonly GameObject PlayerPrefab;
    public int PlayerCount { get; private set; }

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }

        _ = this.Network.TakeOwnership();
    }

    protected override async Task OnLoad() {
        await base.OnLoad();

        if (this.Scene.IsEditor || GameNetworkSystem.IsActive) {
            return;
        }

        GameNetworkSystem.CreateLobby();
    }

    public void OnActive(Connection Channel) {
        Log.Info($"Player '{Channel.DisplayName}' has joined the game");

        // if (!Channel.IsHost) {
        //     this.Towers.AddHub(Channel.DisplayName + "'s Hub");
        // }

        this.PlayerCount++;

        var StartLocation = this.GetSpawnLocation().WithScale(1);
        var NewPlayer = this.PlayerPrefab.Clone(StartLocation, parent: this.GameObject);
        _ = NewPlayer.NetworkSpawn(Channel);

    }

    public void OnDisconnected(Connection Channel) {
        Log.Info($"Player '{Channel.DisplayName}' has left the game");

        // if (!Channel.IsHost) {
        //     this.Towers.RemoveHub(Channel.DisplayName + "'s Hub");
        // }

        this.PlayerCount--;
    }

    public void OnBecameHost(Connection PreviousChannel) => Log.Info("You are now the host of the game");

    /// <summary>
    /// Gets a random spawn location from the scene.
    /// </summary>
    /// <returns>A valid spawn point or the position of the map.</returns>
    private Transform GetSpawnLocation() {
        var SpawnPoints = this.Scene.GetAllComponents<SpawnPoint>().ToArray();
        return SpawnPoints.Length > 0 ? Random.Shared.FromArray(SpawnPoints).Transform.World : this.Transform.World;
    }
}
