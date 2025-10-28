using System.Collections.Generic;

public class BattleCommandManager
{
	private readonly Stack<ICombatCommand> undoStack = new();

	public bool ExecuteCommand(ICombatCommand command, BattleContext context)
	{
		if (command == null || context == null)
		{
			return false;
		}

		if (!command.CanExecute(context))
		{
			return false;
		}

		command.Execute(context);

		if (command.CanUndo(context))
		{
			undoStack.Push(command);
		}

		return true;
	}

	public bool UndoLastCommand(BattleContext context)
	{
		if (context == null || undoStack.Count == 0)
		{
			return false;
		}

		var command = undoStack.Pop();
		if (!command.CanUndo(context))
		{
			return false;
		}

		command.Undo(context);
		return true;
	}

	public void ClearHistory()
	{
		undoStack.Clear();
	}
}
