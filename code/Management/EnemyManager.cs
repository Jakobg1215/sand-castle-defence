using TowerDefense.Enemies;

namespace TowerDefense.Management;

[Category("Tower Defense")]
[Icon("group")]
internal sealed class EnemyManager : Component {
    [Property] private readonly DifficultyManager Difficulty;
    [Property] private readonly TowerManager Towers;
    [Property] private readonly GameObject BanditPrefab;

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }

        _ = this.Network.TakeOwnership();
    }

    /// <summary>
    /// Spawns a wave of bandits at the given position. The wave is scaled based on the difficulty.
    /// </summary>
    /// <param name="Position">The location wher the wave should spawn</param>
    public void SpawnWaveAt(Vector3 Position) {
        for (int CurrentSizeCount = 0; CurrentSizeCount < this.Difficulty.WaveSize; CurrentSizeCount++) {
            var NewBandit = this.BanditPrefab.Clone(this.GameObject, Position, Rotation.Identity, Vector3.One);
            _ = NewBandit.NetworkSpawn();
            var TargetPosition = this.Towers.GetClosestCastlePosition(Position);
            NewBandit.Components.Get<Bandit>().SetTargetPosition(TargetPosition);
        }
    }

    public void EndGame() {
        foreach (var Enemy in this.GameObject.Children) {
            Enemy.Destroy();
        }
    }
}
