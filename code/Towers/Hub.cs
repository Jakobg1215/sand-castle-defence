using TowerDefense.Management;

namespace TowerDefense.Towers;

[Category("Towers")]
[Icon("local_mall")]
internal sealed class Hub : Component {
    private DifficultyManager Difficulty;
    private EnemyManager Enemies;
    private TimeSince PreviousWave = 0;

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }

        this.Difficulty = this.Scene.Components.GetInDescendants<DifficultyManager>();
        this.Enemies = this.Scene.Components.GetInDescendants<EnemyManager>();
    }

    protected override void OnFixedUpdate() {
        if (this.IsProxy) {
            return;
        }

        if (this.PreviousWave > this.Difficulty.WaveSpawnTime) {
            this.PreviousWave = 0;
            this.SpawnWave();
        }
    }

    /// <summary>
    /// Spawns a wave of enemies at the hub's location.
    /// </summary>
    private void SpawnWave() => this.Enemies.SpawnWaveAt(this.Transform.Position);
}
