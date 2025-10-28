using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleContext
{
	private const int MaxPlayerCombatants = 2;
	private const int MaxMobCombatants = 3;

	private readonly List<ICombatant> combatants = new();
	private readonly List<ICombatant> playerCombatants = new();
	private readonly List<ICombatant> mobCombatants = new();
	private readonly Stack<BattleMenuState> menuStateStack = new();
	private bool suppressNotifications;

	public BattleContext(BattleManager manager)
	{
		BattleManager = manager ?? throw new ArgumentNullException(nameof(manager));
		CommandManager = new BattleCommandManager();
		InitializeMenuState(BattleMenuState.ActionMenu);
	}

	public BattleManager BattleManager { get; }
	public ActionResolver ActionResolver { get; private set; }
	public TurnManager TurnManager { get; private set; }
	public UIController UIController { get; private set; }
	public BattleCommandManager CommandManager { get; }

	public IReadOnlyList<ICombatant> Combatants => combatants;
	public IReadOnlyList<ICombatant> PlayerCombatants => playerCombatants;
	public IReadOnlyList<ICombatant> MobCombatants => mobCombatants;

	public ICombatant ActiveActor { get; internal set; }
	public ICombatant SelectedTarget { get; set; }
	public ICombatCommand PendingAction { get; internal set; }
	public ICombatCommand CommandToResolve { get; internal set; }
	public BattleMenuState CurrentMenuState => menuStateStack.Count == 0 ? BattleMenuState.None : menuStateStack.Peek();
	public bool CanPopMenuState => menuStateStack.Count > 1;

	public event Action<ICombatant> CombatantRegistered;
	public event Action<ICombatant> CombatantUnregistered;
	public event Action RosterChanged;
	public event Action<BattleMenuState> MenuStateChanged;

	public void ConfigureManagers(ActionResolver actionResolver, TurnManager turnManager, UIController uiController)
	{
		ActionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
		TurnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
		UIController = uiController ?? throw new ArgumentNullException(nameof(uiController));
	}

	public bool RegisterCombatant(ICombatant combatant)
	{
		if (!RegisterCombatantInternal(combatant))
		{
			return false;
		}

		NotifyCombatantRegistered(combatant);
		NotifyRosterChanged();
		return true;
	}

	public bool UnregisterCombatant(ICombatant combatant)
	{
		if (!UnregisterCombatantInternal(combatant))
		{
			return false;
		}

		NotifyCombatantUnregistered(combatant);
		NotifyRosterChanged();
		return true;
	}

	public void SetCombatants(IEnumerable<ICombatant> players, IEnumerable<ICombatant> mobs)
	{
		var previousRoster = combatants.ToList();

		suppressNotifications = true;
		try
		{
			combatants.Clear();
			playerCombatants.Clear();
			mobCombatants.Clear();

			if (players != null)
			{
				foreach (var combatant in players)
				{
					if (combatant == null)
					{
						continue;
					}

					combatant.SetBattleSide(BattleSide.Player);
					RegisterCombatantInternal(combatant);
				}
			}

			if (mobs != null)
			{
				foreach (var combatant in mobs)
				{
					if (combatant == null)
					{
						continue;
					}

					combatant.SetBattleSide(BattleSide.Enemy);
					RegisterCombatantInternal(combatant);
				}
			}
		}
		finally
		{
			suppressNotifications = false;
		}

		var newRoster = combatants.ToList();
		var removed = previousRoster.Except(newRoster).ToList();
		var added = newRoster.Except(previousRoster).ToList();

		foreach (var combatant in removed)
		{
			NotifyCombatantUnregistered(combatant, raiseRosterChanged: false);
		}

		foreach (var combatant in added)
		{
			NotifyCombatantRegistered(combatant, raiseRosterChanged: false);
		}

		NotifyRosterChanged();
		CommandManager.ClearHistory();
		PendingAction = null;
		CommandToResolve = null;
		InitializeMenuState(BattleMenuState.ActionMenu);
	}

	public bool ExecuteCommand(ICombatCommand command)
	{
		return CommandManager.ExecuteCommand(command, this);
	}

	public bool UndoLastCommand()
	{
		return CommandManager.UndoLastCommand(this);
	}

	public void ResetTurnState()
	{
		ActiveActor = null;
		SelectedTarget = null;
		PendingAction = null;
		CommandToResolve = null;
		CommandManager.ClearHistory();
		InitializeMenuState(BattleMenuState.ActionMenu);
	}

	private bool RegisterCombatantInternal(ICombatant combatant)
	{
		if (combatant == null)
		{
			return false;
		}

		if (combatants.Contains(combatant))
		{
			CategorizeCombatant(combatant, enforceCapacity: true);
			return false;
		}

		if (IsSideAtCapacity(combatant.Side))
		{
			PrintCapacityWarning(combatant);
			return false;
		}

		combatants.Add(combatant);
		CategorizeCombatant(combatant, enforceCapacity: false);
		return true;
	}

	private bool UnregisterCombatantInternal(ICombatant combatant)
	{
		if (combatant == null)
		{
			return false;
		}

		var removed = combatants.Remove(combatant);
		playerCombatants.Remove(combatant);
		mobCombatants.Remove(combatant);
		return removed;
	}

	private void CategorizeCombatant(ICombatant combatant, bool enforceCapacity)
	{
		if (combatant == null)
		{
			return;
		}

		switch (combatant.Side)
		{
			case BattleSide.Player:
				if (playerCombatants.Contains(combatant))
				{
					mobCombatants.Remove(combatant);
					break;
				}

				if (enforceCapacity && IsSideAtCapacity(BattleSide.Player))
				{
					PrintCapacityWarning(combatant);
					break;
				}

				if (!playerCombatants.Contains(combatant))
				{
					playerCombatants.Add(combatant);
				}
				mobCombatants.Remove(combatant);
				break;
			case BattleSide.Enemy:
				if (mobCombatants.Contains(combatant))
				{
					playerCombatants.Remove(combatant);
					break;
				}

				if (enforceCapacity && IsSideAtCapacity(BattleSide.Enemy))
				{
					PrintCapacityWarning(combatant);
					break;
				}

				if (!mobCombatants.Contains(combatant))
				{
					mobCombatants.Add(combatant);
				}
				playerCombatants.Remove(combatant);
				break;
			default:
				playerCombatants.Remove(combatant);
				mobCombatants.Remove(combatant);
				break;
		}
	}

	private void NotifyCombatantRegistered(ICombatant combatant, bool raiseRosterChanged = true)
	{
		if (combatant == null || suppressNotifications)
		{
			return;
		}

		CombatantRegistered?.Invoke(combatant);
		if (raiseRosterChanged)
		{
			NotifyRosterChanged();
		}
	}

	private void NotifyCombatantUnregistered(ICombatant combatant, bool raiseRosterChanged = true)
	{
		if (combatant == null || suppressNotifications)
		{
			return;
		}

		CombatantUnregistered?.Invoke(combatant);
		if (raiseRosterChanged)
		{
			NotifyRosterChanged();
		}
	}

	private void NotifyRosterChanged()
	{
		if (suppressNotifications)
		{
			return;
		}

		RosterChanged?.Invoke();
	}

	public void InitializeMenuState(BattleMenuState initialState = BattleMenuState.ActionMenu)
	{
		menuStateStack.Clear();
		if (initialState != BattleMenuState.None)
		{
			menuStateStack.Push(initialState);
		}

		NotifyMenuStateChanged();
	}

	internal void PushMenuState(BattleMenuState menuState)
	{
		if (menuState == BattleMenuState.None)
		{
			return;
		}

		if (menuStateStack.Count > 0 && menuStateStack.Peek() == menuState)
		{
			return;
		}

		menuStateStack.Push(menuState);
		NotifyMenuStateChanged();
	}

	internal bool PopMenuState()
	{
		if (menuStateStack.Count <= 1)
		{
			return false;
		}

		menuStateStack.Pop();
		NotifyMenuStateChanged();
		return true;
	}

	private void NotifyMenuStateChanged()
	{
		MenuStateChanged?.Invoke(CurrentMenuState);
	}

	private bool IsSideAtCapacity(BattleSide side)
	{
		return side switch
		{
			BattleSide.Player => playerCombatants.Count >= MaxPlayerCombatants,
			BattleSide.Enemy => mobCombatants.Count >= MaxMobCombatants,
			_ => false
		};
	}

	private static string GetCombatantLabel(ICombatant combatant)
	{
		return string.IsNullOrEmpty(combatant?.CombatantName) ? "combatant" : combatant.CombatantName;
	}

	private void PrintCapacityWarning(ICombatant combatant)
	{
		if (combatant == null)
		{
			return;
		}

		string sideLabel = combatant.Side switch
		{
			BattleSide.Player => "player",
			BattleSide.Enemy => "enemy",
			_ => "neutral"
		};

		GD.PrintErr($"BattleContext: Cannot add {GetCombatantLabel(combatant)} to {sideLabel} side; capacity reached.");
	}

	public bool HasLivingPlayers()
	{
		return playerCombatants.Any(combatant => combatant != null && combatant.IsAlive());
	}

	public bool HasLivingEnemies()
	{
		return mobCombatants.Any(combatant => combatant != null && combatant.IsAlive());
	}

	public BattleOutcome EvaluateBattleOutcome()
	{
		bool playersAlive = HasLivingPlayers();
		bool enemiesAlive = HasLivingEnemies();

		if (!playersAlive && !enemiesAlive)
		{
			return BattleOutcome.Draw;
		}

		if (!playersAlive)
		{
			return BattleOutcome.Defeat;
		}

		if (!enemiesAlive)
		{
			return BattleOutcome.Victory;
		}

		return BattleOutcome.Ongoing;
	}

	public bool TryGetBattleOutcome(out BattleOutcome outcome)
	{
		outcome = EvaluateBattleOutcome();
		return outcome != BattleOutcome.Ongoing;
	}
}
