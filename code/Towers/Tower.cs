namespace TowerDefense.Towers;

[Hide]
internal class Tower : Component {
    [Property] public static readonly uint Cost = 50;
    [Property] protected readonly uint MaxHealth = 10;
    protected uint Health;

    protected override void OnAwake() => this.Health = this.MaxHealth;

    /// <summary>
    /// Deals damage to the tower. If damage amount is greater than the health, the tower will be destroyed.
    /// </summary>
    /// <param name="Damage">The amount of damage to take.</param>
    public void TakeDamage(uint Damage) {
        if (this.Health > Damage) {
            this.Health -= Damage;
            return;
        }

        this.Collapse();
    }

    /// <summary>
    /// Destroys the tower.
    /// </summary>
    public void Collapse() => this.Destroy();
}
