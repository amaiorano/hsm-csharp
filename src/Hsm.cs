// Hierarchical State Machine (HSM)
//
// Copyright (c) 2017 Antonio Maiorano
//
// Distributed under the Boost Software License, Version 1.0. (See
// accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace Hsm
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Diagnostics;
    using System.Text;

    public class Version
    {
        public static int Major = 3;
        public static int Minor = 1;
        public static int Patch = 0;
        public static string String = string.Format("{0}.{1}.{2}", Major, Minor, Patch);
    }

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
        internal List<StateValueResetter> mStateValueResetters = null;
        internal int mStackDepth;

        ///////////////////////////////
        // Overridables
        ///////////////////////////////

        public virtual void OnEnter() { }
        public virtual void OnEnter(object[] aArgs) { }
        public virtual void OnExit() { }
        public virtual Transition GetTransition() { return Transition.None(); }
        public virtual void Update(float aDeltaTime) { }

        [Obsolete("Use GetTransition instead")]
        public virtual Transition EvaluateTransitions() { return GetTransition(); }

        [Obsolete("Use Update instead")]
        public virtual void PerformStateActions(float aDeltaTime) { Update(aDeltaTime); }


        ///////////////////////////////
        // Accessors
        ///////////////////////////////

        public StateMachine StateMachine { get { return mOwnerStateMachine; } }

        [Obsolete("Use StateMachine property instead")]
        public StateMachine OwnerStateMachine { get { return StateMachine; } }

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
        public bool IsInOuterState<StateType>() where StateType : State { return FindOuterState<StateType>() != null; }

        public bool HasInnerState() { return mOwnerStateMachine.HasStateAtDepth(mStackDepth + 1); }
        public StateType FindInnerState<StateType>() where StateType : State { return mOwnerStateMachine.FindInnerStateFromDepth<StateType>(mStackDepth); }
        public StateType GetInnerState<StateType>() where StateType : State
        {
            StateType result = FindInnerState<StateType>();
            Debug.Assert(result != null, string.Format("Failed to get inner state on stack: {0}", typeof(StateType)));
            return result;
        }
        public bool IsInInnerState<StateType>() where StateType : State { return FindInnerState<StateType>() != null; }

        public StateType FindImmediateInnerState<StateType>() where StateType : State { return mOwnerStateMachine.FindStateAtDepth<StateType>(mStackDepth + 1) as StateType; }
        public StateType GetImmediateInnerState<StateType>() where StateType : State
        {
            StateType result = FindImmediateInnerState<StateType>();
            Debug.Assert(result != null, string.Format("Failed to get immeidate inner state on stack: {0}", typeof(StateType)));
            return result;
        }
        public bool IsInImmediateInnerState<StateType>() where StateType : State { return FindImmediateInnerState<StateType>() != null; }
        // Returns generic State, might be useful to query whether current state has an inner state at all
        public State FindImmediateInnerState() { return mOwnerStateMachine.FindStateAtDepth(mStackDepth + 1); }


        ///////////////////////////////
        // StateValues
        ///////////////////////////////

        [Obsolete("Use SetStateValue instead", true)]
        public void SetAttribute<T>(StateValue<T> aStateValue, T aValue) where T : struct { }

        // Use to set value-type StateValue
        public void SetStateValue<T>(StateValue<T> aStateValue, T aValue)
        {
            if (!IsStateValueInResetterList(aStateValue))
                mStateValueResetters.Add(new StateValueResetterT<T>(aStateValue));

            aStateValue.__ValueToBeAccessedByStateMachineOnly = aValue;
        }

        internal void ResetAllStateValues()
        {
            if (mStateValueResetters == null)
                return;

            foreach (StateValueResetter resetter in mStateValueResetters)
                resetter.Reset();

            mStateValueResetters.Clear();
        }

        private bool IsStateValueInResetterList<T>(StateValue<T> aStateValue)
        {
            if (mStateValueResetters == null) // First time, lazily create list
            {
                mStateValueResetters = new List<StateValueResetter>();
            }
            else
            {
                foreach (StateValueResetter resetter in mStateValueResetters)
                {
                    var r = resetter as StateValueResetterT<T>;
                    if (r != null && r.StateValue == aStateValue)
                        return true;
                }
            }
            return false;
        }
    }

    [Obsolete("Use StateWithOwner instead", true)]
    public class StateT<OwnerType> : State
    { }

    // Utility base class for states that should be used to access Owner/Data more easily
    public class StateWithOwner<OwnerType> : State
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

        private OwnerType mOwner;
    }

    ///////////////////////////////////////////////////////////////////////////
    // StateValue
    ///////////////////////////////////////////////////////////////////////////

    [Obsolete("Renamed to StateValue<T> (Search & Replace \"Attribute\" with \"StateValue\")", true)]
    public class Attribute<T> { }

    public class StateValue<T>
    {
        // Do not access this value from states - would normally be private if I could declare friendship
        internal T __ValueToBeAccessedByStateMachineOnly;

        public StateValue() {}
        public StateValue(T aInitialValue) { __ValueToBeAccessedByStateMachineOnly = aInitialValue; }

        // Use to read value of StateValue
        public T Value { get { return __ValueToBeAccessedByStateMachineOnly; } }

        public static implicit operator T(StateValue<T> aStateValue)
        {
            return aStateValue.Value;
        }
    }

    internal class StateValueResetter
    {
        //@LAME: Can't use destructors like in C++
        public virtual void Reset() { }
    }

    internal class StateValueResetterT<T> : StateValueResetter
    {
        private StateValue<T> mStateValue;
        private T mOriginalValue;

        public StateValueResetterT(StateValue<T> aStateValue)
        {
            mStateValue = aStateValue;
            mOriginalValue = aStateValue.__ValueToBeAccessedByStateMachineOnly;
        }

        public override void Reset()
        {
            mStateValue.__ValueToBeAccessedByStateMachineOnly = mOriginalValue;
            mStateValue = null; //@TODO: Add Dispose (or Finalize) that asserts that this is null (that Reset got called)
        }

        public StateValue<T> StateValue { get { return mStateValue; } }
    }

    ///////////////////////////////////////////////////////////////////////////
    // Transition
    ///////////////////////////////////////////////////////////////////////////

    public enum TransitionType
    {
        None,
        Inner,
        InnerEntry,
        Sibling
    };

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

        public override string ToString()
        {
            return TransitionType.ToString();
        }

        // None transition functions

        public static Transition None()
        {
            return new Transition(TransitionType.None, null, null);
        }

        // Inner transition functions

        public static Transition Inner(Type aTargetStateType)
        {
            return new Transition(TransitionType.Inner, aTargetStateType, null);
        }
        public static Transition Inner(Type aTargetStateType, object arg1)
        {
            return new Transition(TransitionType.Inner, aTargetStateType, new object[] { arg1 });
        }
        public static Transition Inner(Type aTargetStateType, object arg1, object arg2)
        {
            return new Transition(TransitionType.Inner, aTargetStateType, new object[] { arg1, arg2 });
        }
        public static Transition Inner(Type aTargetStateType, object arg1, object arg2, object arg3)
        {
            return new Transition(TransitionType.Inner, aTargetStateType, new object[] { arg1, arg2, arg3 });
        }
        public static Transition Inner(Type aTargetStateType, object[] args)
        {
            return new Transition(TransitionType.Inner, aTargetStateType, args);
        }

        public static Transition Inner<TargetStateType>() where TargetStateType : State
        {
            return Inner(typeof(TargetStateType), null);
        }
        public static Transition Inner<TargetStateType>(object arg1) where TargetStateType : State
        {
            return Inner(typeof(TargetStateType), arg1);
        }
        public static Transition Inner<TargetStateType>(object arg1, object arg2) where TargetStateType : State
        {
            return Inner(typeof(TargetStateType), arg1, arg2);
        }
        public static Transition Inner<TargetStateType>(object arg1, object arg2, object arg3) where TargetStateType : State
        {
            return Inner(typeof(TargetStateType), arg1, arg2, arg3);
        }
        public static Transition Inner<TargetStateType>(object[] args) where TargetStateType : State
        {
            return Inner(typeof(TargetStateType), args);
        }

        // InnerEntry transition functions

        public static Transition InnerEntry(Type aTargetStateType)
        {
            return new Transition(TransitionType.InnerEntry, aTargetStateType, null);
        }
        public static Transition InnerEntry(Type aTargetStateType, object arg1)
        {
            return new Transition(TransitionType.InnerEntry, aTargetStateType, new object[] { arg1 });
        }
        public static Transition InnerEntry(Type aTargetStateType, object arg1, object arg2)
        {
            return new Transition(TransitionType.InnerEntry, aTargetStateType, new object[] { arg1, arg2 });
        }
        public static Transition InnerEntry(Type aTargetStateType, object arg1, object arg2, object arg3)
        {
            return new Transition(TransitionType.InnerEntry, aTargetStateType, new object[] { arg1, arg2, arg3 });
        }
        public static Transition InnerEntry(Type aTargetStateType, object[] args)
        {
            return new Transition(TransitionType.InnerEntry, aTargetStateType, args);
        }

        public static Transition InnerEntry<TargetStateType>() where TargetStateType : State
        {
            return InnerEntry(typeof(TargetStateType), null);
        }
        public static Transition InnerEntry<TargetStateType>(object arg1) where TargetStateType : State
        {
            return InnerEntry(typeof(TargetStateType), arg1);
        }
        public static Transition InnerEntry<TargetStateType>(object arg1, object arg2) where TargetStateType : State
        {
            return InnerEntry(typeof(TargetStateType), arg1, arg2);
        }
        public static Transition InnerEntry<TargetStateType>(object arg1, object arg2, object arg3) where TargetStateType : State
        {
            return InnerEntry(typeof(TargetStateType), arg1, arg2, arg3);
        }
        public static Transition InnerEntry<TargetStateType>(object[] args) where TargetStateType : State
        {
            return InnerEntry(typeof(TargetStateType), args);
        }

        // Sibling transition functions

        public static Transition Sibling(Type aTargetStateType)
        {
            return new Transition(TransitionType.Sibling, aTargetStateType, null);
        }
        public static Transition Sibling(Type aTargetStateType, object arg1)
        {
            return new Transition(TransitionType.Sibling, aTargetStateType, new object[] { arg1 });
        }
        public static Transition Sibling(Type aTargetStateType, object arg1, object arg2)
        {
            return new Transition(TransitionType.Sibling, aTargetStateType, new object[] { arg1, arg2 });
        }
        public static Transition Sibling(Type aTargetStateType, object arg1, object arg2, object arg3)
        {
            return new Transition(TransitionType.Sibling, aTargetStateType, new object[] { arg1, arg2, arg3 });
        }
        public static Transition Sibling(Type aTargetStateType, object[] args)
        {
            return new Transition(TransitionType.Sibling, aTargetStateType, args);
        }

        public static Transition Sibling<TargetStateType>() where TargetStateType : State
        {
            return Sibling(typeof(TargetStateType), null);
        }
        public static Transition Sibling<TargetStateType>(object arg1) where TargetStateType : State
        {
            return Sibling(typeof(TargetStateType), arg1);
        }
        public static Transition Sibling<TargetStateType>(object arg1, object arg2) where TargetStateType : State
        {
            return Sibling(typeof(TargetStateType), arg1, arg2);
        }
        public static Transition Sibling<TargetStateType>(object arg1, object arg2, object arg3) where TargetStateType : State
        {
            return Sibling(typeof(TargetStateType), arg1, arg2, arg3);
        }
        public static Transition Sibling<TargetStateType>(object[] args) where TargetStateType : State
        {
            return Sibling(typeof(TargetStateType), args);
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    // StateMachine
    ///////////////////////////////////////////////////////////////////////////

    public enum TraceLevel
    {
        None = 0,
        Basic = 1,
        Diagnostic = 2
    }

    public class StateMachine
    {
        private List<State> mStateStack = new List<State>();
        private object mOwner = null;
        private TraceLevel mTraceLevel = TraceLevel.None;
        private Type mInitialStateType;

        public void Init<InitialStateType>(object aOwner = null) where InitialStateType : State
        {
            Init(typeof(InitialStateType), aOwner);
        }

        public void Init(Type aInitialStateType, object aOwner = null)
        {
            mOwner = aOwner;
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
            ProcessStateTransitions();
            UpdateStates(aDeltaTime);
        }

        public void ProcessStateTransitions()
        {
            bool isFinishedTransitioning = false;
            int loopCountdown = 100;
            while (!isFinishedTransitioning && --loopCountdown > 0)
            {
                if (loopCountdown == 4) // Something's wrong, start logging
                {
                    mTraceLevel = TraceLevel.Diagnostic;
                }
                isFinishedTransitioning = ProcessStateTransitionsOnce();
            }

            if (loopCountdown == 0)
            {
                Client.LogError(this, "Infinite loop detected !!!");
            }
        }

        public void UpdateStates(float aDeltaTime)
        {
            foreach (State state in mStateStack)
            {
                #pragma warning disable CS0618
                state.PerformStateActions(aDeltaTime);
                #pragma warning restore CS0618
            }
        }

        [Obsolete("Use ProcessStateTransitions instead")]
        public void EvaluateStateTransitions()
        {
            ProcessStateTransitions();
        }

        [Obsolete("Use UpdateStates instead")]
        public void PerformStateActions(float aDeltaTime)
        {
            UpdateStates(aDeltaTime);
        }

        public object Owner { get { return mOwner; } }
        public TraceLevel TraceLevel { get { return mTraceLevel; } set { mTraceLevel = value; } }

        [Obsolete("Use TraceLevel instead")]
        public int DebugLogLevel { get { return (int)TraceLevel; } set { TraceLevel = (TraceLevel)value; } }

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

        public bool HasStateAtDepth(int aDepth)
        {
            return aDepth < mStateStack.Count;
        }

        public State FindStateAtDepth(int aDepth)
        {
            if (aDepth >= 0 && aDepth < mStateStack.Count)
                return mStateStack[aDepth];
            return null;
        }

        public State FindStateAtDepth<StateType>(int aDepth) where StateType : State
        {
            return FindStateAtDepth(aDepth) as StateType;
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

        public void InvokeOuterToInner(Action<State> aLambda)
        {
            InvokeStates(aLambda, mStateStack);
        }

        public void InvokeInnerToOuter(Action<State> aLambda)
        {
            InvokeStates(aLambda, CreateReverseIterator(mStateStack));
        }
        
        public void InvokeOuterToInner(Func<State, bool> aLambda)
        {
            InvokeStates(aLambda, mStateStack);
        }

        public void InvokeInnerToOuter(Func<State, bool> aLambda)
        {
            InvokeStates(aLambda, CreateReverseIterator(mStateStack));
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
                    var keepGoing = (bool)methodInfo.Invoke(state, aArgs);
                    if (!keepGoing)
                        return;
                }
            }
        }

        private void InvokeStates(Action<State> aLambda, IEnumerable<State> aEnumerable)
        {
            foreach (State state in aEnumerable)
            {
                aLambda(state);
            }
        }

        private void InvokeStates(Func<State, bool> aLambda, IEnumerable<State> aEnumerable)
        {
            foreach (State state in aEnumerable)
            {
                var keepGoing = aLambda(state);
                if (!keepGoing)
                    return;
            }
        }
        
        private void LogTransition(TraceLevel aTraceLevel, int aDepth, string aTransitionName, Type aTargetStateType)
        {
            if (mTraceLevel < aTraceLevel)
                return;

            string s = string.Format("HSM [{0}]:{1}{2,-11}{3}",
                (mOwner != null ? mOwner : "NoOwner"),
                new String(' ', aDepth),
                aTransitionName,
                aTargetStateType);

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
            aState.ResetAllStateValues();
        }

        private void PushState(Type aStateType, object[] aArgs, int aStackDepth)
        {
            LogTransition(TraceLevel.Diagnostic, aStackDepth, "(Push)", aStateType);
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
                LogTransition(TraceLevel.Diagnostic, depth, "(Pop)", currState.GetType());
                ExitState(currState);
            }
            mStateStack.RemoveRange(aStartDepthInclusive, endDepth - aStartDepthInclusive + 1);
        }

        private State GetStateAtDepth(int aDepth)
        {
            return aDepth < mStateStack.Count ? mStateStack[aDepth] : null;
        }

        // Returns true if state stack is unchanged after calling EvaluateTransition on each state (from outer to inner)
        private bool ProcessStateTransitionsOnce()
        {
            if (mStateStack.Count == 0)
            {
                LogTransition(TraceLevel.Basic, 0, new Transition(TransitionType.Inner, mInitialStateType, null).ToString(), mInitialStateType);
                PushState(mInitialStateType, null, 0);
            }

            for (int currDepth = 0; currDepth < mStateStack.Count; ++currDepth)
            {
                State currState = mStateStack[currDepth];
                #pragma warning disable CS0618
                Transition trans = currState.EvaluateTransitions();
                #pragma warning restore CS0618
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
                        LogTransition(TraceLevel.Basic, currDepth + 1, trans.ToString(), trans.TargetStateType);
                        PopStatesFromDepth(currDepth + 1);
                        PushState(trans.TargetStateType, trans.Args, currDepth + 1);
                        return false;

                    case TransitionType.InnerEntry:
                        // Only if no state on stack below us do we push target state
                        if (HasStateAtDepth(currDepth + 1))
                            break;

                        LogTransition(TraceLevel.Basic, currDepth + 1, trans.ToString(), trans.TargetStateType);
                        PushState(trans.TargetStateType, trans.Args, currDepth + 1);
                        return false;

                    case TransitionType.Sibling:
                        LogTransition(TraceLevel.Basic, currDepth, trans.ToString(), trans.TargetStateType);
                        PopStatesFromDepth(currDepth);
                        PushState(trans.TargetStateType, trans.Args, currDepth);
                        return false; // State stack has changed, evaluate from root again

                }
            }

            return true; // State stack has settled, we're done!
        }
    }
}
