using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarterBot;
using StarterBot.Models;

namespace Tests
{
    public class StrategyTests
    {
        private static int _id;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Obliterate_Single_Weak_Enemy()
        {
            var enemyHealth = 1F;
            var myPlanet = CreatePlanet();
            var enemyPlanet = CreateEnemyPlanet(health:enemyHealth, x: 0, y: 1F);
            var planets = new List<Planet> { myPlanet, enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets);

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.AreEqual(1, moves.Length);
            Assert.AreEqual(myPlanet.Id, moves.Single().Source);
            Assert.AreEqual(enemyPlanet.Id, moves.Single().Target);
            Assert.LessOrEqual(enemyHealth, moves.Single().Power);
        }

        [Test]
        public void Dont_send_more_power_then_needed_to_turn_planet()
        {
            var enemyDistanceTurns = 3;
            var health = 1F;
            var enemyPlanet = CreateEnemyPlanet(health:health, x: 0, y: enemyDistanceTurns * CH.ShipSpeed);
            var planets = new List<Planet> { CreatePlanet(), enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets);

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            var enemyHealth = health+enemyDistanceTurns*enemyPlanet.GrowthSpeed;
            var power = moves.Single().Power;
            Assert.LessOrEqual(enemyHealth, power);
            Assert.GreaterOrEqual(enemyHealth + 2, power);
        }

        [Test]
        public void Dont_send_more_power_then_needed_to_turn_neutral_planet()
        {
            var enemyDistanceTurns = 3;
            var enemyPlanet = CreatePlanet(health: 3F, x: 0, y: enemyDistanceTurns * CH.ShipSpeed, owner: null);
            var planets = new List<Planet> { CreatePlanet(), enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets);

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.LessOrEqual(3F, moves.Single().Power);
            Assert.GreaterOrEqual(3F + 2, moves.Single().Power);
        }

        [Test]
        public void Take_into_account_planet_growth_when_trying_to_take_over_planet()
        {
            var enemyHealth = 1F;
            var enemyDistanceTurns = 3;
            var enemyPlanet = CreateEnemyPlanet(health:enemyHealth, x: 0, y: enemyDistanceTurns * CH.ShipSpeed);
            var planets = new List<Planet> { CreatePlanet(), enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets);

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.LessOrEqual(enemyHealth + enemyPlanet.GrowthSpeed * enemyDistanceTurns, moves.Single().Power);
        }

        [Test]
        public void Take_into_account_incoming_enemy_reinforcements_when_trying_to_take_over_planet()
        {
            var enemyHealth = 1F;
            var enemyDistanceTurns = 3;
            var enemyPlanet = CreateEnemyPlanet(health:enemyHealth, x: 0, y: enemyDistanceTurns * CH.ShipSpeed);
            var planets = new List<Planet> { CreatePlanet(), enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets, new List<Ship> { CreateEnemyShip(enemyPlanet, enemyDistanceTurns, 3) });

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.LessOrEqual(enemyPlanet.GetHealthAtTurnKnown(enemyDistanceTurns).health, moves.Single().Power);
        }

        [Test]
        public void Dont_send_more_ships_when_already_enough_power_inbound_to_take_over_enemy_planet()
        {
            var enemyHealth = 1F;
            var enemyDistanceTurns = 3;
            var enemyPlanet = CreateEnemyPlanet(health:enemyHealth, x: 0, y: enemyDistanceTurns * CH.ShipSpeed);
            var planets = new List<Planet> { CreatePlanet(), enemyPlanet };
            Connect(planets[0], planets[1]);
            var gameState = CreateGameState(planets, new List<Ship> { CreateShip(enemyPlanet, enemyDistanceTurns, 10) });

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.AreEqual(0, moves.Length);
        }

        [Test]
        // biggest first because it generates health more quickly
        public void Obliterate_Biggest_Of_Two_Enemy_Planets()
        {
            var enemyHealth = 6F;
            var myPlanet = CreatePlanet();
            var enemyPlanet = CreateEnemyPlanet(health: enemyHealth, x: 1F, y: 0, radius: 10);
            var planets = new List<Planet> { myPlanet, CreateEnemyPlanet(health: enemyHealth, x: 0, y: 1F, radius: 1), enemyPlanet };
            Connect(planets[0], planets[1]);
            Connect(planets[0], planets[2]);
            var gameState = CreateGameState(planets);

            var moves = TheMoleStrategy.PlayTurn(gameState, 1);

            Assert.AreEqual(1, moves.Length);
            Assert.AreEqual(myPlanet.Id, moves.Single().Source);
            Assert.AreEqual(enemyPlanet.Id, moves.Single().Target);
            Assert.LessOrEqual(enemyHealth, moves.Single().Power);
        }

        private static Ship CreateShip(BarePlanetState target, int distanceFromTarget, int power, int owner=0)
        {
            return new Ship { Owner = owner, TargetId = target.Id, X = 0, Y = (distanceFromTarget - 1) * CH.ShipSpeed, Power = power };
        }

        private static Ship CreateEnemyShip(BarePlanetState target, int distanceFromTarget, int power)
        {
            return CreateShip(target, distanceFromTarget, power, 1);
        }

        private static void Connect(BarePlanetState p1, BarePlanetState p2)
        {
            p1.Neighbors = p1.Neighbors.Concat(new[] { p2.Id }).ToArray();
            p2.Neighbors = p2.Neighbors.Concat(new[] { p1.Id }).ToArray();
        }

        private static Planet CreatePlanet(float health = 10F, int radius = 20, int? owner = 0, float x = 0F, float y = 0F)
        {
            return new Planet { Id = _id++, Health = health, Owner = owner, Radius = radius, X = x, Y = y, Neighbors = new int[0] };
        }

        private static Planet CreateEnemyPlanet(float health = 10F, int radius = 20, float x = 0F, float y = 0F)
        {
            return CreatePlanet(health, radius, owner: 1, x, y);
        }

        private static GameState CreateGameState(List<Planet> planets, List<Ship> ships = null)
        {
            ships = ships ?? new List<Ship>();
            return new GameState(new Settings { PlayerId = 0, Players = 2 }) { Planets = planets, Ships = ships };
        }
    }
}