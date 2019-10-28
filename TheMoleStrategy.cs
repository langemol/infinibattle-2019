using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using StarterBot.Models;

namespace StarterBot
{
    public class TheMoleStrategy
    {

        public static Move[] PlayTurn(GameState gamestate, int turn)
        {
            if (turn == 1)
            {
                gamestate.DoInitialCalculations();
            }

            var moves = new Turn(turn, gamestate).Play();

            return moves.ToArray();
        }
    }
}