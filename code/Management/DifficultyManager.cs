namespace TowerDefense.Management;

[Category("Tower Defense")]
[Icon("keyboard_arrow_up")]
internal sealed class DifficultyManager : Component {
    [Property] private readonly NetworkManager Networking;
    [Property] public readonly uint MaxCastle = 10;
    [Property] private readonly uint CastleDifficultyScale = 500;
    [Property] private readonly float MinimumWaveSpawnTime = 1;
    [Property] private readonly float BaseWaveSpawnTime = 5;
    [Property] private readonly float WaveSpawnTimeDecrease = 0.1f;
    [Property] private readonly float WaveSpawnTimeDifficultyScale = 250;
    [Property] private readonly uint MaxWaveSize = 10;
    [Property] private readonly uint WaveDifficultyScale = 150;
    [Property] private readonly uint HealthBonusScale = 100;
    /// <summary>
    /// The current difficulty of the game. This value is arbitrary.
    /// </summary>
    [Sync] private ulong Difficulty { get; set; }

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }

        _ = this.Network.TakeOwnership();
    }

    /// <summary>
    /// Increases the difficulty by the given amount.
    /// </summary>
    /// <param name="Amount">This value is arbitrary.</param>
    public void IncreaseDifficulty(ulong Amount) => this.Difficulty += Amount * (ulong)this.Networking.PlayerCount;

    /// <summary>
    /// Useful for UI to display the current max amount of castles that can be placed.
    /// </summary>
    /// <returns>The max amount of castles that can be allowed at once.</returns>
    public uint CastleCount => (uint)Math.Min(this.MaxCastle, (this.Difficulty / this.CastleDifficultyScale) + 1);

    /// <summary>
    /// Gets the time it takes for the next wave to spawn.
    /// </summary>
    /// <returns>The time between a spawn wave.</returns>
    public float WaveSpawnTime => Math.Max(this.MinimumWaveSpawnTime, this.BaseWaveSpawnTime - (this.Difficulty / this.WaveSpawnTimeDifficultyScale * this.WaveSpawnTimeDecrease));

    public uint WaveSize => (uint)Math.Min(this.MaxWaveSize, (this.Difficulty / this.WaveDifficultyScale) + 1);

    public uint HealthBonus => (uint)(this.Difficulty / this.HealthBonusScale);

    public void EndGame() => this.Difficulty = 0;
}
