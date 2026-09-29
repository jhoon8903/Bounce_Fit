using NUnit.Framework;
using Unity.Mathematics;

namespace BounceFit.Mvp.Tests
{
    public class MvpBallFlightTests
    {
        private MvpConfigSet config;
        [SetUp] public void SetUp() => config = MvpConfigSet.CreateTransient();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        [Test]
        public void FarWallReflectsOvershootWithoutRemovingBall()
        {
            float2 p=new(2,13.5f), v=new(2,9);
            var flight=new CombatBallFlight();
            int result=MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,float2.zero,1f/60);
            Assert.That(result,Is.EqualTo(MvpBallFlight.TopBounce));
            Assert.That(v.y,Is.LessThan(0));
            Assert.That(p.y,Is.LessThanOrEqualTo(MvpBallFlight.Top-.17f));
            Assert.That(flight.Returning,Is.Zero);
        }
        [Test]
        public void RearWallStartsRecallInsteadOfDeleting()
        {
            float2 p=new(4,-6.4f),v=new(1,-9);
            var flight=new CombatBallFlight();
            int result=MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,new float2(0,-3),1f/60);
            Assert.That(result,Is.EqualTo(MvpBallFlight.RearBounce));
            Assert.That(flight.Returning,Is.EqualTo(1));
            Assert.That(p.y,Is.EqualTo(MvpBallFlight.Rear+.17f).Within(.001f));
        }
        [Test]
        public void RecallTracksMovingPlayerAndCollectsOnlyOnArrival()
        {
            float2 p=new(5,-6),v=float2.zero,target=new(-3,2);
            var flight=new CombatBallFlight { Returning=1 };
            float before=math.distance(p,target);
            Assert.That(MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,target,.05f),Is.Zero);
            Assert.That(math.distance(p,target),Is.LessThan(before));
            target=new(3,4);
            int result=0;
            for(int i=0;i<50 && result!=MvpBallFlight.Collected;i++)
                result=MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,target,.05f);
            Assert.That(result,Is.EqualTo(MvpBallFlight.Collected));
            Assert.That(math.distance(p,target),Is.LessThan(.001f));
        }
        [Test]
        public void OldThreeSecondLifetimeDoesNotExpireOutboundBall()
        {
            float2 p=new(0,2),v=new(0,9);
            var flight=new CombatBallFlight { Age=3.5f };
            Assert.That(MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,float2.zero,.05f),Is.Zero);
            Assert.That(flight.Returning,Is.Zero);
            Assert.That(v.y,Is.GreaterThan(0));
        }
        [Test]
        public void SideWallReflectsInsteadOfRemovingBall()
        {
            float2 p=new(7,1),v=new(9,1);
            var flight=new CombatBallFlight();
            MvpBallFlight.Resolve(ref p,ref v,ref flight,.17f,float2.zero,.05f);
            Assert.That(v.x,Is.LessThan(0));Assert.That(p.x,Is.LessThan(7));
            Assert.That(flight.Returning,Is.Zero);
        }
        [Test]
        public void InventoryWaitsForRecallAndThenFiresAgain()
        {
            using var combat=new MvpCombat(41, config: config);
            var stats=new CombatStats { BallCount=1,DamageMultiplier=1,FireRateMultiplier=1,MoveSpeedMultiplier=1,PickupRadius=1 };
            bool seenReturn=false,seenShotAfterReturn=false;
            for(int i=0;i<2400;i++)
            {
                combat.DebugSetHealth(100);
                combat.Step(1f/60,float2.zero,new float2(0,1),stats);
                int balls=0;
                foreach(var visual in combat.Visuals)
                {
                    if(visual.Kind!=1)continue;
                    balls++;
                    seenReturn|=visual.Returning;
                    if(combat.ReturnedBalls>0&&!visual.Returning)seenShotAfterReturn=true;
                }
                Assert.That(balls,Is.LessThanOrEqualTo(1),"Cannot replace a ball still in flight or returning.");
            }
            Assert.That(combat.TopWallBounces,Is.GreaterThan(0));
            Assert.That(seenReturn,Is.True);
            Assert.That(combat.ReturnedBalls,Is.GreaterThan(0));
            Assert.That(seenShotAfterReturn,Is.True);
        }
    }
}
