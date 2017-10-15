using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerHsm
{
    using Hsm;
    using PlayerState = Hsm.StateT<Player, StateData>;

    class StateData
    {
        public bool StateDataBool = false;

        public Attribute<float> Attribute_Test = new Attribute<float>(12.03f);
    };

    class Root : PlayerState
    {
        public override void OnEnter()
        {
            SetAttribute(Data.Attribute_Test, 1.0f);
        }

        public override Transition EvaluateTransitions()
        {
            //return Transition.Sibling<RootSibling), "Hello!", 2);
            return Transition.Inner<Healthy>();
        }
    }

    class Healthy : PlayerState
    {
        public override void OnEnter()
        {
            var root = GetOuterState<Root>(); // Test being able to grab Root from inner state
        }

        public override Transition EvaluateTransitions()
        {
            if (FindInnerState<Driving>() != null)
            {
                Console.Out.WriteLine("Healthy state: my inner is Driving!");
            }

            return Transition.InnerEntry<Platforming>("Yo!");
        }
    }

    class Platforming : PlayerState
    {
        public override void OnEnter(object[] aArgs)
        {
            string s = (string)aArgs[0];
            SetAttribute(Data.Attribute_Test, 2.0f);
        }

        public override Transition EvaluateTransitions()
        {
            if (count == 1)
                return Transition.Sibling<Driving>();
            ++count;
            return Transition.None();
        }

        public override void PerformStateActions(float aDeltaTime)
        {
            Console.Out.WriteLine("Player's Health: {0}, StateDataBool: {1}", Owner.Health, Data.StateDataBool);
            Console.Out.WriteLine("Data.Attribute_Test: {0}", Data.Attribute_Test.Value);
        }

        int count;
    }

    class Driving : PlayerState
    {
        public override void PerformStateActions(float aDeltaTime)
        {
            Console.Out.WriteLine("Data.Attribute_Test: {0}", Data.Attribute_Test.Value);

            SetAttribute(Data.Attribute_Test, 3.0f);
        }
    }

    class RootSibling : PlayerState
    {
        public override void OnEnter(object[] aArgs)
        {
            string s = (string)aArgs[0];
            int i = (int)aArgs[1];
        }

        public override Transition EvaluateTransitions()
        {
            return Transition.Sibling<Root>();
        }
    }
}

class Player
{
    private Hsm.StateMachine mStateMachine = new Hsm.StateMachine();

    public int Health = 100;

    public void Init()
    {
        mStateMachine.Init<PlayerHsm.Root>(this, new PlayerHsm.StateData());
        mStateMachine.DebugLogLevel = 2;
    }

    public void Shutdown()
    {
        mStateMachine.Shutdown();
    }

    public void Update(float aDeltaTime)
    {
        mStateMachine.Update(aDeltaTime);
    }
}
namespace Sample1
{
    class TestHsm
    {
        static void Main(string[] args)
        {
            Player kid = new Player();
            kid.Init();

            kid.Update(1.0f);
            kid.Update(1.0f);
            kid.Update(1.0f);
            kid.Update(1.0f);
            kid.Update(1.0f);

            kid.Shutdown();
        }
    }
}
