using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using MoreLinq;

namespace StarterBot.Models
{
    public class BarePlanetState : IHasCoordinates
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// Between 20.0 and 40.0
        /// </summary>
        public float Radius { get; set; }

        public int? Owner { get; set; }

        public float Health { get; set; }

        public int[] Neighbors { get; set; } 
    }

    public class Planet : BarePlanetState, IProperty
    {
        public Friendlyness Friendlyness { get; set; }


        public float GrowthSpeed => 0.05f * Radius;
        public float HealthMax => 5.0f * Radius; // check Health/HealthMax or
        public bool HealthLimitAboutToBeExceeded => Health + GrowthSpeed > HealthMax;// alleen interessant voor Mine/Owner?
        /// <summary>
        /// if not owner,
        /// turns until investment is returned
        /// LET OP: dit is gezien vanuit GameTotalHealth, vanuit de planeet gezien moet je 1 of 2x de distance erbij optellen
        /// </summary>
        public int TimeToValue(float health) => (int) Math.Ceiling(health / GrowthSpeed);
        private int TimeToValue(float healthAtTurn, int turnsToReach) => TimeToValue(healthAtTurn) + turnsToReach;

        public int TimeToValueNeutralAdjusted(int turnInGame, int turnsToReach, float healthAtTurn)
        {
            var ttv = TimeToValue(healthAtTurn, turnsToReach);
            var turnValueFrom = turnInGame + ttv;
            if (turnValueFrom > MaxGameTurns)
            {
                return MaxGameTurns;
            }

            if (InboundHostileShips.Any(s => s.TurnsToReachTarget > turnsToReach) && healthAtTurn>GrowthSpeed)
            {
                return MaxGameTurns; // wacht lekker ff tot enemy zich tegen neutral heeft verzwakt
            }

            return ttv; //* GrowthSpeed;
//            turnInGame / MaxGameTurns;
            // also consider enemy nearness?
            // consider enemy inbound ships arriving after turnsToReach
        }

        public int TimeToValueEnemyAdjusted(int turnInGame, int turnsToReach, float healthAtTurn)
        {
            var ttv = TimeToValue(healthAtTurn, turnsToReach);
            

            return ttv; //* GrowthSpeed;
//            turnInGame / MaxGameTurns;
            // also consider enemy nearness?
            // consider enemy inbound ships arriving after turnsToReach
        }

        public int MaxGameTurns = 500;

        /// <summary>
        /// Ordered by distance
        /// </summary>
        public List<Planet> NeighboringPlanets { get; set; } // calculated at game start
        public IEnumerable<NeighbouringPlanet> NeighboringFriendlyPlanets => NeighbouringPlanetsDistanceTurns.Where(p=>p.Target.Friendlyness==Friendlyness.Owner);
        public IEnumerable<NeighbouringPlanet> NeighboringHostilePlanets => NeighbouringPlanetsDistanceTurns.Where(p=>p.Target.Friendlyness==Friendlyness.Hostile);
        // + neutral..

        /// <summary>
        /// Ordered by nearness
        /// </summary>
        public List<Ship> InboundShips { get; set; }
        public List<Ship> InboundHostileShips { get; private set; } // calculated at turn start
        public void SetInboundShips(IEnumerable<Ship> ships)
        {
            HealthAtTurnKnown=new Dictionary<int, (float health, int? owner, bool ownerChanged)>(NeighbouringPlanetsDistanceTurns?.Last()?.TurnsToReach??0);
            InboundShips = ships.OrderBy(s => s.TurnsToReachTarget).ToList();
            InboundHostileShips = InboundShips.Where(s=>PH.IsHostile(s)).ToList();
            HealthDiffInboundForTurns =
                InboundShips.GroupBy(s => s.TurnsToReachTarget, s => s).ToDictionary(g => g.Key,
                    g => g.GroupBy(s2=>s2.Owner).Select(s=>(s.Key,s.Sum(s2 => s2.Power))).ToList()
                    );
        }

        private Dictionary<int,(float health, int? owner, bool ownerChanged)> HealthAtTurnKnown { get; set; }
        /// <summary>
        /// based on known inbound ships + growth!
        /// TODO check if Ship arrives first or new Health is added first!!
        /// werkt alleen tot owner change
        /// </summary>
        // TODO calculate at turn start
        public (float health, int? owner, bool ownerChanged) GetHealthAtTurnKnown(int turn)
        {
            if (!HealthAtTurnKnown.TryGetValue(turn, out var cachedValue))
            {
                var statusAtTurnStart = turn <= 1 ? (Health, Owner, false) : GetHealthAtTurnKnown(turn - 1);
                var growth = statusAtTurnStart.Item2 == null ? 0 : GrowthSpeed; // neutrals groeien niet
                var healthDiffByOwner = HealthDiffInboundForTurn(turn);
                var healthDiff = healthDiffByOwner.Sum(h => h.Item2 * (h.Key == statusAtTurnStart.Item2 ? 1 : -1));
                var health = Math.Min(HealthMax, statusAtTurnStart.Item1 + growth + healthDiff);
                var newStatus = (health, statusAtTurnStart.Item2, statusAtTurnStart.Item3);
                if (health < 0)
                {
                    newStatus.health = health * -1; // TODO dit klopt niet bij meerdere inbound ships van verschillende owners
                    newStatus.Item2 = healthDiffByOwner.OrderByDescending(h => h.Item2).First().Key;
                    newStatus.Item3 = true;
                }

                HealthAtTurnKnown.Add(turn, newStatus);
                return newStatus;
            }

            return cachedValue;
        }

        private List<(int? Key, float)> HealthDiffInboundForTurn(int turn) => HealthDiffInboundForTurns.GetValueOrDefault(turn, new List<(int? Key, float)>());
        /// <summary>
        /// based on known inbound ships (both friendly and hostile)
        /// </summary>
        private Dictionary<int, List<(int? Key, float)>> HealthDiffInboundForTurns { get; set; }

        public Dictionary<int, float> HealthPossibleToReceiveInTurns(int playerId)
        {
            void AddToResult(Dictionary<int, float> dictionary, OtherPlanet planet)
            {
                dictionary[planet.TurnsToReach] = dictionary.ContainsKey(planet.TurnsToReach) ? dictionary[planet.TurnsToReach] : 0 + planet.Target.Health;
            }

            // TODO dit berekent alleen met huidige health van planeten, daarna kan er ook nog elke turn growth gestuurd worden
            var result = new Dictionary<int, float>();
            var playerPlanets = ShortestPaths.Where(p=>p.Target.Owner == playerId);
            foreach (var planet in playerPlanets)
            {
                if (planet.Target == planet.Via)
                {
                    AddToResult(result, planet);
                }

                // TODO dat werkt niet echt met die korste paden wanneer er neutral planeten of van andere player planeten tussen zitten, dan is een ander pad wellicht beter.
                // kan iets proberen met kijken of kortste pad geheel van player is en anders deze planeet helemaal vergeten of penalty geven oid
                AddToResult(result, planet);
            }

            return result;
        }

        // turns to nearest hostile planet (not distance) or just check NeighboringHostilePlanets.Any()
        public OtherPlanet GetNearestPlanet(Friendlyness friendlyness)
        {
            return ShortestPaths.First(p => p.Target.Friendlyness == friendlyness);
        }

        public int NearestEnemyPlanetTurns => GetNearestPlanet(Friendlyness.Hostile).TurnsToReach;
        
        public List<OtherPlanet> ShortestPaths { get; set; }
        
        public List<NeighbouringPlanet> NeighbouringPlanetsDistanceTurns { get; set; }
    }

    public static class CH // CoordinateHelper
    {
        public const float ShipSpeed = 15.0f;

        private static int DistanceToTurns(float distance)
        {
            return (int) Math.Ceiling(distance / ShipSpeed);
        }

        public static int DistanceTo(this IHasCoordinates self, IHasCoordinates target)
        {
            return DistanceToTurns(DistanceTo(self.X, self.Y, target.X, target.Y));
        }

        private static float DistanceTo(float x, float y, float otherX, float otherY)
        {
            var dx = otherX - x;
            var dy = otherY - y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }

    public static class PH // PropertyHelper
    {
        public static bool IsHostile(IProperty p) => p.Friendlyness == Friendlyness.Hostile;
        public static bool IsMine(IProperty p) => p.Friendlyness == Friendlyness.Owner;
        public static bool IsNeutral(IProperty p) => p.Friendlyness == Friendlyness.Neutral;
    }

    public interface IProperty
    {
        int? Owner { get; set; }
        Friendlyness Friendlyness { get; set; }
    }

    public interface IHasCoordinates //..name?
    {
        float X { get; set; }
        float Y { get; set; }
    }
}