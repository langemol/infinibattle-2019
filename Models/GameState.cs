using System.Collections.Generic;

namespace StarterBot.Models
{
    internal class GameState
    {
        public GameState(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }
        public List<Planet> Planets { get; set; }
        public List<Ship> Ships { get; set; }
    }
}