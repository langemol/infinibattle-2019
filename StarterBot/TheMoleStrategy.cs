using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using StarterBot.Models;

namespace StarterBot
{
    public class TheMoleStrategy
    {

        public static Move[] PlayTurn(GameState gamestate, int turn, Stopwatch watch)
        {
            if (turn == 1)
            {
                gamestate.DoInitialCalculations();
            }
            gamestate.TurnInit();
            
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} Initialize turn");

            var moves = new Turn(turn, gamestate).Play(watch);

            return moves.ToArray();
        }
    }
}