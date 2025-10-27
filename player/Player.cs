using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Player : Node, ICombatant, ICommandSource
{
	public string CombatantName { get; set; }
	public int Health { get; set; }
	public int Attack { get; private set; }
	public int Speed { get; private set; }
	public int Defense { get; private set; }
	public int Luck { get; private set; }
	public CombatantStats CombatantStats { get; private set; }
	public BattleSide Side { get; private set; } = BattleSide.Player;

	private List<ICombatCommand> commands;
	private List<ICombatCommand> targetSelectionCommands;

	public Player(CombatantStats stats , string name)
	{
		CombatantName = name;
		if (stats != null)
		{
			InitializeFromStats(stats);
		}
	}
	public void SetBattleSide(BattleSide side)
	{
		Side = side;
	}

	public override void _Ready()
	{
		if (commands == null)
		{
			commands = new List<ICombatCommand>();
		}

		if (targetSelectionCommands == null)
		{
			targetSelectionCommands = new List<ICombatCommand>();
		}

		if (CombatantStats != null && commands.Count == 0)
		{
		InitializeFromStats(CombatantStats);
	}

	EnsureDefaultCommands();
	EnsureTargetSelectionCommands();
	}

	public void SetCombatantStats(CombatantStats stats)
	{
		if (stats == null)
		{
			return;
		}

		InitializeFromStats(stats);
	}

	public void TakeDamage(int amount)
	{
		Health -= amount;
		if (Health < 0)
		{
			Health = 0;
		}
	}

	public void TakeTurn()
	{
		// Default behaviour if no UI-driven commands are available.
	}

	public bool IsAlive()
	{
		return Health > 0;
	}

	public IReadOnlyList<ICombatCommand> GetAvailableCommands(BattleContext context)
	{
		if (context == null)
		{
			return commands;
		}

		return context.CurrentMenuState switch
		{
			BattleMenuState.TargetSelection => targetSelectionCommands,
			_ => commands
		};
	}

	private void InitializeFromStats(CombatantStats stats)
	{
		CombatantStats = stats;
		Health = stats.health;
		Speed = stats.speed;
		Defense = stats.defense;
		Attack = stats.attack;
		Luck = stats.luck;

		if (commands == null)
		{
			commands = new List<ICombatCommand>();
		}

		if (targetSelectionCommands == null)
		{
			targetSelectionCommands = new List<ICombatCommand>();
		}

		EnsureDefaultCommands();
		EnsureTargetSelectionCommands();
	}

	private void EnsureDefaultCommands()
	{
		commands.RemoveAll(command => command is AttackCommand);

		if (!commands.Any(command => command is PrepareAttackCommand))
		{
			commands.Add(new PrepareAttackCommand());
		}

		if (!commands.Any(command => command.Id == "menu.items"))
		{
			commands.Add(new OpenMenuCommand("menu.items", "Items", BattleMenuState.ItemMenu));
		}
	}

	private void EnsureTargetSelectionCommands()
	{
		if (!targetSelectionCommands.Any(command => command is ConfirmPendingActionCommand))
		{
			targetSelectionCommands.Add(new ConfirmPendingActionCommand());
		}
	}
}
