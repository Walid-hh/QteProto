using Godot;

public class OpenMenuCommand : ICombatCommand
{
	private readonly BattleMenuState targetState;
	private bool executed;

	public OpenMenuCommand(string id, string displayName, BattleMenuState targetState)
	{
		Id = id;
		DisplayName = displayName;
		this.targetState = targetState;
	}

	public string Id { get; }
	public string DisplayName { get; }
	public bool EndsTurn => false;
	public BattleMenuState TargetState => targetState;

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		if (targetState == BattleMenuState.None)
		{
			return false;
		}

		return context.CurrentMenuState != targetState;
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print($"OpenMenuCommand cannot open {targetState} from the current state.");
			return;
		}

		context.PushMenuState(targetState);
		executed = true;
	}

	public bool CanUndo(BattleContext context)
	{
		if (!executed || context == null)
		{
			return false;
		}

		return context.CurrentMenuState == targetState && context.CanPopMenuState;
	}

	public void Undo(BattleContext context)
	{
		if (!CanUndo(context))
		{
			return;
		}

		context.PopMenuState();
		executed = false;
	}
}
