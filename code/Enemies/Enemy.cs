using TowerDefense.Management;

namespace TowerDefense.Enemies;

[Hide]
internal class Enemy : Component, Component.ITriggerListener {
    protected DifficultyManager Difficulty;
    protected EconomyManager Economy;
    [Property] public readonly uint MaxHealth = 10;
    [Property] protected readonly uint StealAmount = 10;
    [Property] protected readonly uint KillReward = 5;
    [Property] protected readonly uint DifficultyReward = 5;
    [Property] protected readonly WorldPanel WorldPanel;
    /// <summary>
    /// The current health of the enemy.
    /// </summary>
    [Sync] public uint Health { get; private set; }

    protected override void OnStart() {
        if (this.IsProxy) {
            return;
        }

        this.Difficulty = this.Scene.Components.GetInDescendants<DifficultyManager>();
        this.Economy = this.Scene.Components.GetInDescendants<EconomyManager>();

        this.Health = this.MaxHealth + this.Difficulty.HealthBonus;
    }

    public void OnTriggerEnter(Collider Other) {
        if (Other.Tags.Has("castletarget")) {
            this.Economy.RemoveFunds(this.StealAmount);

            // TODO: Add a visual effect for stealing money from the player.

            this.GameObject.Destroy();
        }
    }

    public void OnTriggerExit(Collider Other) { }

    /// <summary>
    /// Deals damage to the enemy. If the damage is greater than the health, the enemy will die.
    /// </summary>
    /// <param name="Damage">The amount of damage to take.</param>
    public void TakeDamage(uint Damage) {
        if (this.Health > Damage) {
            this.Health -= Damage;
            return;
        }

        this.Difficulty.IncreaseDifficulty(this.DifficultyReward);
        this.Die();
    }

    /// <summary>
    /// This will destroy the enemy.
    /// </summary>
    public void Die() {
        this.Economy.AddFunds(this.KillReward);
        this.GameObject.Destroy();
    }

    [Broadcast(NetPermission.HostOnly)]
    public void RevealHealth() => this.WorldPanel.Enabled = true;
}
