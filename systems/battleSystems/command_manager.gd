class_name CommandManager extends Node

signal command_executed(command_type: int, user: Node, target: Node)
enum CommandType {
    ATTACK,
    WEAPON,
    ITEM,
    RUN
}

func execute_command(command_type: CommandType, user: Node, target: Node) -> void:
    if !CommandType.has(command_type):
        print("Invalid command type: " + str(command_type))
        return
    elif !user.is_in_group("Player") and !user.is_in_group("Enemy"):
        print("User is not a valid participant in battle." + user.name)
        return
    elif !target.is_in_group("Player") and !target.is_in_group("Enemy"):
        print("Target is not a valid participant in battle." + target.name)
        return
    command_executed.emit(command_type, user, target)
