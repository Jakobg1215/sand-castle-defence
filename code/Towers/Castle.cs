using TowerDefense.Management;

namespace TowerDefense.Towers;

[Category("Towers")]
[Icon("castle")]
internal sealed class Castle : Component {
    private EconomyManager Economy;
    [Property] private readonly float PayoutInterval = 10;
    [Property] private readonly ulong PayoutAmount = 10;
    private TimeSince PreviousPayout = 0;

    protected override void OnAwake() => this.Economy = this.Scene.Components.GetInDescendants<EconomyManager>();

    protected override void OnFixedUpdate() {
        if (this.IsProxy) {
            return;
        }

        if (this.PreviousPayout > this.PayoutInterval) {
            this.PreviousPayout = 0;
            this.Economy.AddFunds(this.PayoutAmount);
        }
    }
}
