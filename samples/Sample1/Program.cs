using System;

// Would be in a file named Player.cs
namespace Sample1
{
    partial class Player
    {
        public partial class PlayerHsm { }

        private Hsm.StateMachine mStateMachine = new Hsm.StateMachine();

        private class Foo
        {
            public int value1;
            public int value2;
            public override string ToString()
            {
                return string.Format("value1: {0}, value2: {1}", value1, value2);
            }
        }

        private int Health = 100;
        private bool StateDataBool = false;
        private Hsm.StateValue<float> StateValue_Test = new Hsm.StateValue<float>(12.03f);
        private Hsm.StateValue<string> StateValue_String = new Hsm.StateValue<string>("Hello");
        private Hsm.StateValue<Foo> StateValue_Foo = new Hsm.StateValue<Foo>();

        public void Init()
        {
            mStateMachine.Init<PlayerHsm.Root>(this);
            mStateMachine.TraceLevel = Hsm.TraceLevel.Diagnostic;
        }

        public void Shutdown()
        {
            mStateMachine.Shutdown();
        }

        public void Update(float aDeltaTime)
        {
            mStateMachine.Update(aDeltaTime);

            Console.Out.WriteLine("StateValue_Test: {0}", StateValue_Test.Value);
            Console.Out.WriteLine("StateValue_String: {0}", StateValue_String.Value);
            Console.Out.WriteLine("StateValue_Foo: {0}", StateValue_Foo.Value);
        }
    }
}

// Would be in a file named PlayerHsm.cs
namespace Sample1
{
    using Hsm;
    using PlayerState = Hsm.StateWithOwner<Player>;

    partial class Player
    {
        public partial class PlayerHsm
        {
            public class Root : PlayerState
            {
                public override void OnEnter()
                {
                    SetStateValue(Owner.StateValue_Test, 1.0f);
                }

                public override Transition GetTransition()
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

                public override Transition GetTransition()
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
                    SetStateValue(Owner.StateValue_Test, 2.0f);

                    SetStateValue(Owner.StateValue_String, "Goodbye");

                    Foo foo = new Foo();
                    foo.value1 = 12;
                    foo.value2 = 13;
                    SetStateValue(Owner.StateValue_Foo, foo);
                }

                public override Transition GetTransition()
                {
                    if (count == 1)
                        return Transition.Sibling<Driving>();
                    ++count;
                    return Transition.None();
                }

                public override void Update(float aDeltaTime)
                {
                    Console.Out.WriteLine("Player's Health: {0}, StateDataBool: {1}", Owner.Health, Owner.StateDataBool);
                }

                int count;
            }

            class Driving : PlayerState
            {
                public override void Update(float aDeltaTime)
                {
                    SetStateValue(Owner.StateValue_Test, 3.0f);
                }
            }

            class RootSibling : PlayerState
            {
                public override void OnEnter(object[] aArgs)
                {
                    string s = (string)aArgs[0];
                    int i = (int)aArgs[1];
                }

                public override Transition GetTransition()
                {
                    return Transition.Sibling<Root>();
                }
            }
        }
    }
}

// Would be in the main program file
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
