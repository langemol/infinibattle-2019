using System;

namespace StarterBot.Models
{
    public class Ship : IProperty, IHasCoordinates
    {
        private Planet _target;
        public float X { get; set; }
        public float Y { get; set; }
        public int TargetId { get; set; }

        public Planet Target
        {
            get => _target;
            set
            {
                _target = value;
                TurnsToReachTarget = this.DistanceTo(_target);
            }
        }

        public int? Owner { get; set; }
        public float Power { get; set; }


        public int TurnsToReachTarget { get; private set; }
        public Friendlyness Friendlyness { get; set; }

    }
}