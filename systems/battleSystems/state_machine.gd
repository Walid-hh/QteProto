class_name PlayerFSM extends Node

enum Events {
	NONE,
	FINISHED,

}

class StateMachine extends Node:

	var is_debugging := false : set = set_is_debugging
	var transitions := {}: set = set_transitions
	var current_state: State

	func _ready() -> void:
		set_physics_process(false)

	func set_is_debugging(new_value : bool) -> void :
		is_debugging = new_value
		if (
			current_state != null and
			current_state.player != null and 
			current_state.player.debug_label != null
		) :
			current_state.player.debug_label.text = current_state.name
			current_state.player.debug_label.visible = is_debugging

	func set_transitions(new_transitions: Dictionary) -> void:
		transitions = new_transitions
		if OS.is_debug_build():
			for state: State in transitions:
				assert(
					state is State,
					"Invalid state in the transitions dictionary. " +
					"Expected a State object, but got " + str(state)
				)
				for event: Events in transitions[state]:
					assert(
						event is Events,
						"Invalid event in the transitions dictionary. " +
						"Expected an Events object, but got " + str(event)
					)
					assert(
						transitions[state][event] is State,
						"Invalid state in the transitions dictionary. " +
						"Expected a State object, but got " +
						str(transitions[state][event])
					)

	func activate(initial_state: State = null) -> void:
		if initial_state != null:
			current_state = initial_state
		assert(
			current_state != null,
			"Activated the state machine but the state variable is null. " +
			"Please assign a starting state to the state machine."
		)
		current_state.finished.connect(_on_state_finished.bind(current_state))
		current_state.enter()
		set_physics_process(true)

	func _physics_process(delta: float) -> void:
		var event := current_state.update(delta)
		if event == Events.NONE:
			return
		trigger_event(event)

	func trigger_event(event: Events) -> void:
		if not current_state in transitions:
			return
		if current_state.has_method("should_ignore_event") and current_state.should_ignore_event(event):
			return
		if not transitions[current_state].has(event):
			print_debug(
				"Trying to trigger event " + Events.keys()[event] +
				" from state " + current_state.name +
				" but the transition does not exist."
			)
			return
		var next_state = transitions[current_state][event]
		_transition(next_state)

	func _transition(new_state: State) -> void:
		current_state.exit()
		current_state.finished.disconnect(_on_state_finished)
		current_state = new_state
		current_state.finished.connect(_on_state_finished.bind(current_state))
		current_state.enter()
		if is_debugging and current_state.player.debug_label != null :
			current_state.player.debug_label.text = current_state.name

	func _on_state_finished(finished_state: State) -> void:
		assert(
			Events.FINISHED in transitions[finished_state],
			"Received a state that does not have a transition for the FINISHED event, " + current_state.name + ". " +
			"Add a transition for this event in the transitions dictionary."
		)
		_transition(transitions[finished_state][Events.FINISHED])
	
	func add_transition_to_all_states(event: Events, end_state: State) -> void :
		for state in transitions:
			if state == end_state:
				continue
			transitions[state][event] = end_state

class State extends RefCounted:

	## Emitted when the state completes and the state machine should transition to the next state.
	## Use this for time-based states or moves that have a fixed duration.
	signal finished

	## Display name of the state, for debugging purposes.
	var name := "State"
	## Reference to the player that the state controls.
	var player: Node = null


	func _init(init_name: String, init_player: Node) -> void:
		name = init_name
		player = init_player


	## Called by the state machine on the engine's physics update tick.
	## Returns an event that the state machine can use to transition to the next state.
	## If there is no event, return [constant AI.Events.None]
	func update(_delta: float) -> Events:
		return Events.NONE


	## Called by the state machine upon changing the active state.
	func enter() -> void:
		pass


	## Called by the state machine before changing the active state. Use this function
	## to clean up the state.
	func exit() -> void:
		pass
	


