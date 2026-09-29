using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("BounceFit.Mvp.Tests")]

namespace BounceFit.Mvp
{
    internal struct CombatBallFlight : IComponentData
    {
        public byte Returning;
        public float Age;
        public int Level;
    }

    internal static class MvpBallFlight
    {
        public const float Top = 13.5f;
        public const float Rear = -6.4f;
        public const float Side = 7f;
        public const int TopBounce = 1, RearBounce = 2, Collected = 4;

        // Outbound movement is integrated by the ECS job. Returning balls have zero
        // integration velocity and move toward the current player position here.
        public static int Resolve(ref float2 position, ref float2 velocity, ref CombatBallFlight flight,
            float radius, float2 player, float dt)
            => Resolve(ref position, ref velocity, ref flight, radius, player, dt, Top, Rear, Side, 14f, .15f, 45f);

        /// <summary>Configurable variant used by combat. The compatibility overload above keeps deterministic rule tests concise.</summary>
        public static int Resolve(ref float2 position, ref float2 velocity, ref CombatBallFlight flight,
            float radius, float2 player, float dt, float topBoundary, float rearBoundary, float sideBoundary,
            float returnSpeed, float arrivalDistance, float maxFlightSeconds)
            => Resolve(ref position, ref velocity, ref flight, radius, player, dt, topBoundary, rearBoundary,
                -sideBoundary, sideBoundary, returnSpeed, arrivalDistance, maxFlightSeconds);

        /// <summary>Configurable variant with independent left and right reflection boundaries.</summary>
        public static int Resolve(ref float2 position, ref float2 velocity, ref CombatBallFlight flight,
            float radius, float2 player, float dt, float topBoundary, float rearBoundary, float leftBoundary,
            float rightBoundary, float returnSpeed, float arrivalDistance, float maxFlightSeconds)
        {
            flight.Age += dt;
            if (flight.Returning != 0)
            {
                velocity = float2.zero;
                float2 delta = player - position;
                float distance = math.length(delta);
                float step = math.max(0f, returnSpeed) * dt;
                if (distance <= math.max(step, math.max(.001f, arrivalDistance))) { position = player; return Collected; }
                position += delta / distance * step;
                return 0;
            }
            float right = rightBoundary - radius;
            float left = leftBoundary + radius;
            if (position.x > right && velocity.x > 0) { position.x = 2 * right - position.x; velocity.x = -velocity.x; }
            if (position.x < left && velocity.x < 0) { position.x = 2 * left - position.x; velocity.x = -velocity.x; }
            float top = topBoundary - radius;
            int result = 0;
            if (position.y >= top && velocity.y > 0)
            { position.y = 2 * top - position.y; velocity.y = -velocity.y; result |= TopBounce; }
            if (position.y <= rearBoundary + radius && velocity.y < 0)
            {
                position.y = rearBoundary + radius;
                flight.Returning = 1;
                velocity = float2.zero;
                result |= RearBounce;
            }
            // A near-horizontal ball can otherwise bounce forever. Recover visibly,
            // instead of using the old short TTL/out-of-bounds deletion.
            if (flight.Age >= math.max(.01f, maxFlightSeconds)) { flight.Returning = 1; velocity = float2.zero; }
            return result;
        }
    }
}
