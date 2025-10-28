using Godot;
using System.Collections.Generic;
using System.Diagnostics;


public partial class StateMachine : Node
{
	public enum Events
	{
		NONE,
		FINISHED,
		TURN_HANDLE,
		BATTLE_END,
	}

	public abstract partial class State : RefCounted
	{
		[Signal]
		public delegate void FinishedEventHandler();

		public FinishedEventHandler handleStateFinished;
		public string Name { get; }
		public GodotObject ContextObject { get; }
		public Node ContextNode => ContextObject as Node;

		protected State(string name, GodotObject context)
		{
			ContextObject = context;
			Name = name;
		}

		protected TContext GetContext<TContext>() where TContext : GodotObject
		{
			return (TContext)ContextObject;
		}

		public virtual void Enter()
		{
		}

		public virtual void Exit()
		{
		}

		public virtual Events Update(double delta) { return Events.NONE; }
	}

	public abstract partial class TypedState<TContext> : State where TContext : GodotObject
	{
		protected TypedState(string name, TContext context) : base(name, context)
		{
		}

		public TContext Context => GetContext<TContext>();
	}

	private bool _isDebugging;

	private Dictionary<State, Dictionary<Events, State>> _transitions = new();

	public State currentState;


	public override void _Ready()
	{
		SetPhysicsProcess(false);
	}

	public bool IsDebugging
	{
		get => _isDebugging;
		set
		{
			_isDebugging = value;
			if (currentState != null && currentState.ContextNode != null && currentState.ContextNode.HasNode("debugLabel"))
			{
				currentState.ContextNode.GetNode<Label>("debugLabel").Text = currentState.Name;
				currentState.ContextNode.GetNode<Label>("debugLabel").Visible = _isDebugging;
			}
		}
	}

	public Dictionary<State, Dictionary<Events, State>> Transitions
	{
		get => _transitions;
		set
		{
			_transitions = value;
			if (OS.IsDebugBuild())
			{
				foreach (State state in _transitions.Keys)
				{
					Debug.Assert(state is State,
					"Invalid state in the transitions dictionary. " + "Expected State Object but got " + state.ToString());
					foreach (Events evt in _transitions[state].Keys)
					{
						Debug.Assert(evt is Events,
						"Invalid event in the transitions dictionary. " + "Expected Event Object but got " + evt.ToString());
						Debug.Assert(_transitions[state][evt] is State,
						"Invalid transition in the transitions dictionary. " + "Expected State Object but got " + _transitions[state][evt].ToString());
					}

				}

			}

		}
	}

	public void Activate(State initialState = null)
	{
		if (initialState != null)
		{
			currentState = initialState;
		}
		Debug.Assert(currentState != null,
		"Activated the state machine but the state variable is null. " +
		"Please assign a starting state to the state machine.");
		currentState.Enter();
		currentState.handleStateFinished = () => _OnStateFinished(currentState);
		currentState.Finished += currentState.handleStateFinished;
		SetPhysicsProcess(true);
	}

	public override void _PhysicsProcess(double delta)
	{
		Events evt = currentState.Update(delta);
		if (evt == Events.NONE)
		{
			return;
		}
		TriggerEvent(evt);
	}

	public void TriggerEvent(Events evt)
	{
		if (currentState == null)
		{
			return;
		}
		if (_transitions.ContainsKey(currentState) && _transitions[currentState].ContainsKey(evt))
		{
			State nextState = _transitions[currentState][evt];
			if (nextState == null)
			{
				GD.Print("Trying to trigger event " + evt +
				" from state " + currentState.Name +
				" but the transition does not exist.");
				return;

			}
			_Transition(nextState);

		}
	}
	public void _Transition(State nextState)
	{
		if (currentState == null)
		{
			return;
		}
		currentState.Exit();
		currentState.Finished -= currentState.handleStateFinished;

		currentState = nextState;
		currentState.handleStateFinished = () => _OnStateFinished(currentState);
		currentState.Finished += currentState.handleStateFinished;
		currentState.Enter();

		if (_isDebugging && currentState.ContextNode != null && currentState.ContextNode.HasNode("debugLabel"))
		{
			currentState.ContextNode.GetNode<Label>("debugLabel").Text = currentState.Name;
		}
	}

	public void _OnStateFinished(State finishedState)
	{
		Debug.Assert(_transitions[finishedState].ContainsKey(Events.FINISHED),
		"Received a state that does not have a transition for the FINISHED event, " + currentState.Name + ". " +
			"Add a transition for this event in the transitions dictionary."
		);
		_Transition(_transitions[finishedState][Events.FINISHED]);
	}
	public void AddTransitionsToAllStates(Events evt, State end_state)
	{
		foreach (State state in _transitions.Keys)
		{
			if (state == end_state)
			{
				continue;
			}
			_transitions[state].Add(evt, end_state);
		}
	}

}
