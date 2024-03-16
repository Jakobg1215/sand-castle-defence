using System.Linq;

using TowerDefense.Enemies;

namespace TowerDefense.Towers;

[Hide]
internal class FiringTower : Tower {
    [Property] protected readonly SphereCollider RangeCollider;
    [Property] protected readonly uint Damage = 4;
    [Property] protected readonly float FireRate = 1;
    private TimeSince LastFireTime = 0;

    protected override void OnFixedUpdate() {
        if (this.LastFireTime > this.FireRate) {
            this.LastFireTime = 0;
            this.FireAway();
        }
    }

    /// <summary>
    /// Fires at the closest enemy in range.
    /// </summary>
    private void FireAway() {
        if (this.EnemiesInRange.Length == 0) {
            return;
        }

        var ClosestTarget = this.EnemiesInRange.OrderBy(Enemy => this.Transform.Position.DistanceSquared(Enemy.Transform.Position)).First();

        if (ClosestTarget is null) {
            return;
        }

        var TowerTarget = ClosestTarget.Components.Get<Enemy>();
        TowerTarget.TakeDamage(this.Damage);
    }

    /// <summary>
    /// Enemies in range of the tower.
    /// </summary>
    protected GameObject[] EnemiesInRange => this.RangeCollider.Touching.Where(Collider => Collider.Tags.Has("enemy")).Select(Collider => Collider.GameObject).ToArray();
}