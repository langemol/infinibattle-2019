using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StarterBot.Models
{
    public class GameState
    {
        public GameState(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }
        public List<Planet> Planets { get; set; }
        public List<Ship> Ships { get; set; }

        public float GetHealthPlayerTotal(int playerId)
        {
            return PlanetsHealthTotalForPlayer(playerId) + ShipsPowerTotalForPlayer(playerId);
        }
        // HealthHostileTotal //ForUser[] not necessary in 2player

        private float PlanetsHealthTotalForPlayer(int playerId) => Planets.Where(p => p.Owner.HasValue && p.Owner.Value == playerId).Sum(p => p.Health);
        private float ShipsPowerTotalForPlayer(int playerId) => Ships.Where(s => s.Owner.HasValue && s.Owner.Value == playerId).Sum(s => s.Power);

        // do something with health per section? (divide grid in sections)

        public ImmutableSortedDictionary<int, Planet> PlanetsById { get; set; }


        // -- Calculate things at turn start --
        public void SetRelationships()
        {
            Planets.ForEach(p=>p.Friendlyness = DetermineFriendlyness(p.Owner));
            Ships.ForEach(s=>s.Friendlyness = DetermineFriendlyness(s.Owner));

            Planets.ForEach(p=>p.NeighboringPlanets = Planets.Where(n=>p.Neighbors.Contains(n.Id)).OrderBy(n=>n.DistanceTo(p)).ToList());

            Ships.ForEach(s => s.Target = Planets.Single(p => p.Id == s.TargetId));
            Planets.ForEach(p => p.SetInboundShips(Ships.Where(s => s.TargetId == p.Id)));
        }

        private Friendlyness DetermineFriendlyness(int? owner)
        {
            if (!owner.HasValue)
            {
                return Friendlyness.Neutral;
            }

            return owner == Settings.PlayerId ? Friendlyness.Owner : Friendlyness.Hostile;
        }

        public void DoInitialCalculations()
        {
            PlanetsById = Planets.ToImmutableSortedDictionary(p => p.Id, p => p);

            SetRelationships();

            Planets.ForEach(p=>p.NeighbouringPlanetsDistanceTurns = CalculateNeighbouringPlanetsDistanceTurns(p));
            Planets.ForEach(p=>p.ShortestPaths = CalculateShortestPaths(p));
        }

        private List<OtherPlanet> CalculateShortestPaths(Planet planet)
        {
            // TODO Testen
            var planetShortestPaths = new Dictionary<int, (int d, Planet via)>(Planets.Count);
            planetShortestPaths[planet.Id] = (0, planet);

            CalculateShortestPaths(planetShortestPaths, 0, planet);
            
            return planetShortestPaths.Select(p => new OtherPlanet(p.Value.d, PlanetsById[p.Key], p.Value.via)).OrderBy(p=>p.TurnsToReach).ToList();
        }

        private static void CalculateShortestPaths(Dictionary<int, (int d, Planet via)> planetShortestPaths, int d, Planet neighbour)
        {
            foreach (var x in neighbour.NeighbouringPlanetsDistanceTurns)
            {
                var newD = d + x.TurnsToReach;
                if (!planetShortestPaths.ContainsKey(x.Target.Id) ||
                    newD < planetShortestPaths[x.Target.Id].d)
                {
                    planetShortestPaths[x.Target.Id] = (newD, neighbour);

                    CalculateShortestPaths(planetShortestPaths, newD, x.Target);
                }
            }
        }

        private static List<NeighbouringPlanet> CalculateNeighbouringPlanetsDistanceTurns(Planet p)
        {
            return p.NeighboringPlanets.Select(n => new NeighbouringPlanet(p.DistanceTo(n), n)).OrderBy(n=>n.TurnsToReach).ToList();
        }
    }

    public class NeighbouringPlanet
    {
        public NeighbouringPlanet(int turnsToReach, Planet target)
        {
            TurnsToReach = turnsToReach;
            Target = target;
        }

        public int TurnsToReach { get; }
        public Planet Target { get; }
    }
    public class OtherPlanet : NeighbouringPlanet
    {
        public Planet Via { get; }

        public OtherPlanet(int turnsToReach, Planet target, Planet via) : base(turnsToReach, target)
        {
            Via = via;
        }
    }
}