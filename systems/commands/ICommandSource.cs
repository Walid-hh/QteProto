using System.Collections.Generic;

public interface ICommandSource
{
	IReadOnlyList<ICombatCommand> GetAvailableCommands(BattleContext context);
}
