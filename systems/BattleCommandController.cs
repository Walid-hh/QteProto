using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleCommandController : Node
{
	private BattleContext battleContext;
	private ICombatant activeCombatant;
	private bool selectionActive;

	public event Action<ICombatant, IReadOnlyList<ICombatCommand>> CommandsAvailable;
	public event Action<ICombatCommand> TurnEndingCommandExecuted;

	public override void _Ready()
	{
	}

	public void SetBattleContext(BattleContext context)
	{
		if (battleContext != null)
		{
			battleContext.MenuStateChanged -= HandleMenuStateChanged;
		}

		battleContext = context;

		if (battleContext != null)
		{
			battleContext.MenuStateChanged += HandleMenuStateChanged;
		}
		else
		{
			EndCommandPhase();
		}
	}

	public void BeginCommandPhase(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("BattleCommandController requires a BattleContext before beginning command selection.");
			return;
		}

		activeCombatant = combatant;
		selectionActive = true;
		PublishAvailableCommands();
	}

	public void EndCommandPhase()
	{
		selectionActive = false;
		if (activeCombatant != null)
		{
			PublishClearedCommands(activeCombatant);
		}

		activeCombatant = null;
	}

	public void ExecuteCommand(ICombatCommand command, ICombatant target = null)
	{
		if (!selectionActive || command == null || battleContext == null)
		{
			return;
		}

		if (target != null)
		{
			battleContext.SelectedTarget = target;
		}

		bool executed = battleContext.CommandManager.ExecuteCommand(command, battleContext);
		if (!executed)
		{
			GD.Print("BattleCommandController: Command execution failed or was invalid for the current context.");
			return;
		}

		if (command.EndsTurn)
		{
			selectionActive = false;
			PublishClearedCommands(activeCombatant);
			TurnEndingCommandExecuted?.Invoke(command);
		}
		else
		{
			PublishAvailableCommands();
		}
	}

	public bool UndoLastCommand()
	{
		if (!selectionActive || battleContext == null)
		{
			return false;
		}

		bool undone = battleContext.UndoLastCommand();
		if (undone)
		{
			PublishAvailableCommands();
		}

		return undone;
	}

	private void HandleMenuStateChanged(BattleMenuState state)
	{
		if (!selectionActive || battleContext == null || activeCombatant == null)
		{
			return;
		}

		PublishAvailableCommands();
	}

	private void PublishAvailableCommands()
	{
		if (!selectionActive || battleContext == null || activeCombatant == null)
		{
			PublishClearedCommands(activeCombatant);
			return;
		}

		if (activeCombatant is ICommandSource commandSource)
		{
			var commands = commandSource.GetAvailableCommands(battleContext) ?? Array.Empty<ICombatCommand>();
			var filteredCommands = FilterCommandsForCombatant(activeCombatant, commands);
			if (filteredCommands.Count == 0)
			{
				PublishClearedCommands(activeCombatant);
			}
			else
			{
				CommandsAvailable?.Invoke(activeCombatant, filteredCommands);
			}
		}
		else
		{
			PublishClearedCommands(activeCombatant);
		}
	}

	private void PublishClearedCommands(ICombatant combatant = null)
	{
		if (CommandsAvailable != null)
		{
			CommandsAvailable.Invoke(combatant, Array.Empty<ICombatCommand>());
		}
	}

	private IReadOnlyList<ICombatCommand> FilterCommandsForCombatant(ICombatant combatant, IReadOnlyList<ICombatCommand> commands)
	{
		if (combatant == null || commands == null)
		{
			return Array.Empty<ICombatCommand>();
		}

		if (combatant.Side == BattleSide.Enemy)
		{
			var allowed = commands.Where(IsEnemyCommandAllowed).ToList();
			if (allowed.Count == 0)
			{
				allowed.Add(new AttackCommand());
			}

			return allowed;
		}

		return commands;
	}

	private static bool IsEnemyCommandAllowed(ICombatCommand command)
	{
		return command is AttackCommand || command is WeaponAttackCommand;
	}
}
