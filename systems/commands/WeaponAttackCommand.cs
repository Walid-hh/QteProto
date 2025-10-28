using Godot;

public class WeaponAttackCommand : ICombatCommand
{
	private readonly float powerMultiplier;

	public WeaponAttackCommand(string weaponId, string displayName, float powerMultiplier)
	{
		WeaponId = string.IsNullOrEmpty(weaponId) ? "weapon.unknown" : weaponId;
		DisplayName = string.IsNullOrEmpty(displayName) ? "Weapon Attack" : displayName;
		Id = $"action.weapon.{WeaponId}";
		this.powerMultiplier = powerMultiplier <= 0 ? 1.0f : powerMultiplier;
	}

	public string Id { get; }
	public string DisplayName { get; }
	public bool EndsTurn => true;
	public string WeaponId { get; }
	public float PowerMultiplier => powerMultiplier;

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		if (context.ActiveActor == null || context.ActionResolver == null)
		{
			return false;
		}

		if (context.SelectedTarget == null || !context.SelectedTarget.IsAlive())
		{
			return false;
		}

		return true;
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print("WeaponAttackCommand cannot execute without a valid attacker, target, and action resolver.");
			return;
		}

		context.ActionResolver.ResolveWeaponAttack(context.ActiveActor, context.SelectedTarget, DisplayName, powerMultiplier);
	}

	public bool CanUndo(BattleContext context)
	{
		return false;
	}

	public void Undo(BattleContext context)
	{
	}
}
