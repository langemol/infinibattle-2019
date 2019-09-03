using System;

namespace StarterBot.Models
{
    internal class Ship
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int TargetId { get; set; }
        public Planet Target { get; set; }
        public int? Owner { get; set; }
        public float Power { get; set; }

        private const float Speed = 15.0f;

        public int TurnsToReachTarget => (int) Math.Ceiling(Target.DistanceTo(this) / Speed);
    }
}