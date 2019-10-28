using System;

namespace StarterBot.Models
{
    public class Ship : IProperty
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int TargetId { get; set; }
        public Planet Target { get; set; }
        public int? Owner { get; set; }
        public float Power { get; set; }

        public const float Speed = 15.0f;

        public int TurnsToReachTarget => DistanceToTurns(Target.DistanceTo(this));
        public Friendlyness Friendlyness { get; set; }

        public static int DistanceToTurns(float distance)
        {
            return (int) Math.Ceiling(distance / Speed);
        }
    }
}