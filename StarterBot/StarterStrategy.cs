﻿using System;
using System.Collections.Generic;
using System.Linq;
using StarterBot.Models;

namespace StarterBot
{
    internal class StarterStrategy
    {
        public static Move[] Strategy(GameState gamestate)
        {
            var moves = new List<Move>();

            var myPlanets = gamestate.Planets.Where(p =>
                p.Owner == gamestate.Settings.PlayerId &&
                p.Health >= new Random().Next(2, 100)
            );

            foreach (var planet in myPlanets)
            {
                var target = planet.Neighbors
                    .Select(n => gamestate.Planets[n])
                    .Where(p => p.Owner != gamestate.Settings.PlayerId)
                    .OrderBy(p => p.DistanceTo(planet))
                    .FirstOrDefault();

                if (target != null)
                {
                    var power = gamestate.Planets[planet.Id].Health / 2;
                    moves.Add(new Move(power, planet.Id, target.Id));
                }
            }

            return moves.ToArray();
        }
    }
}