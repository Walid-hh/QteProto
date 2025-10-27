using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TurnManager : Node
{
	private List<ICombatant> combatantTurnOrder;
	private ICombatant currentCombatant;
	private BattleContext battleContext;
	private int currentTurnIndex;
	private bool turnCycleActive;

	public event Action<ICombatant, IReadOnlyList<ICombatCommand>> CommandsAvailable;
	public event Action<ICombatant> TurnStarted;
	public event Action<ICombatant> TurnEnded;

	public override void _Ready()
	{
		combatantTurnOrder = new List<ICombatant>();
	}

	public void SetBattleContext(BattleContext context)
	{
		if (battleContext != null)
		{
			battleContext.RosterChanged -= HandleBattleRosterChanged;
			battleContext.MenuStateChanged -= HandleMenuStateChanged;
		}

		battleContext = context;

		if (battleContext != null)
		{
			battleContext.RosterChanged += HandleBattleRosterChanged;
			battleContext.MenuStateChanged += HandleMenuStateChanged;
			UpdateTurnOrder();
		}
		else
		{
			combatantTurnOrder = new List<ICombatant>();
		}
	}

	public void RegisterCombatant(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before registering combatants.");
			return;
		}

		if (!battleContext.RegisterCombatant(combatant))
		{
			return;
		}

		UpdateTurnOrder();
	}

	public void UnregisterCombatant(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before unregistering combatants.");
			return;
		}

		if (!battleContext.UnregisterCombatant(combatant))
		{
			return;
		}

		UpdateTurnOrder();
	}

	public void StartTurnCycle()
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before starting turns.");
			return;
		}

		UpdateTurnOrder();
		if (combatantTurnOrder.Count == 0)
		{
			GD.Print("TurnManager: No combatants available to start turn cycle.");
			return;
		}

		turnCycleActive = true;
		currentTurnIndex = 0;
		StartCurrentTurn();
	}

	public void NextTurn()
	{
		if (!turnCycleActive)
		{
			StartTurnCycle();
			return;
		}

		EndCurrentTurn();
	}

	public void TurnStart()
	{
		if (!turnCycleActive)
		{
			StartTurnCycle();
		}
	}

	public void ExecuteCommand(ICombatCommand command, ICombatant target)
	{
		if (command == null || battleContext == null)
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
			GD.Print("TurnManager: Command execution failed or was invalid for the current context.");
			return;
		}

		if (command.EndsTurn)
		{
			EndCurrentTurn();
		}
	}

	public override void _Process(double delta)
	{
	}

	private void StartCurrentTurn()
	{
		if (!turnCycleActive || combatantTurnOrder.Count == 0)
		{
			return;
		}

		if (battleContext == null)
		{
			GD.PrintErr("TurnManager cannot start a turn without a BattleContext.");
			return;
		}

		if (currentTurnIndex >= combatantTurnOrder.Count)
		{
			currentTurnIndex = 0;
		}

		currentCombatant = combatantTurnOrder[currentTurnIndex];

		if (currentCombatant == null || !currentCombatant.IsAlive())
		{
			battleContext?.ResetTurnState();
			EndCurrentTurn();
			return;
		}

		TurnStarted?.Invoke(currentCombatant);

		if (battleContext.ActionResolver == null)
		{
			GD.PrintErr("TurnManager requires an ActionResolver before starting turns.");
			currentCombatant.TakeTurn();
			EndCurrentTurn();
			return;
		}

		battleContext.ActiveActor = currentCombatant;
		battleContext.SelectedTarget = null;

		if (currentCombatant is ICommandSource commandSource)
		{
			var commands = commandSource.GetAvailableCommands(battleContext) ?? Array.Empty<ICombatCommand>();
			CommandsAvailable?.Invoke(currentCombatant, commands);
		}
		else
		{
			currentCombatant.TakeTurn();
			EndCurrentTurn();
		}
	}

	private void EndCurrentTurn()
	{
		TurnEnded?.Invoke(currentCombatant);

		battleContext?.ResetTurnState();
		currentCombatant = null;

		UpdateTurnOrder();

		if (!turnCycleActive || combatantTurnOrder.Count == 0)
		{
			turnCycleActive = false;
			return;
		}

		currentTurnIndex++;
		if (currentTurnIndex >= combatantTurnOrder.Count)
		{
			currentTurnIndex = 0;
		}

		StartCurrentTurn();
	}

	public void UpdateTurnOrder()
	{
		if (battleContext == null)
		{
			combatantTurnOrder = new List<ICombatant>();
			return;
		}

		combatantTurnOrder = battleContext.Combatants
			.Where(combatant => combatant != null && combatant.IsAlive())
			.OrderByDescending(combatant => combatant.Speed)
			.ToList();
	}

	private void HandleBattleRosterChanged()
	{
		UpdateTurnOrder();

		if (!turnCycleActive || combatantTurnOrder.Count != 0)
		{
			return;
		}

		turnCycleActive = false;
		currentCombatant = null;
	}

	private void HandleMenuStateChanged(BattleMenuState state)
	{
		if (battleContext == null || currentCombatant == null)
		{
			return;
		}

		if (currentCombatant is ICommandSource commandSource)
		{
			var commands = commandSource.GetAvailableCommands(battleContext) ?? Array.Empty<ICombatCommand>();
			CommandsAvailable?.Invoke(currentCombatant, commands);
		}
	}
}
