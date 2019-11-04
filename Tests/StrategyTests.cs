using System.Collections.Generic;
using System.Diagnostics;
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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

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

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

            Assert.AreEqual(1, moves.Length);
            Assert.AreEqual(myPlanet.Id, moves.Single().Source);
            Assert.AreEqual(enemyPlanet.Id, moves.Single().Target);
            Assert.LessOrEqual(enemyHealth, moves.Single().Power);
        }

        [Test]
        public void AttackNeutralWithTwoPlanets()
        {
            var planet1 = CreatePlanet(29.9F, 26.693428F, 0, 200.47342F, 150.36024F);
            var planet2 = CreatePlanet(19.9F, 30.496107F, 0, 93.75485F, 61.48056F);
            var enemyHealth = 47F;
            var planetNeutral = CreatePlanet(enemyHealth, 28.83773F, owner: null, 83.12419F, 237.5396F);
            
            Connect(planet1, planet2);
            Connect(planet1, planetNeutral);
            Connect(planet2, planetNeutral);
            var gameState = CreateGameState(new List<Planet> { planet1, planet2, planetNeutral });

            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());

            Assert.AreEqual(2, moves.Length);
            Assert.AreEqual(1, moves.Count(m => m.Source == planet1.Id));
            Assert.AreEqual(1, moves.Count(m => m.Source == planet2.Id));
            Assert.AreEqual(2, moves.Count(m => m.Target == planetNeutral.Id));
            Assert.LessOrEqual(enemyHealth, moves.Sum(m => m.Power));
        }

//        [Test]
//        public void WageWarForPlanet()
//        {
//            var planet1 = CreatePlanet(27F, 24.585754F, 1, 552.32776F, 239.91684F);
//            var planetEnemy = CreatePlanet(3F, 27.995485F, 0, 275.55322F, 220.97034F);
//            var neutralHealth = 3F;
//            var planetNeutral = CreatePlanet(neutralHealth, 38.397026F, owner: null, 443.6608F, 187.52002F);
//            
//            Connect(planet1, planetEnemy);
//            Connect(planet1, planetNeutral);
//            Connect(planetEnemy, planetNeutral);
//            var gameState = CreateGameState(new List<Planet> { planet1, planetEnemy, planetNeutral });
//            gameState.Ships
//
//            var moves = TheMoleStrategy.PlayTurn(gameState, 1, new Stopwatch());
//
//            Assert.AreEqual(2, moves.Length);
//            Assert.AreEqual(1, moves.Count(m => m.Source == planet1.Id));
//            Assert.AreEqual(1, moves.Count(m => m.Source == planetEnemy.Id));
//            Assert.AreEqual(2, moves.Count(m => m.Target == planetNeutral.Id));
//            Assert.LessOrEqual(neutralHealth, moves.Sum(m => m.Power));
//        }

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

        private static Planet CreatePlanet(float health = 10F, float radius = 20, int? owner = 0, float x = 0F, float y = 0F)
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