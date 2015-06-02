// Hierarchical State Machine (HSM)
//
// Copyright (c) 2013 Antonio Maiorano
//
// Distributed under the Boost Software License, Version 1.0. (See
// accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace Hsm
{
	using System.Reflection;
	using System.Diagnostics;
	using System.Text;

	// Client code must provide their own implementation of this class
	public static partial class Client
	{
		//public static void Log(StateMachine aStateMachine, string aMessage);
        //public static void LogError(StateMachine aStateMachine, string aMessage);
	}

	///////////////////////////////////////////////////////////////////////////
	// State
	///////////////////////////////////////////////////////////////////////////

	public class State
	{
		internal StateMachine mOwnerStateMachine;
		internal List<AttributeResetter> mAttributeResetters = null;
		internal int mStackDepth;

		///////////////////////////////
		// Overridables
		///////////////////////////////

		public virtual void OnEnter() { }
		public virtual void OnEnter(object[] aArgs) { }
		public virtual void OnExit() { }
		public virtual Transition EvaluateTransitions() { return Transition.None(); }
		public virtual void PerformStateActions(float aDeltaTime) { }

		///////////////////////////////
		// Accessors
		///////////////////////////////

		public StateMachine OwnerStateMachine { get { return mOwnerStateMachine; } }

        public StateType FindState<StateType>() where StateType : State { return mOwnerStateMachine.FindState<StateType>(); }
        public StateType GetState<StateType>() where StateType : State { return mOwnerStateMachine.GetState<StateType>(); }		
        public bool IsInState<StateType>() where StateType : State { return FindState<StateType>() != null; }

        public StateType FindOuterState<StateType>() where StateType : State { return mOwnerStateMachine.FindOuterStateFromDepth<StateType>(mStackDepth); }
        public StateType GetOuterState<StateType>() where StateType : State
        {
            StateType result = FindOuterState<StateType>();
            Debug.Assert(result != null, string.Format("Failed to get outer state on stack: {0}", typeof(StateType)));
            return result;
        }

        public StateType FindInnerState<StateType>() where StateType : State { return mOwnerStateMachine.FindInnerStateFromDepth<StateType>(mStackDepth); }
        public StateType GetInnerState<StateType>() where StateType : State
        {
            StateType result = FindInnerState<StateType>();
            Debug.Assert(result != null, string.Format("Failed to get inner state on stack: {0}", typeof(StateType)));
            return result;
        }

        public State FindImmediateInnerState() { return mOwnerStateMachine.FindStateAtDepth(mStackDepth + 1); }


		///////////////////////////////
		// Attributes
		///////////////////////////////

		// Use to set value-type attribute
		public void SetAttribute<T>(Attribute<T> aAttribute, T aValue) where T : struct
		{
			if (!IsAttributeInResetterList(aAttribute))
				mAttributeResetters.Add(new AttributeResetterT<T>(aAttribute));

			aAttribute.__ValueToBeAccessedByStateMachineOnly = aValue;
		}

		internal void ResetAllAttributes()
		{
			if (mAttributeResetters == null)
				return;

			foreach (AttributeResetter resetter in mAttributeResetters)
				resetter.Reset();

			mAttributeResetters.Clear();
		}

		private bool IsAttributeInResetterList<T>(Attribute<T> aAttribute)
		{
			if (mAttributeResetters == null) // First time, lazily create list
			{
				mAttributeResetters = new List<AttributeResetter>();
			}
			else
			{
				foreach (AttributeResetter resetter in mAttributeResetters)
					if (resetter is AttributeResetterT<T>)
						return true;
			}
			return false;
		}
	}

	// Utility base class for states that should be used to access Owner/Data more easily
	public class StateT<OwnerType, StateDataType> : State
	{
		public OwnerType Owner
		{
			get
			{
				if (mOwner == null)
					mOwner = (OwnerType)mOwnerStateMachine.Owner;
				return mOwner;
			}
		}

		public StateDataType Data
		{
			get
			{
				if (mStateData == null)
					mStateData = (StateDataType)mOwnerStateMachine.StateData;
				return mStateData;
			}
		}

		private OwnerType mOwner;
		private StateDataType mStateData;
	}

	///////////////////////////////////////////////////////////////////////////
	// Attribute
	///////////////////////////////////////////////////////////////////////////

	public class Attribute<T>
	{
		// Do not access this value from states - would normally be private if I could declare friendship
		internal T __ValueToBeAccessedByStateMachineOnly;

		public Attribute(T aInitialValue) { __ValueToBeAccessedByStateMachineOnly = aInitialValue; }

		// Use to read value of attribute
		public T Value { get { return __ValueToBeAccessedByStateMachineOnly; } }

		public static implicit operator T(Attribute<T> aAttribute)
		{
			return aAttribute.Value;
		}
	}

	internal class AttributeResetter
	{
		//@LAME: Can't use destructors like in C++
		public virtual void Reset() { }
	}

	internal class AttributeResetterT<T> : AttributeResetter
	{
		private Attribute<T> mAttribute;
		private T mOriginalValue;

		public AttributeResetterT(Attribute<T> aAttribute)
		{
			mAttribute = aAttribute;
			mOriginalValue = aAttribute.__ValueToBeAccessedByStateMachineOnly;
		}

		public override void Reset()
		{
			mAttribute.__ValueToBeAccessedByStateMachineOnly = mOriginalValue;
			mAttribute = null; //@TODO: Add Dispose (or Finalize) that asserts that this is null (that Reset got called)
		}
	}

	///////////////////////////////////////////////////////////////////////////
	// Transition
	///////////////////////////////////////////////////////////////////////////

	public enum TransitionType { None, Inner, InnerEntry, Sibling };
	public struct Transition
	{
		public TransitionType TransitionType;
		public Type TargetStateType;
		public object[] Args;

		public Transition(TransitionType aTransitionType, Type aTargetStateType, object[] aArgs)
		{
			TransitionType = aTransitionType;
			TargetStateType = aTargetStateType;
			Args = aArgs;
		}

		public static Transition None()
		{
			return new Transition(TransitionType.None, null, null);
		}

		public static Transition Inner(Type aTargetStateType, params object[] aArgs)
		{
			//@NOTE: passing no 'params' results in a zero-length array, but we pass in null to simplify our code
			return new Transition(TransitionType.Inner, aTargetStateType, aArgs.Length == 0 ? null : aArgs);
		}

		public static Transition Inner<TargetStateType>(params object[] aArgs) where TargetStateType : State
		{
			//@NOTE: passing no 'params' results in a zero-length array, but we pass in null to simplify our code
			return Inner(typeof(TargetStateType), aArgs);
		}

		public static Transition InnerEntry(Type aTargetStateType, params object[] aArgs)
		{
			return new Transition(TransitionType.InnerEntry, aTargetStateType, aArgs.Length == 0 ? null : aArgs);
		}

		public static Transition InnerEntry<TargetStateType>(params object[] aArgs) where TargetStateType : State
		{
			return InnerEntry(typeof(TargetStateType), aArgs);
		}

		public static Transition Sibling(Type aTargetStateType, params object[] aArgs)
		{
			return new Transition(TransitionType.Sibling, aTargetStateType, aArgs.Length == 0 ? null : aArgs);
		}

		public static Transition Sibling<TargetStateType>(params object[] aArgs) where TargetStateType : State
		{
			return Sibling(typeof(TargetStateType), aArgs);
		}

		public override string ToString()
		{
			return TransitionType.ToString();
		}
	}

	///////////////////////////////////////////////////////////////////////////
	// StateMachine
	///////////////////////////////////////////////////////////////////////////

	public class StateMachine
	{
		private List<State> mStateStack = new List<State>();
		private object mOwner = null;
		private object mStateData = null;
		private int mDebugLogLevel = 0; // 0 means no logging; 1 for basic logging (most useful), 2 for full logging
		private Type mInitialStateType;

		public void Init<InitialStateType>(object aOwner = null, object aStateData = null) where InitialStateType : State
		{
			Init(typeof(InitialStateType), aOwner, aStateData);
		}

		public void Init(Type aInitialStateType, object aOwner = null, object aStateData = null)
		{
			mOwner = aOwner;
			mStateData = aStateData;
			mInitialStateType = aInitialStateType;
		}

		//@TODO: Add Dipose/Finalize that calls this
		public void Shutdown()
		{
			Stop();
		}

		// Stopping the state machine means popping the state stack so that all OnExits get called. Note that
		// calling Update afterwards will start up the state machine again (starting with the initial state).
		public void Stop()
		{
			PopStatesFromDepth(0);
		}

		public bool IsStarted()
		{
			return mStateStack.Count > 0; // Always has at least one state on the stack if started
		}

		public void Update(float aDeltaTime)
		{
			EvaluateStateTransitions();
			PerformStateActions(aDeltaTime);
		}

		public void EvaluateStateTransitions()
		{
			bool isFinishedTransitioning = false;
			int loopCountdown = 100;
			while (!isFinishedTransitioning && --loopCountdown > 0)
			{
				if (loopCountdown == 4) // Something's wrong, start logging
				{
					mDebugLogLevel = 2;
				}
				isFinishedTransitioning = EvaluateTransitionsOnce();
			}
			
			if (loopCountdown == 0)
			{
                Client.LogError(this, "Infinite loop detected !!!");
			}
		}

		public void PerformStateActions(float aDeltaTime)
		{
			foreach (State state in mStateStack)
			{
				state.PerformStateActions(aDeltaTime);
			}
		}

		public object Owner { get { return mOwner; } }
		public object StateData { get { return mStateData; } }
		public int DebugLogLevel { get { return mDebugLogLevel; } set { mDebugLogLevel = value; } }

		public StateType FindState<StateType>() where StateType : State
		{
			foreach (State state in mStateStack)
			{
				StateType st = state as StateType;
				if (st != null)
					return st;
			}
			return null;
		}

		public StateType GetState<StateType>() where StateType : State
		{
			StateType result = FindState<StateType>();
			Debug.Assert(result != null, string.Format("Failed to get state on stack: {0}", typeof(StateType)));
			return result;
		}

		public bool IsInState<StateType>() where StateType : State
		{
			return FindState<StateType>() != null;
		}

		public State FindStateAtDepth(int aDepth)
		{
			if (aDepth >= 0 && aDepth < mStateStack.Count)
				return mStateStack[aDepth];
			return null;
		}

        public StateType FindOuterStateFromDepth<StateType>(int aDepth) where StateType : State
        {
            Debug.Assert(aDepth >= 0 && aDepth < mStateStack.Count);
            for (int d = aDepth - 1; d >= 0; --d)
            {
                StateType st = mStateStack[d] as StateType;
                if (st != null)
                    return st;
            }
            return null;
        }

        public StateType FindInnerStateFromDepth<StateType>(int aDepth) where StateType : State
        {
            Debug.Assert(aDepth >= 0 && aDepth < mStateStack.Count);
            for (int d = aDepth + 1; d < mStateStack.Count; ++d)
            {
                StateType st = mStateStack[d] as StateType;
                if (st != null)
                    return st;
            }
            return null;
        }

        public string GetStateStackAsString()
		{
			var s = new StringBuilder();
			foreach (State state in mStateStack)
			{
				if (s.Length > 0)
				{
					s.Append(" / ");
				}
				s.Append(state.GetType().ToString());
			}
			return s.ToString();
		}

		public List<State> GetStateStack()
		{
			return mStateStack;
		}

		// State Stack Visitor functions

		// State visitor delegate - return true to keep visiting the next state on the stack, false to stop
		public delegate bool VisitState<StateType>(StateType aState);

		public void VisitOuterToInner<StateType>(VisitState<StateType> aVisitor) where StateType : State
		{
			VisitStates<StateType>(aVisitor, mStateStack);
		}

		public void VisitInnerToOuter<StateType>(VisitState<StateType> aVisitor) where StateType : State
		{
			VisitStates<StateType>(aVisitor, CreateReverseIterator(mStateStack));
		}

		// State Stack Invoker functions - use to invoke a named method with arbitrary args on the
		// state stack. The only restriction is that the method return bool: true to keep invoking
		// on the state stack, false to stop. Note that this uses reflection, which can be costly.

		public void InvokeOuterToInner(string aMethodName, params object[] aArgs)
		{
			InvokeStates(aMethodName, aArgs, mStateStack);
		}

		public void InvokeInnerToOuter(string aMethodName, params object[] aArgs)
		{
			InvokeStates(aMethodName, aArgs, CreateReverseIterator(mStateStack));
		}

		// PRIVATE

		static private IEnumerable<T> CreateReverseIterator<T>(IList<T> aList)
		{
			int count = aList.Count;
			for (int i = count - 1; i >= 0; --i)
				yield return aList[i];
		}

		private void VisitStates<StateType>(VisitState<StateType> aVisitor, IEnumerable<State> aEnumerable) where StateType : State
		{
			foreach (State state in aEnumerable)
			{
				bool keepGoing = aVisitor((StateType)state);
				if (!keepGoing)
					return;
			}
		}

		private void InvokeStates(string aMethodName, object[] aArgs, IEnumerable<State> aEnumerable)
		{
			foreach (State state in aEnumerable)
			{
				MethodInfo methodInfo = state.GetType().GetMethod(aMethodName);
				if (methodInfo != null)
				{
					bool keepGoing = (bool)methodInfo.Invoke(state, aArgs);
					if (!keepGoing)
						return;
				}
			}
		}

		private void LogTransition(int aLogLevel, int aDepth, string aTransitionName, Type aTargetStateType)
		{
			if (mDebugLogLevel < aLogLevel)
				return;

			string s = string.Format("HSM [{0}]:{1}{2,-11}{3}",
				(mOwner != null ? mOwner : "NoOwner"),
				new String(' ', aDepth),
				aTransitionName,
				aTargetStateType);

			if (mDebugLogLevel < aLogLevel)
				return;

			Client.Log(this, s);
		}

        private State CreateState(Type aStateType, int aStackDepth)
        {
            State state = (State)Activator.CreateInstance(aStateType);
            state.mOwnerStateMachine = this;
            state.mStackDepth = aStackDepth;
            return state;
        }

		private void EnterState(State aState, object[] aArgs)
		{
#if DEBUG
            string stateName = aState.ToString();
			bool haveArgs = aArgs != null;
			MethodInfo mi = aState.GetType().GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			if (mi != null) // OnEnter overridden on state
			{
				bool expectsArgs = mi.GetParameters().Length > 0;

				if (!haveArgs && expectsArgs)
				{
					Debug.Fail(String.Format("State {0} expects args, but none were passed in via Transition", stateName));
				}
				else if (haveArgs && !expectsArgs)
				{
					Debug.Fail(String.Format("State {0} does not expect args, but some were passed in via Transition", stateName));
				}
			}
			else if (haveArgs)
			{
				Debug.Fail(String.Format("Args are being passed via Transition to State {0}, but State doesn't implement OnEnter(params)", stateName));
			}
#endif

			if (aArgs != null)
                aState.OnEnter(aArgs);
			else
                aState.OnEnter();
		}

        private void ExitState(State aState)
		{
			aState.OnExit();
			aState.ResetAllAttributes();
		}

		private void PushState(Type aStateType, object[] aArgs, int aStackDepth)
		{
			LogTransition(2, aStackDepth, "(Push)", aStateType);
            State state = CreateState(aStateType, aStackDepth);
			mStateStack.Add(state);
            EnterState(state, aArgs);
		}

		private void PopStatesFromDepth(int aStartDepthInclusive)
		{
			int endDepth = mStateStack.Count - 1;

			if (aStartDepthInclusive > endDepth) // Nothing to pop
				return;

			// From inner to outer
			for (int depth = endDepth; depth >= aStartDepthInclusive; --depth)
			{
				State currState = mStateStack[depth];
				LogTransition(2, depth, "(Pop)", currState.GetType());
				ExitState(currState);
			}
			mStateStack.RemoveRange(aStartDepthInclusive, endDepth - aStartDepthInclusive + 1);
		}

		private bool HasStateAtDepth(int aDepth)
		{
			return aDepth < mStateStack.Count;
		}

		private State GetStateAtDepth(int aDepth)
		{
			return aDepth < mStateStack.Count ? mStateStack[aDepth] : null;
		}

		// Returns true if state stack is unchanged after calling EvaluateTransition on each state (from outer to inner)
		private bool EvaluateTransitionsOnce()
		{
			if (mStateStack.Count == 0)
			{
				LogTransition(1, 0, new Transition(TransitionType.Inner, mInitialStateType, null).ToString(), mInitialStateType);
				PushState(mInitialStateType, null, 0);
			}
			
			for (int currDepth = 0; currDepth < mStateStack.Count; ++currDepth)
			{
				State currState = mStateStack[currDepth];
				Transition trans = currState.EvaluateTransitions();
				switch (trans.TransitionType)
				{
					case TransitionType.None:
						break;

					case TransitionType.Inner:
						// If state already on stack, continue to next state
						State immediateInnerState = GetStateAtDepth(currDepth + 1);
						if (immediateInnerState != null && immediateInnerState.GetType() == trans.TargetStateType)
							break;

						// Pop states below (if any) and push new one
						LogTransition(1, currDepth + 1, trans.ToString(), trans.TargetStateType);
						PopStatesFromDepth(currDepth + 1);
						PushState(trans.TargetStateType, trans.Args, currDepth + 1);
						return false;

					case TransitionType.InnerEntry:
						// Only if no state on stack below us do we push target state
						if (HasStateAtDepth(currDepth + 1))
							break;

						LogTransition(1, currDepth + 1, trans.ToString(), trans.TargetStateType);
						PushState(trans.TargetStateType, trans.Args, currDepth + 1);
						return false;

					case TransitionType.Sibling:
						LogTransition(1, currDepth, trans.ToString(), trans.TargetStateType);
						PopStatesFromDepth(currDepth);
						PushState(trans.TargetStateType, trans.Args, currDepth);
						return false; // State stack has changed, evaluate from root again

				}
			}

			return true; // State stack has settled, we're done!
		}
	}
}
