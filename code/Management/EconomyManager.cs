using System.Linq;

namespace TowerDefense.Management;

[Category("Tower Defense")]
[Icon("attach_money")]
internal sealed class EconomyManager : Component {
    [Property] private readonly ulong StartingFunds = 100;
    [Property] private readonly DifficultyManager Difficulty;
    [Property] private readonly EnemyManager Enemies;
    [Property] private readonly NetworkManager Networking;
    [Property] private readonly TowerManager Towers;
    /// <summary>
    /// The current funds of all players.
    /// The player's money is tied to their steamid.
    /// If they join back it will still be saved to not introduce any exploits.
    /// </summary>
    [Sync] private NetDictionary<ulong, ulong> PlayerFunds { get; } = new();
    /// <summary>
    /// The highest amount of funds that the players has gathered.
    /// </summary>
    private ulong HighestFunds;
    [Sync] public bool IsGameEnded { get; private set; }

    protected override void OnAwake() {
        if (this.IsProxy) {
            return;
        }
        _ = this.Network.TakeOwnership();
    }


    /// <summary>
    /// Checks if the player has enough funds to spend.
    /// </summary>
    /// <param name="Player">The steamid of the player.</param>
    /// <param name="Amount">The amount to check if they have the funds.</param>
    /// <returns>If the player has the funds for the amount.</returns>
    public bool HasTheFunds(ulong Player, ulong Amount) {
        ulong CurrentFunds = this.GetOrCreateAccount(Player);

        return CurrentFunds >= Amount;
    }

    /// <summary>
    /// Gets the current funds of the player or creates a new account with the starting funds.
    /// </summary>
    /// <param name="Player">The steamid of the player.</param>
    /// <returns>The amount they have or the starting amount.</returns>
    public ulong GetOrCreateAccount(ulong Player) {
        if (this.PlayerFunds.TryGetValue(Player, out ulong CurrentFunds)) {
            return CurrentFunds;
        }

        this.CreateAccount(Player);
        return this.StartingFunds;
    }

    [Authority]
    private void CreateAccount(ulong Player) => this.PlayerFunds[Player] = this.StartingFunds;

    /// <summary>
    /// Spends the funds of a player.
    /// </summary>
    /// <param name="Player"></param>
    /// <param name="Amount"></param>
    /// <returns>If it spent the funds or not.</returns>
    public void SpendFunds(ulong Player, ulong Amount) => this.PlayerFunds[Player] -= Amount;

    /// <summary>
    /// Adds funds to all players.
    /// </summary>
    /// <param name="Amount">The amount to give to all players.</param>
    public void AddFunds(ulong Amount) {
        foreach (var (Player, _) in this.PlayerFunds) {
            this.PlayerFunds[Player] += Amount;
        }

        ulong CurrentAmount = this.PlayerFunds.Values.Max();
        if (CurrentAmount > this.HighestFunds) {
            this.HighestFunds = CurrentAmount;
        }
    }

    /// <summary>
    /// Removes funds from all players.
    /// If a player doesn't have enough funds, it will increase the amount of players that don't have enough funds then the next player will have the amount removed times the amount of players that didn't have enough funds.
    /// If all players don't have enough funds, the game will end.
    /// </summary>
    /// <param name="Amount">The amout to remove from all players.</param>
    public void RemoveFunds(ulong Amount) {
        uint InsufficientFunds = 0;

        foreach (var (AccountOwner, CurrentFunds) in this.PlayerFunds) {
            if (CurrentFunds < Amount + (Amount * InsufficientFunds)) {
                InsufficientFunds++;
                continue;
            }
            this.PlayerFunds[AccountOwner] -= Amount + (Amount * InsufficientFunds);
        }

        if (InsufficientFunds >= this.Networking.PlayerCount) {
            this.EndGame();
        }
    }

    /// <summary>
    /// This will end the game.
    /// </summary>
    private void EndGame() {
        this.Difficulty.EndGame();
        this.Enemies.EndGame();
        this.Towers.EndGame();

        this.PlayerFunds.Clear();
    }
}