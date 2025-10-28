using Godot;
using System;
using System.Collections.Generic;

public partial class UIController : Node
{
	private BattleCommandController commandController;
	private bool playerMenuVisible;

	public event Action<ICombatant, IReadOnlyList<ICombatCommand>> PlayerCommandsUpdated;
	public event Action PlayerMenuHidden;

	public void SetCommandController(BattleCommandController controller)
	{
		if (commandController != null)
		{
			commandController.CommandsAvailable -= HandleCommandsAvailable;
		}

		commandController = controller;

		if (commandController != null)
		{
			commandController.CommandsAvailable += HandleCommandsAvailable;
		}

		HidePlayerMenu();
	}

	private void HandleCommandsAvailable(ICombatant combatant, IReadOnlyList<ICombatCommand> commands)
	{
		if (combatant == null || combatant.Side != BattleSide.Player)
		{
			HidePlayerMenu();
			return;
		}

		ShowPlayerMenu(combatant, commands ?? Array.Empty<ICombatCommand>());
	}

	private void ShowPlayerMenu(ICombatant combatant, IReadOnlyList<ICombatCommand> commands)
	{
		playerMenuVisible = true;
		PlayerCommandsUpdated?.Invoke(combatant, commands);
	}

	private void HidePlayerMenu()
	{
		playerMenuVisible = false;
		PlayerMenuHidden?.Invoke();
	}
}
