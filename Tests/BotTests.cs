using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using StarterBot;
using StarterBot.Models;

namespace Tests
{
    [TestFixture]
    public class BotTests
    {
        [Test]
        public void test()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            var input = new StreamReader("../../../example-input-start.txt");
            Console.SetIn(input);

            var gameState = Bot.InitGameState();
            
            var input1 = new StreamReader("../../../example-input-turn1.txt");
            Console.SetIn(input1);

            var planets = Bot.ReadPlanets();
            var ships = Bot.ReadShips();
            var turn = 1;
            Bot.AdjustGamestateForTurn(turn, gameState, planets, ships);

            var moves = TheMoleStrategy.PlayTurn(gameState, turn);

            var input2 = new StreamReader("../../../example-input-turn2.txt");
            Console.SetIn(input2);
            
            planets = Bot.ReadPlanets();
            ships = Bot.ReadShips();
            turn++;
            Bot.AdjustGamestateForTurn(turn, gameState, planets, ships);
            
            moves = TheMoleStrategy.PlayTurn(gameState, turn);

            Assert.AreEqual(1, gameState.Ships.Count);
            Assert.AreEqual(1, gameState.PlanetsById[4].InboundShips.Count);
        }
    }
}
