class_name TurnManager extends Node

signal turn_started(current_actor: Node)
var line_up_turn_order := []
var current_actor_turn : Node = null
var turn_index: int = 0

func initialize_turns(actors_in_battle: Array[Node]) -> void :
    for actor in actors_in_battle:
        if !actor.is_in_group("Player") or !actor.is_in_group("Enemy"):
            print("Actor is not a valid participant in battle." + actor.name)
            return
    line_up_turn_order = actors_in_battle.duplicate()
    line_up_turn_order.sort_custom(sort_descending)
    current_actor_turn = line_up_turn_order[turn_index]
    turn_started.emit(current_actor_turn)

func sort_descending(a: Node, b: Node) -> bool:
    return a.speed > b.speed
    
func next_turn() -> void:
    turn_index += 1
    if line_up_turn_order[turn_index] == null:
        turn_index = 0
        initialize_turns(line_up_turn_order)
