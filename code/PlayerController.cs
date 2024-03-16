using System.Linq;

using Sandbox.Citizen;

using TowerDefense.Management;
using TowerDefense.Towers;

namespace TowerDefense;

[Category("Tower Defense")]
[Icon("person")]
internal sealed class PlayerController : Component, Component.INetworkSpawn {
    private TowerManager Towers;
    private EconomyManager Economy;
    [Property] private readonly float WalkSpeed = 100;
    [Property] private readonly float RunSpeed = 300;
    [Property] private readonly float JumpStrength = 450;
    [Property] private readonly float VoidHeight = -1000;
    [Property] private readonly GameObject Camera;
    [Property] private readonly CharacterController CharacterController;
    [Property] private readonly CitizenAnimationHelper AnimationHelper;
    [Property] private readonly SkinnedModelRenderer ModelRenderer;

    [Sync] private Angles LookOrientation { get; set; }
    [Sync] private Vector3 WishVelocity { get; set; }
    [Sync] private bool IsCrouching { get; set; }
    private TowerType SelectedTowerType = TowerType.None;

    protected override void OnStart() {
        if (this.IsProxy) {
            this.ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
            this.Camera.Enabled = false;
            return;
        }

        this.Scene.NavMesh.IsEnabled = true;

        Angles CurrentRotation = this.Transform.Rotation;
        this.LookOrientation = CurrentRotation.WithPitch(0).WithRoll(0);
        this.Towers = this.Scene.Components.GetInDescendants<TowerManager>();
        this.Economy = this.Scene.Components.GetInDescendants<EconomyManager>();
    }

    protected override void OnUpdate() {
        if (this.IsProxy) {
            return;
        }

        var InputLook = Input.AnalogLook.Normal;

        this.LookOrientation += InputLook;
        this.LookOrientation = this.LookOrientation.WithPitch(this.LookOrientation.pitch.Clamp(-60, 80));
    }

    protected override void OnFixedUpdate() {
        if (!this.IsProxy) {
            this.BuildingInput();
        }

        this.DoMovementInput();
        this.UpdateCharacter();
    }

    public void OnNetworkSpawn(Connection Channel) {
        var Clothing = ClothingContainer.CreateFromJson(Channel.GetUserData("avatar"));
        Clothing.Apply(this.ModelRenderer);
        this.UpdateBodyVisibility();
    }

    /// <summary>
    /// Processes the player's input and movement.
    /// </summary>
    private void DoMovementInput() {
        this.BuildWishVelocity();

        if (this.CharacterController.IsOnGround && Input.Pressed("Jump")) {
            this.AnimationHelper.TriggerJump();
            this.CharacterController.Punch(Vector3.Up * this.JumpStrength);
        }

        this.IsCrouching = this.CharacterController.IsOnGround && Input.Down("Duck");

        this.CharacterController.Height = this.IsCrouching ? 48 : 72;
        this.Camera.Transform.LocalPosition = this.IsCrouching ? this.Camera.Transform.LocalPosition.WithZ(39) : this.Camera.Transform.LocalPosition.WithZ(63);

        if (this.CharacterController.IsOnGround) {
            this.CharacterController.Velocity = this.CharacterController.Velocity.WithZ(0f);
            this.CharacterController.Accelerate(this.WishVelocity);
            this.CharacterController.ApplyFriction(4.0f);
        }
        else {
            this.CharacterController.Velocity += this.Scene.PhysicsWorld.Gravity * Time.Delta;
            this.CharacterController.Accelerate(this.WishVelocity.ClampLength(50f));
            this.CharacterController.ApplyFriction(0.5f);
        }

        this.CharacterController.Move();

        if (!this.CharacterController.IsOnGround) {
            this.CharacterController.Velocity += this.Scene.PhysicsWorld.Gravity * Time.Delta;
        }
        else {
            this.CharacterController.Velocity = this.CharacterController.Velocity.WithZ(0);
        }

        if (this.Transform.Position.z < this.VoidHeight) {
            var SpawnPoints = this.Scene.GetAllComponents<SpawnPoint>().ToArray();
            this.Transform.World = SpawnPoints.Length > 0 ? Random.Shared.FromArray(SpawnPoints).Transform.World : this.Transform.World;
        }

        this.Transform.Rotation = Rotation.FromYaw(this.LookOrientation.ToRotation().Yaw());
        this.Camera.Transform.LocalRotation = new Angles(this.LookOrientation.pitch, 0, 0);
    }

    /// <summary>
    /// Sets the wishvelocity to a vector based on the player's input.
    /// </summary>
    private void BuildWishVelocity() {
        var LookRotation = Rotation.FromYaw(this.LookOrientation.ToRotation().Yaw());

        this.WishVelocity = LookRotation * Input.AnalogMove.Normal;

        if (Input.Down("Run") && !this.IsCrouching) {
            this.WishVelocity *= this.RunSpeed;
        }
        else {
            this.WishVelocity *= this.WalkSpeed;
        }
    }

    /// <summary>
    /// Updates the character's animation.
    /// </summary>
    private void UpdateCharacter() {
        this.AnimationHelper.WithVelocity(this.CharacterController.Velocity);
        this.AnimationHelper.WithWishVelocity(this.WishVelocity);
        this.AnimationHelper.IsGrounded = this.CharacterController.IsOnGround;
        this.AnimationHelper.WithLook(this.LookOrientation.Forward * 1024, 1, 0.5f, 0.25f);
        this.AnimationHelper.DuckLevel = this.IsCrouching ? 1 : 0;
    }

    protected override void OnPreRender() => this.UpdateBodyVisibility(); // TODO: See if this can be removed.

    /// <summary>
    /// Hides the host player's body parts.
    /// </summary>
    private void UpdateBodyVisibility() {
        foreach (var ClothingObject in this.ModelRenderer.GameObject.Children) {
            if (!ClothingObject.Tags.Has("clothing")) {
                continue;
            }

            var Model = ClothingObject.Components.Get<ModelRenderer>();
            if (Model is null) {
                continue;
            }

            Model.RenderType = this.IsProxy ? Sandbox.ModelRenderer.ShadowRenderType.On : Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
        }
    }

    /// <summary>
    /// The player's steamid.
    /// </summary>
    private ulong SteamId => this.Network.OwnerConnection.SteamId;

    /// <summary>
    /// Processes the player's input for building towers.
    /// </summary>
    // TODO: This should be split up into a separate methods.
    private void BuildingInput() {
        if (Input.Pressed("Slot1")) {
            this.SelectedTowerType = TowerType.Castle;
        }

        if (Input.Pressed("Slot2")) {
            this.SelectedTowerType = TowerType.WatchTower;
        }

        if (Input.Pressed("attack2")) {
            if (this.SelectedTowerType != TowerType.None) {
                this.Scene.Children.Find(X => X.Tags.Has("tower_visualizer"))?.Destroy();
            }

            this.SelectedTowerType = TowerType.None;
        }

        if (this.SelectedTowerType == TowerType.None) {
            return;
        }

        int BuildReach = 250;
        var EyePosition = this.Camera.Transform.Position;
        var EyeDirection = this.LookOrientation.Forward;

        var RayTrace = this.Scene.Trace.Ray(EyePosition, EyePosition + (EyeDirection * BuildReach)).WithoutTags("towers").Run();

        if (!RayTrace.Hit) {
            this.Scene.Children.Find(X => X.Tags.Has("tower_visualizer"))?.Destroy();
            return;
        }

        var TowerPlacement = this.Scene.NavMesh.GetClosestPoint(RayTrace.HitPosition);

        if (TowerPlacement is null) {
            return;
        }

        var TowerVisualizer = this.Scene.Children.Find(X => X.Tags.Has("tower_visualizer"));

        if (TowerVisualizer is null) {
            TowerVisualizer = this.Scene.CreateObject();
            TowerVisualizer.Tags.Add("tower_visualizer");
            TowerVisualizer.Name = "Tower Visualizer";
            var Renderer = TowerVisualizer.Components.Create<ModelRenderer>();
            Renderer.SetMaterial(Material.Load("materials/tower_visualizer.vmat"));
            Renderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.Off;
            Renderer.Tint = Color.Green;
        }

        TowerVisualizer.Transform.Position = (Vector3)TowerPlacement;

        bool IsValidPlacement = !this.Towers.CanPlaceTower((Vector3)TowerPlacement);
        bool HasFunds = this.SelectedTowerType == TowerType.Castle || this.Economy.HasTheFunds(this.SteamId, TowerManager.CostOfTower(this.SelectedTowerType));
        bool CanPlace = IsValidPlacement && HasFunds;

        CanPlace = this.SelectedTowerType == TowerType.Castle ? CanPlace && this.Towers.CanPlaceCastle : CanPlace && !this.Towers.CanPlaceCastle;

        string ModelPath = this.SelectedTowerType switch {
            TowerType.WatchTower => "models/towers/sandcastle.vmdl",
            TowerType.Castle => "models/towers/sandcastle.vmdl",
            TowerType.Hub => "",
            TowerType.None => "",
            _ => "",
        };
        var ModelRenderer = TowerVisualizer.Components.Get<ModelRenderer>();
        ModelRenderer.Model = Model.Load(ModelPath);
        ModelRenderer.Tint = CanPlace ? Color.Green : Color.Red;

        if (!CanPlace) {
            return;
        }

        if (Input.Pressed("attack1")) {
            if (this.SelectedTowerType == TowerType.Castle) {
                this.Towers.CreateCastle((Vector3)TowerPlacement);
                return;
            }
            this.Towers.CreateTower(this.SteamId, (Vector3)TowerPlacement, this.SelectedTowerType);
        }

    }
}
