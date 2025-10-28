using Godot;

public class SelectWeaponCommand : ICombatCommand
{
	private WeaponAttackCommand preparedAttack;
	private bool executed;

	public SelectWeaponCommand(string weaponId, string displayName, float powerMultiplier)
	{
		WeaponId = string.IsNullOrEmpty(weaponId) ? "weapon.unknown" : weaponId;
		DisplayName = string.IsNullOrEmpty(displayName) ? "Weapon" : displayName;
		PowerMultiplier = powerMultiplier <= 0 ? 1.0f : powerMultiplier;
		Id = $"menu.weapon.{WeaponId}";
	}

	public string Id { get; }
	public string DisplayName { get; }
	public bool EndsTurn => false;
	public string WeaponId { get; }
	public float PowerMultiplier { get; }

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		if (context.CurrentMenuState != BattleMenuState.WeaponMenu)
		{
			return false;
		}

		return context.ActiveActor != null;
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print($"SelectWeaponCommand cannot execute from state {context?.CurrentMenuState}.");
			return;
		}

		preparedAttack = new WeaponAttackCommand(WeaponId, DisplayName, PowerMultiplier);
		context.PendingAction = preparedAttack;
		context.SelectedTarget = null;
		context.PushMenuState(BattleMenuState.TargetSelection);
		executed = true;
	}

	public bool CanUndo(BattleContext context)
	{
		if (!executed || context == null)
		{
			return false;
		}

		return context.CurrentMenuState == BattleMenuState.TargetSelection;
	}

	public void Undo(BattleContext context)
	{
		if (!CanUndo(context))
		{
			return;
		}

		context.PendingAction = null;
		context.SelectedTarget = null;
		context.PopMenuState();
		executed = false;
		preparedAttack = null;
	}
}
