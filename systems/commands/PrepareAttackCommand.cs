using Godot;

public class PrepareAttackCommand : ICombatCommand
{
	public string Id { get; } = "action.prepare_attack";
	public string DisplayName { get; } = "Attack";
	public bool EndsTurn => false;

	private bool executed;

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		if (context.ActiveActor == null)
		{
			return false;
		}

		return context.CurrentMenuState == BattleMenuState.ActionMenu;
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print("PrepareAttackCommand cannot execute in the current context.");
			return;
		}

		context.PendingAction = new AttackCommand();
		context.SelectedTarget = null;
		context.PushMenuState(BattleMenuState.TargetSelection);
		executed = true;
	}

	public bool CanUndo(BattleContext context)
	{
		return executed && context != null && context.CurrentMenuState == BattleMenuState.TargetSelection;
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
	}
}
