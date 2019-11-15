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
        private const float PlanetMinHealth = 1.0001F;//little extra to be sure, because of rounding
        private const float PlanetMinTakeoverHealth = .0001F;//little extra to be sure, because of rounding
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
            if (possibleTargets.hostiles.Any()) // TODO twee of meer... op basis van myPlanets.Count? misschien alleen Neutral? <---------------
            {
                var target = possibleTargets.hostiles.First();
                var sources = target.NeighboringFriendlyPlanets;

                var source = sources.First();
                var sourceHealth = source.Target.Health - PlanetMinHealth;
                var targetHealth = target.GetHealthAtTurnKnown(source.TurnsToReach).health;
                var powerNeeded = targetHealth + PlanetMinTakeoverHealth;
                if (CheckIfEnoughHealthToSendShips(powerNeeded, source.Target))
                {
                    // TODO Check voor target.EnemyNeighbours die dichterbij zijn dan source.TurnsToReach  (en extract dit ff naar een method?) <--------------
//                    if (sourceHealth >= powerNeeded) // only if enemy planet can be taken
//                    {
                    AddMove(powerNeeded, source.Target,
                        target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                }

                // TODO loopje
                var source2 = sources.Skip(1).FirstOrDefault();
                if (source2 != null && !source.Target.InboundHostileShips.Any() && !source2.Target.InboundHostileShips.Any())
                {
                    var source2Health = source2.Target.Health - PlanetMinHealth;
                    var targetHealth2 = target.GetHealthAtTurnKnown(source2.TurnsToReach);
                    if (targetHealth2.owner != _me)
                    {
                        var powerNeeded2 = targetHealth2.health + PlanetMinTakeoverHealth;
                        if (sourceHealth + source2Health >= powerNeeded2) // only if enemy planet can be taken
                        {
                            var hq = (powerNeeded2) / (sourceHealth + source2Health);
                            // TODO half/half? dichtste meest? dichtste wachten tot even ver?
                            AddMove(hq * sourceHealth, source.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                            AddMove(hq * source2Health, source2.Target,
                                target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                        }
                    }

//                    }
                }
            }
//            Console.WriteLine($"# {watch.ElapsedMilliseconds} sent to hostiles");

            if (possibleTargets.neutrals.Any())
            {
                var target = possibleTargets.neutrals.First();
                AttackNeutral(target);

                if (myPlanets.Count > 3)
                {
                    var target2 = possibleTargets.neutrals.Skip(1).FirstOrDefault();
                    if (target2 != null)
                    {
                        AttackNeutral(target2);
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
                    if (!planet.InboundHostileShips.Any()) // TODO zend zoveel als overblijft na laatste ship..
                    {
                        // TODO rekening houden met dat 1 van die planeten binnenkort naar de enemy gaat
                        var planetThatNeedsReinforcementsMost =
//                            planet.ShortestPaths.FirstOrDefault(p => PH.IsHostile(p.Target)).Via;
                            planet.NeighboringPlanets.OrderBy(p=>p.NearestEnemyPlanetTurns).First(); // TODO which planet is that, houd rekening met HealthMax
                        // TODO divide between multiple planets?
                        var movePower = planet.Health - PlanetMinHealth;
                        AddMove(movePower, planet, planetThatNeedsReinforcementsMost, false);
                    }
                    continue;
                }

                // check of planeet wel health kan missen
//                var enemies = planet.NeighboringHostilePlanets.ToList();
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

                var powerNeeded = targetHealth.health + PlanetMinTakeoverHealth;
//                var planetHealth = planet.Health- PlanetMinHealth;
//                Console.WriteLine($"# {planet.Id} targetting {target.Target.Id} with {planetHealth} against {powerNeeded}");
                if (CheckIfEnoughHealthToSendShips(powerNeeded, planet))//planetHealth > powerNeeded && !planet.InboundShips.Any(PH.IsHostile))) // only if enemy planet can be taken
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

        private void AttackNeutral(Planet target)
        {
            var sources = target.NeighboringFriendlyPlanets;

            var source = sources.First();
            var sourceHealth = source.Target.Health - PlanetMinHealth;
            var targetHealth = target.GetHealthAtTurnKnown(source.TurnsToReach).health;
            var powerNeeded = targetHealth + PlanetMinTakeoverHealth;
            if (CheckIfEnoughHealthToSendShips(powerNeeded, source.Target))
            {
//                    if (sourceHealth >= powerNeeded) // only if enemy planet can be taken
//                    {
                AddMove(powerNeeded, source.Target,
                    target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
            }

            // TODO loopje
            var source2 = sources.Skip(1).FirstOrDefault();
            if (source2 != null && !source.Target.InboundHostileShips.Any() &&
                !source2.Target.InboundHostileShips.Any())
            {
                // TODO DIT KAN EERDER <----------------------------------------------!!!!!!!!!!!!!!!!!!!
                var source2Health = source2.Target.Health - PlanetMinHealth;
//                    var targetHealth2 = target.GetHealthAtTurnKnown(source2.TurnsToReach);
//                    if (targetHealth2.owner != _me)
                {
//                        var powerNeeded2 = targetHealth2.health + PlanetMinTakeoverHealth;
                    if (sourceHealth + source2Health >= powerNeeded) // only if enemy planet can be taken
                    {
                        var hq = (powerNeeded) / (sourceHealth + source2Health);
                        // TODO half/half? dichtste meest? dichtste wachten tot even ver?
                        AddMove(hq * sourceHealth, source.Target,
                            target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                        AddMove(hq * source2Health, source2.Target,
                            target); // TODO meer sturen. Als andere enemy planet dichterbij is dan source, dan kunnen we elke turn allebei steeds een beetje sturen en blijft het alsnog van hem
                    }
                }

//                    }
            }
        }

        /// <summary>
        /// NOTE: source.Target is eigen planeet die ships wil gaan sturen
        /// </summary>
        /// <param name="powerNeeded"></param>
        /// <param name="sourcePlanet"></param>
        /// <returns></returns>
        private bool CheckIfEnoughHealthToSendShips(float powerNeeded, Planet sourcePlanet)
        {
            if (!sourcePlanet.InboundHostileShips.Any())
            {
                var sourceHealth = sourcePlanet.Health - PlanetMinHealth;
                return sourceHealth >= powerNeeded;
            }

            for (var i = 0; i < sourcePlanet.InboundHostileShips.Count; i++)
            {
                var inboundHostile = sourcePlanet.InboundHostileShips[i];
                var turnsToLastShip = inboundHostile.TurnsToReachTarget;
                var healthAtTurnKnown = sourcePlanet.GetHealthAtTurnKnown(turnsToLastShip);
                if (healthAtTurnKnown.owner != _me)
                    return
                        false; // eigenlijk moet je het minimale health in deze turns berekenen... kan zijn dat je akkoord geeft maar alsnog tussendoor daardoor planeet weggeeft


                if (healthAtTurnKnown.health <= powerNeeded) return false;
            }

            return true;
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

                foreach (var hostiles in planet.p.InboundHostileShips.GroupBy(s => s.TurnsToReachTarget).Skip(1))
                {
                    var health = planet.p.GetHealthAtTurnKnown(hostiles.First().TurnsToReachTarget);
                    if (health.owner != _me) // or PlanetMinHealth
                    {
                        SendHelp(planetsThatCanEasilyHelp, health.health, planet);
                    }
                }
            }
        }

        private void SendHelp(List<Planet> planetsThatCanEasilyHelp, float healthNeeded,
            (Planet p, int turn, float healthNeeded) planet)
        {
            healthNeeded += PlanetMinTakeoverHealth; // ietsje extra sturen, 0 is tricky
            foreach (var helper in planetsThatCanEasilyHelp)
            {
                var helperHealth = helper.Health- PlanetMinHealth;
                var power = Math.Min(healthNeeded, helperHealth);
                AddMove(power, helper, planet.p);
                healthNeeded -= power;
                if (healthNeeded <= 0) break;
            }
        }

        private (IList<Planet> hostiles, IList<Planet> neutrals) GetPossibleTargets()
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
            var planetsWithInboundHostiles = myPlanets.Where(p => p.InboundHostileShips.Any());
            var planetsThatNeedHelp = new List<(Planet p, int turn, float healthNeeded)>();
            foreach (var planet in planetsWithInboundHostiles)
            {
                Console.WriteLine($"# {planet.Id} has inbound hostiles");
                for (var i = 0; i < planet.InboundHostileShips.Count; i++)
                {
                    var hostile = planet.InboundHostileShips[i];
                    var health = planet.GetHealthAtTurnKnown(hostile.TurnsToReachTarget);
                    if (health.ownerChanged) // or PlanetMinHealth
                    {
                        Console.WriteLine($"# {planet.Id} needs help");
                        planetsThatNeedHelp.Add((planet, hostile.TurnsToReachTarget, health.health));
                        break;
                    }
                }
            }

            foreach (var target in _gameState.Ships.Where(s=>PH.IsMine(s)).Where(s => !PH.IsMine(s.Target)).GroupBy(s => s.Target))
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

        private void AddMove(float movePower, Planet source, Planet target, bool recalculate = true)
        {
            _moves.Add(new Move(movePower, source.Id, target.Id));

            // adjust game for new move
            var newShip = new Ship
            {
                Friendlyness = Friendlyness.Owner, Owner = _gameState.Settings.PlayerId, Power = movePower, Target = target,
                TargetId = target.Id, X = source.X, Y = source.Y
            };
            source.Health -= movePower;
            if (recalculate)
            {
                _gameState.Ships.Add(newShip);
                var targetInboundShips = target.InboundShips;
                targetInboundShips.Add(newShip);
                target.SetInboundShips(targetInboundShips);
            }
        }
    }
}
