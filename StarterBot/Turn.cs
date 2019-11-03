using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StarterBot.Models;

namespace StarterBot
{
    internal class Turn
    {
        private const float PlanetMinHealth = 1.0F;
        private const int MaxTurns = 500;

        private readonly int _turn;
        private readonly GameState _gameState;
        private readonly List<Move> _moves;
        private readonly int _me;
        private readonly int _enemyPlayer;

        public Turn(int turn, GameState gameState)
        {
            _turn = turn;
            _gameState = gameState;

            _me = _gameState.Settings.PlayerId;
            _enemyPlayer = _gameState.Settings.Enemy;
            
            _moves = new List<Move>();
        }

        public List<Move> Play(Stopwatch watch)
        {
// first turn: 1 planet each, same size
            // get neutral planets nearest / highest TimeToValue / Biggest!
            // (afhankelijk van hoe ver weg de tegenstander is, Bigger in begin better dan goedkoop klein planeetje vanwege verwachte langere duur spel)
            // TODO iets bedenken voor combinatie TimeToValue en nabijheid vijandelijke forces (of verwachte spelduur?)
            // TODO (neutral) planeet (veroveren) dichterbij vijand beter of juist niet?


            // check planets that turn from owner

            var healthQ = _gameState.GetHealthPlayerTotal(_me) / _gameState.GetHealthPlayerTotal(_enemyPlayer); // > 1 => winning! more aggressive?

            // Meer vanuit meerdere planeten tegelijk denken, vanuit meerdere planeten tegelijk naar 1 gezamenlijke neighbour sturen

            var myPlanets = _gameState.Planets.Where(PH.IsMine).ToList();
            var planetsThatNeedHelp = GetPlanetsThatNeedHelp(myPlanets);
            var planetsThatCanAttack = new List<Planet>(myPlanets);
            
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} Checked helpless");
            HelpPlanets(planetsThatNeedHelp, planetsThatCanAttack);
            
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} helped helpless");
            
            var possibleTargets = GetPossibleTargets();
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} Checked targets");
            if (possibleTargets.hostiles.Any())
            {
                var target = possibleTargets.hostiles.First();
                var sources = target.NeighboringFriendlyPlanets;

                var source = sources.First();
                var sourceHealth = source.Target.Health - PlanetMinHealth;
                var targetHealth = target.GetHealthAtTurnKnown(source.TurnsToReach).health;

                var powerNeeded = targetHealth + PlanetMinHealth;
                if (sourceHealth >= powerNeeded) // only if enemy planet can be taken
                {
                    AddMove(powerNeeded, source.Target, target);// TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                }

                // TODO loopje
                var source2 = sources.Skip(1).FirstOrDefault();
                if (source2 != null)
                {
                    var source2Health = source2.Target.Health - PlanetMinHealth;
                    var targetHealth2 = target.GetHealthAtTurnKnown(source2.TurnsToReach);
                    if (targetHealth2.owner != _me)
                    {
                        var powerNeeded2 = targetHealth2.health + PlanetMinHealth;
                        if (sourceHealth + source2Health >= powerNeeded2) // only if enemy planet can be taken
                        {
                            var hq = (sourceHealth + source2Health - powerNeeded2) / (sourceHealth + source2Health);
                            // TODO half/half? dichtste meest? dichtste wachten tot even ver?
                            AddMove(hq * sourceHealth, source.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                            AddMove(hq * source2Health, source2.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                        }
                    }
                }
            }
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} sent to hostiles");

            if (possibleTargets.neutrals.Any())
            {
                var target = possibleTargets.neutrals.First();
                var sources = target.NeighboringFriendlyPlanets;

                var source = sources.First();
                var sourceHealth = source.Target.Health - PlanetMinHealth;
                var targetHealth = target.GetHealthAtTurnKnown(source.TurnsToReach).health;

                var powerNeeded = targetHealth + PlanetMinHealth;
                if (sourceHealth >= powerNeeded) // only if enemy planet can be taken
                {
                    AddMove(powerNeeded, source.Target, target);// TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                }

                // TODO loopje
                var source2 = sources.Skip(1).FirstOrDefault();
                if (source2 != null)
                {
                    var source2Health = source2.Target.Health - PlanetMinHealth;
                    var targetHealth2 = target.GetHealthAtTurnKnown(source2.TurnsToReach);
                    if (targetHealth2.owner != _me)
                    {
                        var powerNeeded2 = targetHealth2.health + PlanetMinHealth;
                        if (sourceHealth + source2Health >= powerNeeded2) // only if enemy planet can be taken
                        {
                            var hq = (sourceHealth + source2Health - powerNeeded2) / (sourceHealth + source2Health);
                            // TODO half/half? dichtste meest? dichtste wachten tot even ver?
                            AddMove(hq * sourceHealth, source.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                            AddMove(hq * source2Health, source2.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                        }
                    }
                }
            }
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} sent to neutrals");

            // bij checken of je non-friendly planet wil aanvallen ook checken of je er niet al troepen heen hebt gestuurd
            foreach (var planet in planetsThatCanAttack)
            {
//                Console.WriteLine($"# {planet.Id}");
                if (planet.NeighboringPlanets.All(PH.IsMine))// misschien ook als alleen Mine of Neutral, wanneer er andere planet is die wel NeighbouringEnemyPlanet heeft, om meer te focussen op enemy?
                {// TODO mag ook als 1tje niet van mij is allemaal naar die sturen
//                    Console.WriteLine($"# {planet.Id} surrounded by friendly");
                    if (!planet.InboundShips.Any(PH.IsHostile))
                    {
                        // TODO rekening houden met dat 1 van die planeten binnenkort naar de enemy gaat
                        var planetThatNeedsReinforcementsMost = 
                            planet.NeighboringPlanets.OrderBy(p=>p.NearestEnemyPlanetTurns).First(); // TODO which planet is that, houd rekening met HealthMax
                        // TODO divide between multiple planets?
                        var movePower = planet.Health - PlanetMinHealth;
                        AddMove(movePower, planet, planetThatNeedsReinforcementsMost);
                    }
                    continue;
                }

                // check of planeet wel health kan missen
                var enemies = planet.NeighboringHostilePlanets.ToList();
                //planet.HealthPossibleToReceiveInTurns(enemyPlayer);
//                if (enemies.Any())
//                {
//                    foreach (var enemy in enemies)
//                    {
//                        var health = planet.GetHealthAtTurnKnown(enemy.TurnsToReach);
//                        var enemyHealth = enemy.Target.Health;
//                    }
//                }

                var closestEnemy = planet.NeighbouringPlanetsDistanceTurns.FirstOrDefault(n => PH.IsHostile(n.Target));
                var closestNeutral = planet.NeighbouringPlanetsDistanceTurns.FirstOrDefault(n => PH.IsNeutral(n.Target));
                var target = closestEnemy ?? closestNeutral; // kan niet null zijn want eerste if checkt alles Mine
//                var healthAtTurn = planet.GetHealthAtTurnKnown(target.TurnsToReach).health;

                var targetHealth = target.Target.GetHealthAtTurnKnown(target.TurnsToReach);
                if (targetHealth.owner == _me)//is al van mij?
                {
                    //choose other planet :) choose planet in loopje?
                    continue;
                }

                var powerNeeded = targetHealth.health + PlanetMinHealth;
                var planetHealth = planet.Health- PlanetMinHealth;
//                Console.WriteLine($"# {planet.Id} targetting {target.Target.Id} with {planetHealth} against {powerNeeded}");
                if (planetHealth > powerNeeded) // only if enemy planet can be taken
                {
                    AddMove(powerNeeded, planet, target.Target);
                    continue;
                }

                if (planet.HealthLimitAboutToBeExceeded) // too much health!
                {
                    var target2 = planet.NeighboringPlanets.First();
                    AddMove(planet.GrowthSpeed, planet, target2);
                }
            }

            return _moves;
        }

        private void HelpPlanets(List<(Planet p, int turn, float healthNeeded)> planetsThatNeedHelp, List<Planet> planetsThatCanAttack)
        {
            foreach (var planet in planetsThatNeedHelp)
            {
//                Console.WriteLine($"# planet {planet.p.Id} needs help");
                planetsThatCanAttack.Remove(planet.p);

                // van welke planeet (of combinatie van) kan hulp het beste komen?
                var neighbours = planet.p.NeighboringFriendlyPlanets;
                // evt aan te vullen door NeighboringFriendlyPlanet ook te markeren als 'needsHelp'?
                var planetsThatCanEasilyHelp = new List<Planet>();
                foreach (var neighbour in neighbours)
                {
                    if (!neighbour.Target.NeighboringHostilePlanets.Any())
                    {
                        planetsThatCanEasilyHelp.Add(neighbour.Target);
                    }
                }

                var healthNeeded = planet.healthNeeded;
                SendHelp(planetsThatCanEasilyHelp, healthNeeded, planet);

                foreach (var hostiles in planet.p.InboundShips.Where(PH.IsHostile).GroupBy(s => s.TurnsToReachTarget).Skip(1))
                {
                    var health = planet.p.GetHealthAtTurnKnown(hostiles.First().TurnsToReachTarget).health;
                    if (health < 0) // or PlanetMinHealth
                    {
                        SendHelp(planetsThatCanEasilyHelp, health * -1, planet);
                    }
                }
            }
        }

        private void SendHelp(List<Planet> planetsThatCanEasilyHelp, float healthNeeded,
            (Planet p, int turn, float healthNeeded) planet)
        {
            foreach (var helper in planetsThatCanEasilyHelp)
            {
                var helperHealth = helper.Health- PlanetMinHealth;
                var power = Math.Min(healthNeeded, helperHealth);
                AddMove(power, helper, planet.p);
                healthNeeded -= power;
                if (healthNeeded <= 0) break;
            }
        }

        private (IEnumerable<Planet> hostiles, IEnumerable<Planet> neutrals) GetPossibleTargets()
        {
            var possibleNeutralTargets = new List<(Planet, int)>();
            var possibleHostileTargets = new List<(Planet, int)>();

            var closestEnemyPlanets = _gameState.Planets.Where(PH.IsHostile)
                .Select(h => (h,h.GetNearestPlanet(Friendlyness.Owner)))
                .OrderBy(o => o.Item2.TurnsToReach);
            var neighbouringEnemyPlanets = closestEnemyPlanets.Where(e => e.Item2.Via == e.h);
            foreach (var nep in neighbouringEnemyPlanets)
            {
                var (health, owner, ownerChanged) = nep.h.GetHealthAtTurnKnown(nep.Item2.TurnsToReach);

                if (ownerChanged)
                {
                    continue;
                }

                var ttv = nep.h.TimeToValueEnemyAdjusted(_turn, nep.Item2.TurnsToReach, health);
                if (ttv >= MaxTurns) continue;
                possibleHostileTargets.Add((nep.h, ttv));
            }

            var closestNeutralPlanets = _gameState.Planets.Where(PH.IsNeutral)
                .Select(h => (h,h.GetNearestPlanet(Friendlyness.Owner)))
                .OrderBy(o => o.Item2.TurnsToReach);
            var neighbouringNeutralPlanets = closestNeutralPlanets.Where(e => e.Item2.Via == e.h);
            foreach (var nnp in neighbouringNeutralPlanets)
            {
                var (health, owner, ownerChanged) = nnp.h.GetHealthAtTurnKnown(nnp.Item2.TurnsToReach);

                if (ownerChanged)
                {
                    if (owner == _me)
                    {
                        continue;
                    }

                    var ttv2 = nnp.h.TimeToValueEnemyAdjusted(_turn, nnp.Item2.TurnsToReach, health);
                    if (ttv2 >= MaxTurns) continue;
                    possibleHostileTargets.Add((nnp.h, ttv2));
                }

                var ttv = nnp.h.TimeToValueNeutralAdjusted(_turn, nnp.Item2.TurnsToReach, health);
                if (ttv >= MaxTurns) continue;
                possibleNeutralTargets.Add((nnp.h, ttv));
            }

            var hostiles = possibleHostileTargets.OrderBy(t=>t.Item2).Select(t=>t.Item1).ToList();
            var neutrals = possibleNeutralTargets.OrderBy(t=>t.Item2).Select(t=>t.Item1).ToList();
            return (hostiles, neutrals);
        }

        private List<(Planet p, int turn, float healthNeeded)> GetPlanetsThatNeedHelp(IEnumerable<Planet> myPlanets)
        {
            var planetsWithInboundHostiles = myPlanets.Where(p => p.InboundShips.Any(PH.IsHostile));
            var planetsThatNeedHelp = new List<(Planet p, int turn, float healthNeeded)>();
            foreach (var planet in planetsWithInboundHostiles)
            {
                foreach (var hostile in planet.InboundShips.Where(PH.IsHostile))
                {
                    var health = planet.GetHealthAtTurnKnown(hostile.TurnsToReachTarget);
                    if (health.ownerChanged) // or PlanetMinHealth
                    {
                        planetsThatNeedHelp.Add((planet, hostile.TurnsToReachTarget, health.health * -1));
                        break;
                    }
                }
            }

            foreach (var target in _gameState.Ships.Where(PH.IsMine).Where(s => !PH.IsMine(s.Target)).GroupBy(s => s.Target))
            {
                var lastShip = target.OrderByDescending(s => s.TurnsToReachTarget).First();
                var status = target.Key.GetHealthAtTurnKnown(lastShip.TurnsToReachTarget);
                if (status.owner != _me)
                {
                    planetsThatNeedHelp.Add((target.Key, lastShip.TurnsToReachTarget, status.health));
                }
                // TODO voor deze planeten ook voor langere tijd checken, net als foreach hierboven!!
            }

            return planetsThatNeedHelp;
        }

        private void AddMove(float movePower, Planet source, Planet target)
        {
            _moves.Add(new Move(movePower, source.Id, target.Id));
            AdjustGamestateForNewMove(movePower, source, target);
        }

        /// <summary>
        /// als je alles van te voren compleet netjes bedenkt is dit niet nodig
        /// </summary>
        private void AdjustGamestateForNewMove(float movePower, Planet source, Planet target)
        {
            var newShip = new Ship
            {
                Friendlyness = Friendlyness.Owner, Owner = _gameState.Settings.PlayerId, Power = movePower, Target = target,
                TargetId = target.Id, X = source.X, Y = source.Y
            };
            source.Health -= movePower;
            _gameState.Ships.Add(newShip);
            var targetInboundShips = target.InboundShips;
            targetInboundShips.Add(newShip);
            target.SetInboundShips(targetInboundShips);
        }
    }
}
