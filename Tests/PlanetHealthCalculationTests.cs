using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarterBot;
using StarterBot.Models;

namespace Tests
{
    public class PlanetHealthCalculationTests
    {
        private static int _id;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Growth_is_added_in_HealthNextTurn()
        {
            var health = 1F;
            var planet = CreatePlanet(health);

            var healthNextTurn = planet.GetHealthAtTurnKnown(1);
            
            Assert.AreEqual(health + planet.GrowthSpeed, healthNextTurn);
        }

        [Test]
        public void GetHealthAtTurnKnown()
        {
            var health = 1F;
            var planet = CreatePlanet(health, owner: null);

            var distanceFromTarget = 7;
            planet.SetInboundShips(new Ship[] { CreateShip(planet, distanceFromTarget, 2) });

            Assert.AreEqual(1, planet.InboundShips.Count);
            Assert.AreEqual(7, planet.InboundShips.Single().TurnsToReachTarget);

            var healthNextTurn = planet.GetHealthAtTurnKnown(7);

            Assert.IsTrue(healthNextTurn.ownerChanged);
            Assert.AreEqual(0, healthNextTurn.owner);
        }

        private static Ship CreateShip(Planet target, int distanceFromTarget, int power, int owner=0)
        {
            var ship = new Ship { Owner = owner, TargetId = target.Id, X = 0, Y = distanceFromTarget * CH.ShipSpeed, Power = power };
            ship.Target = target;
            return ship;
        }

        private static Planet CreatePlanet(float health = 10F, int radius = 20, int? owner = 0, float x = 0F, float y = 0F)
        {
            var p = new Planet { Id = _id++, Health = health, Owner = owner, Radius = radius, X = x, Y = y, Neighbors = new int[0] };
            p.SetInboundShips(new List<Ship>());
            return p;
        }
    }
}