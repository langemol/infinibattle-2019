using System;

namespace StarterBot.Models
{
    internal class Planet
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// Between 20.0 and 40.0
        /// </summary>
        public float Radius { get; set; }
        public int? Owner { get; set; }

        public float Health { get; set; }

        public float GrowthSpeed => 0.05f * Radius;
        public float HealthMax => 5.0f * Radius; // check Health/HealthMax or
        public bool HealthLimitAboutToBeExceeded => Health + GrowthSpeed > HealthMax;

        public int[] Neighbors { get; set; }
        public Planet[] NeighboringPlanets { get; set; }
        public Planet[] NeighboringFriendlyPlanets { get; set; }
        public Planet[] NeighboringHostilePlanets { get; set; }
        // + neutral..
        public Ship[] InboundShips { get; set; }
        public float HealthNextTurnKnown; // based on known inboundships + growth!
        public float[] HealthDiffInboundForTurn; // based on known inboundships
        // friendly ships/power possible to receive in X turns
        // hostile ships/negative power possible to receive in X turns

        // hops to nearest hostile planet (not distance) or just check NeighboringHostilePlanets.Any()



        public float DistanceTo(Planet other)
        {
            return DistanceTo(other.X, other.Y);
        }

        private float DistanceTo(float otherX, float otherY)
        {
            var dx = otherX - X;
            var dy = otherY - Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public float DistanceTo(Ship other)
        {
            return DistanceTo(other.X, other.Y);
        }
    }
}