class_name ActionManager extends Node

func resolve_command(command_type: int, actor: Node, target: Node) -> void:
    match command_type:
        0:
            if actor.is_in_group("actor"):
                resolve_player_attack(actor, target)
            elif actor.is_in_group("Enemy"):
                resolve_enemy_attack(actor, target)
        1:
            resolve_weapon_use(actor, actor.get("equipped_weapon"), target)
        2:
            resolve_item_use(actor, actor.get("selected_item"), target)
        3:
            resolve_run_attempt(actor)
   

func resolve_player_attack(actor: Node, target: Node) -> void:
    # Placeholder for actor attack logic
    print(actor.name + " attacks " + target.name)

func resolve_enemy_attack(actor: Node, target: Node) -> void:
    # Placeholder for enemy attack logic
    print(actor.name + " attacks " + target.name)

func resolve_item_use(actor: Node, item: Node, target: Node) -> void:
    # Placeholder for item use logic
    print(actor.name + " uses " + item.resource_name + " on " + target.name)

func resolve_weapon_use(actor: Node, weapon: Node, target: Node) -> void:
    # Placeholder for weapon use logic
    print(actor.name + " uses " + weapon.resource_name + " on " + target.name)

func resolve_run_attempt(actor: Node) -> void:
    # Placeholder for run attempt logic
    print(actor.name + " attempts to run away!")