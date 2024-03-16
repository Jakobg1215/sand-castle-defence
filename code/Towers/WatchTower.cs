using TowerDefense.Enemies;

namespace TowerDefense.Towers;

[Category("Towers")]
[Icon("do_not_step")]
internal sealed class WatchTower : FiringTower {
    protected override void OnFixedUpdate() {
        if (this.IsProxy) {
            return;
        }

        base.OnFixedUpdate();

        foreach (var EnemyObject in this.EnemiesInRange) {
            var TowerTarget = EnemyObject.Components.Get<Enemy>();
            TowerTarget.RevealHealth();
        }
    }
}
