using Sandbox.Citizen;

namespace TowerDefense.Enemies;

[Category("Enemies")]
[Icon("whatshot")]
internal sealed class Bandit : Enemy {
    [Property] private readonly NavMeshAgent Agent;
    [Property] private readonly CitizenAnimationHelper AnimationHelper;

    /// <summary>
    /// Tells where the bandit should go.
    /// </summary>
    /// <param name="Position">The location on where the bandit should go.</param>
    public void SetTargetPosition(Vector3 Position) => this.Agent.MoveTo(Position);

    protected override void OnFixedUpdate() {
        this.AnimationHelper.WithVelocity(this.Agent.Velocity);
        this.AnimationHelper.WithWishVelocity(this.Agent.WishVelocity);
    }
}
