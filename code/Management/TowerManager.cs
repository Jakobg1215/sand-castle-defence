using System.Diagnostics;
using System.Linq;

using TowerDefense.Towers;

namespace TowerDefense.Management;

[Category("Tower Defense")]
[Icon("location_city")]
internal sealed class TowerManager : Component {
    [Property] private readonly DifficultyManager Difficulty;
    [Property] private readonly EconomyManager Economy;
    [Property] private readonly GameObject CastlePrefab;
    [Property] private readonly GameObject CastleStorage;
    [Property] private readonly GameObject HubPrefab;
    [Property] private readonly GameObject WatchTowerPrefab;

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }

        _ = this.Network.TakeOwnership();
    }

    protected override void OnFixedUpdate() {
        if (this.IsProxy) {
            return;
        }
    }

    /// <summary>
    /// Creates a castle and its hubs at the specified position.
    /// </summary>
    /// <param name="Position">The location where to spawn the castle.</param>
    /// <returns>If the castle was created or not.</returns>
    [Authority]
    public void CreateCastle(Vector3 Position) {
        if (!this.CanPlaceCastle) {
            return;
        }

        // TODO: Make sure it can't be placed too close to another castle or hub.

        var NewCastle = this.CastlePrefab.Clone(this.CastleStorage, Position, Rotation.FromYaw(Random.Shared.Float(0, 360)), Vector3.One);
        _ = NewCastle.NetworkSpawn();
        this.CreateHubs(NewCastle);
        return;
    }

    /// <summary>
    /// If a castle can be placed or not.
    /// </summary>
    public bool CanPlaceCastle => this.CastleStorage.Children.Count < this.Difficulty.CastleCount;

    /// <summary>
    /// Creates hubs around a castle and random positions.
    /// </summary>
    /// <param name="Castle">The castle to spawn the hubs at</param>
    private void CreateHubs(GameObject Castle) {
        // TODO: Make the creation of hubs based on the difficulty value.
        for (int CurrentHubCount = 0; CurrentHubCount < 1; CurrentHubCount++) {
            var NewHub = this.HubPrefab.Clone();
            NewHub.Parent = Castle;
            NewHub.Transform.Position = this.GetRandomPositionAroundPoint(Castle.Transform.Position, 200, 250);
            _ = NewHub.NetworkSpawn();
        }
    }

    /// <summary>
    /// Adds a hub to all the castles.
    /// </summary>
    /// <param name="DisplayName">The name of the hub.</param>
    public void AddHub(string DisplayName = "") {
        foreach (var Castle in this.CastleStorage.Children) {
            this.CreateHub(Castle, DisplayName);
        }
    }

    /// <summary>
    /// Removes a hub from all the castles.
    /// </summary>
    /// <param name="DisplayName">The name of the hub to remove.</param>
    public void RemoveHub(string DisplayName = "Hub") => this.GameObject.Children.Find(Object => Object.Name == DisplayName)?.Destroy();

    /// <summary>
    /// Creates a hub at a random position around a castle.
    /// </summary>
    /// <param name="Castle">The castle to lay seige on.</param>
    /// <param name="Name">The name of the hub.</param>
    private void CreateHub(GameObject Castle, string Name = "Hub") {
        var NewHub = this.HubPrefab.Clone();
        NewHub.Name = Name;
        NewHub.Parent = Castle;
        NewHub.Transform.Position = this.GetRandomPositionAroundPoint(Castle.Transform.Position, 200, 250);
        _ = NewHub.NetworkSpawn();
    }

    /// <summary>
    /// Gets a random position around a point.
    /// </summary>
    /// <param name="Position">The point to get the random point around</param>
    /// <param name="MinimumDistance">The distance that the point must be greater than.</param>
    /// <param name="MaximumDistance">The distance that the point can't be grater than.</param>
    /// <returns>A random position around the point or the orignal point if failed to find a position.</returns>
    private Vector3 GetRandomPositionAroundPoint(Vector3 Position, float MinimumDistance, float MaximumDistance) {
        const byte MaxAttempts = 10;

        for (byte CurrentAttempt = 0; CurrentAttempt < MaxAttempts; CurrentAttempt++) {
            float RandomDirection = Random.Shared.Float(0, 360);
            float RandomDistance = Random.Shared.Float(MinimumDistance, MaximumDistance);
            var RandomPosition = Position + (Rotation.FromYaw(RandomDirection).Forward * RandomDistance);
            var RandomPoint = this.Scene.NavMesh.GetClosestPoint(RandomPosition);

            if (RandomPoint is null) {
                continue;
            }

            if (Vector3.DistanceBetween(Position, RandomPoint.Value) < MinimumDistance) {
                continue;
            }

            if (this.CastleStorage.Children.Any(Castle => Vector3.DistanceBetween(Castle.Transform.Position, RandomPoint.Value) < 200)) {
                continue;
            }

            return RandomPoint.Value;
        }

        Log.Error("Failed to find a random position around the point!");
        return Position;
    }

    /// <summary>
    /// Get the closest castle position from a specified position.
    /// </summary>
    /// <param name="Position">The location to check for the closest castle.</param>
    /// <returns>The closest castle position.</returns>
    public Vector3 GetClosestCastlePosition(Vector3 Position) {
        var ClosestCastle = this.CastleStorage.Children.OrderBy(Castle => Vector3.DistanceBetweenSquared(Castle.Transform.Position, Position)).First();
        return ClosestCastle.Transform.Position;
    }

    /// <summary>
    /// Creates a tower at the specified position.
    /// </summary>
    /// <param name="Player">The steamid of the player that is creating the tower.</param>
    /// <param name="Position">The location where to place the tower.</param>
    /// <param name="TowerType">The type of tower to place.</param>
    /// <returns>If the tower was created or not.</returns>
    [Authority]
    public void CreateTower(ulong Player, Vector3 Position, TowerType TowerType) {
        if (!this.Economy.HasTheFunds(Player, TowerManager.CostOfTower(TowerType))) {
            return;
        }

        if (this.CanPlaceTower(Position)) {
            return;
        }

        this.Economy.SpendFunds(Player, TowerManager.CostOfTower(TowerType));

        switch (TowerType) {
            case TowerType.WatchTower: {
                    _ = this.WatchTowerPrefab.Clone(this.GameObject, Position, Rotation.Identity, Vector3.One).NetworkSpawn();
                    break;
                }
            case TowerType.None:
            case TowerType.Castle:
            case TowerType.Hub:
            default:
                return;
        }

        return;
    }

    public bool CanPlaceTower(Vector3 Position) {
        int TowerPadding = 25;
        bool IsTooCloseToAnotherTower = this.GameObject.Children.Any(Tower => Vector3.DistanceBetween(Tower.Transform.Position, Position) < TowerPadding);
        bool IsTooCloseToACastleOrHub = this.CastleStorage.Children.Any(Castle => Vector3.DistanceBetween(Castle.Transform.Position, Position) < TowerPadding * 2 || Castle.Children.Any(Hub => Vector3.DistanceBetween(Hub.Transform.Position, Position) < TowerPadding));
        return IsTooCloseToAnotherTower || IsTooCloseToACastleOrHub;

    }

    public static uint CostOfTower(TowerType TowerType) {
        return TowerType switch {
            TowerType.WatchTower => WatchTower.Cost,
            TowerType.None => throw new UnreachableException("Can't get the cost of a tower type of None!"),
            TowerType.Castle => throw new UnreachableException("Can't get the cost of a tower type of Castle!"),
            TowerType.Hub => throw new UnreachableException("Can't get the cost of a tower type of Hub!"),
            _ => throw new UnreachableException(),
        };
    }

    public void EndGame() {
        foreach (var Tower in this.GameObject.Children) {
            if (Tower.Id == this.CastleStorage.Id) {
                continue;
            }

            Tower.Destroy();
        }

        foreach (var Castle in this.CastleStorage.Children) {
            Castle.Destroy();
        }
    }
}
