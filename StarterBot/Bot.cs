using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using StarterBot.Models;

namespace StarterBot
{
    public static class Bot
    {
        public static void Start(Func<GameState, int, Stopwatch, Move[]> strategy)
        {
            var gamestate = InitGameState();

            var turn = 0;
            string line;
            while ((line = Console.ReadLine()) != "game-end")
            {
                var watch = Stopwatch.StartNew();
                turn++;
                if (line != "turn-init") throw new Exception($"Expected 'turn-init', got '{line}'");

                var planets = ReadPlanets();
                var ships = ReadShips();

                line = Console.ReadLine();
                if (line != "turn-start") throw new Exception($"Expected 'turn-start', got '{line}");
//                Console.WriteLine($"# {watch.ElapsedMilliseconds} Read input ");
                AdjustGamestateForTurn(turn, gamestate, planets, ships);
                
//                Console.WriteLine($"# {watch.ElapsedMilliseconds} Adjusted gamestate");
                var moves = strategy.Invoke(gamestate, turn, watch);
                WriteMoves(moves);
            }
        }

        private static void WriteMoves(Move[] moves)
        {
            foreach (var move in moves)
            {
                Console.WriteLine(move);
            }

            Console.WriteLine("end-turn");
        }

        public static GameState InitGameState()
        {
            var settings = new Settings
            {
                Seed = ReadInt("seed"),

                Players = ReadInt("num-players"),
                PlayerId = ReadInt("player-id")
            };
            return new GameState(settings);
        }

        public static void AdjustGamestateForTurn(int turn, GameState gamestate, BarePlanetState[] planets, Ship[] ships)
        {
            if (turn == 1)
            {
                var gamePlanets = MapToGamePlanets(planets);
                gamestate.Planets = gamePlanets;
                gamestate.Ships = new List<Ship>(ships);
            }
            else
            {
                AdjustForTurn(gamestate, planets, ships);
            }
        }

        private static List<Planet> MapToGamePlanets(IEnumerable<BarePlanetState> planets)
        {
            var result = new List<Planet>();
            foreach (var planet in planets)
            {
                result.Add(new Planet
                {
                    Id = planet.Id,
                    Health = planet.Health,
                    X = planet.X,
                    Y = planet.Y,
                    Owner = planet.Owner,
                    Radius = planet.Radius,
                    Neighbors = planet.Neighbors
                });
            }

            return result;
        }

        private static void AdjustForTurn(GameState gamestate, BarePlanetState[] planets, Ship[] ships)
        {
            var gamePlanets = gamestate.PlanetsById;
            AdjustForTurn(gamePlanets, planets);

            gamestate.Ships = new List<Ship>(ships);
        }

        private static void AdjustForTurn(ImmutableSortedDictionary<int, Planet> gamePlanets, BarePlanetState[] newState)
        {
            for (var i = 0; i<newState.Length;i++)
//            foreach (var planet in newState)
            {
                AdjustForTurn(gamePlanets[newState[i].Id], newState[i]);
            }
        }

        private static void AdjustForTurn(Planet gamePlanet, BarePlanetState newState)
        {
            gamePlanet.Health = newState.Health;
            gamePlanet.Owner = newState.Owner;
        }

        private static string ReadValue(string key)
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 2 || parts[0] != key)
            {
                throw new Exception($"Excepted '{key} <value>', got '{line}'");
            }

            return parts[1];
        }

        private static int ReadInt(string key)
        {
            return int.Parse(ReadValue(key));
        }

        private static float ReadFloat(string key)
        {
            return float.Parse(ReadValue(key));
        }

        public static BarePlanetState[] ReadPlanets()
        {
            var planetCount = ReadInt("num-planets");
            var planets = new BarePlanetState[planetCount];

            for (var i = 0; i < planetCount; i++)
            {
                planets[i] = ReadPlanet();
            }

            return planets;
        }

        private static BarePlanetState ReadPlanet()
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 7 || parts[0] != "planet")
            {
                throw new Exception($"Expected 'planet <id> <x> <y> <radius> <owner> <health>', got '{line}'");
            }

            return new BarePlanetState
            {
                Id = int.Parse(parts[1]),
                X = float.Parse(parts[2]),
                Y = float.Parse(parts[3]),
                Radius = float.Parse(parts[4]),
                Owner = ParseOwner(parts[5]),
                Health = float.Parse(parts[6]),
                Neighbors = ReadNeighbors(),
            };
        }

        private static int[] ReadNeighbors()
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length == 0 || parts[0] != "neighbors")
            {
                throw new Exception($"Expected 'neighbors <neighbor1> <neighbor2> ...', got '{line}'");
            }

            return parts.Skip(1).Select(int.Parse).ToArray();
        }

        public static Ship[] ReadShips()
        {
            var shipCount = ReadInt("num-ships");
            var ships = new Ship[shipCount];

            for (var i = 0; i < shipCount; i++)
            {
                ships[i]=ReadShip();
            }

            return ships;
        }

        private static Ship ReadShip()
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 6 || parts[0] != "ship")
            {
                throw new Exception($"Expected 'ship <x> <y> <target_id> <owner> <power>', got '{line}'");
            }

            return new Ship
            {
                X = float.Parse(parts[1]),
                Y = float.Parse(parts[2]),
                TargetId = int.Parse(parts[3]),
                Owner = ParseOwner(parts[4]),
                Power = float.Parse(parts[5])
            };
        }

        private static int? ParseOwner(string owner)
        {
            if (owner == "neutral")
            {
                return null;
            }

            return int.Parse(owner);
        }
    }
}