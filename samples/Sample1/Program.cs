using System;

// Would be in a file named Player.cs
namespace Sample1
{
    partial class Player
    {
        public partial class PlayerHsm { }

        private Hsm.StateMachine mStateMachine = new Hsm.StateMachine();

        private int Health = 100;
        private bool StateDataBool = false;
        private Hsm.StateVar<float> StateVar_Test = new Hsm.StateVar<float>(12.03f);


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
                    SetStateVar(Owner.StateVar_Test, 1.0f);
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
                    SetStateVar(Owner.StateVar_Test, 2.0f);
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
                    Console.Out.WriteLine("Owner.StateVar_Test: {0}", Owner.StateVar_Test.Value);
                }

                int count;
            }

            class Driving : PlayerState
            {
                public override void Update(float aDeltaTime)
                {
                    Console.Out.WriteLine("Owner.StateVar_Test: {0}", Owner.StateVar_Test.Value);

                    SetStateVar(Owner.StateVar_Test, 3.0f);
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
