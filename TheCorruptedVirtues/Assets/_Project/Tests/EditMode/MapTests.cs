using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the maps-as-data layer: ObstacleMap, BattleMapSpec/MapLibrary, the
    // obstacle-aware pathfinding seam (obstacles folded into occupancy), and
    // that every authored encounter spawns its units on legal tiles.
    public class ObstacleMapTests
    {
        [Test]
        public void TracksBlockedTiles()
        {
            var map = new ObstacleMap();
            Assert.That(map.IsBlocked(new GridCoord(1, 1)), Is.False);

            map.Add(new GridCoord(1, 1));
            Assert.That(map.IsBlocked(new GridCoord(1, 1)), Is.True);
            Assert.That(map.Blocked.Count, Is.EqualTo(1));

            map.Remove(new GridCoord(1, 1));
            Assert.That(map.IsBlocked(new GridCoord(1, 1)), Is.False);
        }
    }

    public class BattleMapSpecTests
    {
        [Test]
        public void BuildsElevationObstaclesAndBounds()
        {
            var spec = new BattleMapSpec("T", 10, 8,
                elevation: new[] { new ElevationTile(new GridCoord(2, 2), 1) },
                obstacles: new[] { new GridCoord(4, 4) });

            Assert.That(spec.Bounds.Width, Is.EqualTo(10));
            Assert.That(spec.Bounds.Height, Is.EqualTo(8));
            Assert.That(spec.BuildElevationMap().GetLevel(new GridCoord(2, 2)), Is.EqualTo(1));
            Assert.That(spec.BuildObstacleMap().IsBlocked(new GridCoord(4, 4)), Is.True);
        }

        [Test]
        public void DefaultsToEmptyTerrain()
        {
            var spec = new BattleMapSpec("Flat", 8, 8);
            Assert.That(spec.Elevation.Count, Is.EqualTo(0));
            Assert.That(spec.Obstacles.Count, Is.EqualTo(0));
        }

        [Test]
        public void MapLibrary_RuinedHall_AddsObstaclesAndSize_OverPlateau()
        {
            Assert.That(MapLibrary.Plateau().Obstacles.Count, Is.EqualTo(0));
            Assert.That(MapLibrary.RuinedHall().Obstacles.Count, Is.GreaterThan(0));
            Assert.That(MapLibrary.RuinedHall().Width, Is.GreaterThan(MapLibrary.Plateau().Width));
        }
    }

    public class ObstaclePathfindingTests
    {
        [Test]
        public void FindPath_RoutesAroundObstacle()
        {
            var bounds = new GridBounds(3, 3);
            var occ = new GridOccupancy();
            occ.Add(new GridCoord(1, 0));   // wall on column x=1 with a gap at (1,2)
            occ.Add(new GridCoord(1, 1));

            List<GridCoord> path = GridPathfinderBfs.FindPath(
                new GridCoord(0, 0), new GridCoord(2, 0), occ, bounds);

            Assert.That(path, Is.Not.Empty);
            Assert.That(path[0], Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(path[path.Count - 1], Is.EqualTo(new GridCoord(2, 0)));
            Assert.That(path.Contains(new GridCoord(1, 0)), Is.False);
            Assert.That(path.Contains(new GridCoord(1, 1)), Is.False);
        }

        [Test]
        public void CanPlace_RejectsObstacleTile()
        {
            var bounds = new GridBounds(4, 4);
            var occ = new GridOccupancy();
            occ.Add(new GridCoord(1, 1));

            Assert.That(occ.CanPlace(GridFootprint.Single, new GridCoord(1, 1), bounds), Is.False);
            Assert.That(occ.CanPlace(GridFootprint.Single, new GridCoord(0, 0), bounds), Is.True);
        }

        [Test]
        public void Footprint2x2_CannotCrossOneWideGap_CanCrossTwoWide()
        {
            var bounds = new GridBounds(6, 4);
            var big = new GridFootprint(2, 2);
            var start = new GridCoord(0, 1);
            var target = new GridCoord(5, 1);

            // Wall at x=3 with a single-tile gap at (3,1): a 2x2 unit can't fit through.
            var oneWide = new GridOccupancy();
            oneWide.Add(new GridCoord(3, 0));
            oneWide.Add(new GridCoord(3, 2));
            oneWide.Add(new GridCoord(3, 3));
            List<GridCoord> blocked = GridPathfinderBfs.FindFootprintApproach(start, big, target, oneWide, bounds);
            Assert.That(blocked, Is.Empty, "a 2x2 unit must not squeeze through a 1-wide gap");

            // Widen the gap to two tiles (3,1)+(3,2): now it fits.
            var twoWide = new GridOccupancy();
            twoWide.Add(new GridCoord(3, 0));
            twoWide.Add(new GridCoord(3, 3));
            List<GridCoord> open = GridPathfinderBfs.FindFootprintApproach(start, big, target, twoWide, bounds);
            Assert.That(open, Is.Not.Empty, "a 2x2 unit should pass through a 2-wide gap");
        }

        [Test]
        public void BattleState_SeedsObstaclesIntoOccupancy()
        {
            var battle = new BattleState();
            var obstacles = new ObstacleMap();
            obstacles.Add(new GridCoord(2, 2));
            battle.SetObstacles(obstacles);

            Assert.That(battle.Occupancy.IsOccupied(new GridCoord(2, 2)), Is.True);
            Assert.That(battle.BuildOccupancyExcluding(null).IsOccupied(new GridCoord(2, 2)), Is.True);
        }
    }

    public class EncounterMapTests
    {
        // Guards the handcrafted maps: every unit must spawn in bounds, off any
        // obstacle, and not on top of another unit — a cheap catch for an
        // authoring slip (a wall tile under a spawn, a unit pushed off-grid).
        [Test]
        public void EveryEncounter_SpawnsUnits_InBounds_OffObstacles_NonOverlapping()
        {
            foreach (EncounterSpec spec in EncounterLibrary.All())
            {
                GridBounds bounds = spec.Map.Bounds;
                ObstacleMap obstacles = spec.Map.BuildObstacleMap();
                var occupied = new HashSet<GridCoord>();

                foreach (CombatUnit unit in spec.BuildRoster())
                {
                    foreach (GridCoord cell in unit.Footprint.Cells(unit.Coord))
                    {
                        Assert.That(bounds.Contains(cell), Is.True,
                            $"{spec.Name}: unit {unit.Id.Value} cell ({cell.X},{cell.Y}) is out of bounds");
                        Assert.That(obstacles.IsBlocked(cell), Is.False,
                            $"{spec.Name}: unit {unit.Id.Value} spawns on an obstacle at ({cell.X},{cell.Y})");
                        Assert.That(occupied.Add(cell), Is.True,
                            $"{spec.Name}: units overlap at ({cell.X},{cell.Y})");
                    }
                }
            }
        }

        // Guards against an obstacle/elevation layout that walls a unit off:
        // every enemy must have *some* path (over any number of turns) to a
        // player. The 2x2 beast is the sharp case — it needs 2-wide lanes, so a
        // map that only leaves it a 1-wide gap would strand it here.
        [Test]
        public void EveryEncounter_EachEnemyCanReachAPlayer()
        {
            foreach (EncounterSpec spec in EncounterLibrary.All())
            {
                GridBounds bounds = spec.Map.Bounds;
                ElevationMap elevation = spec.Map.BuildElevationMap();

                var battle = new BattleState();
                battle.SetObstacles(spec.Map.BuildObstacleMap());
                battle.SetRoster(spec.BuildRoster());

                var players = new List<CombatUnit>();
                var enemies = new List<CombatUnit>();
                foreach (CombatUnit u in battle.Units)
                {
                    (u.Faction == Faction.Player ? players : enemies).Add(u);
                }

                foreach (CombatUnit enemy in enemies)
                {
                    GridOccupancy blocked = battle.BuildOccupancyExcluding(enemy);
                    bool canReach = false;

                    foreach (CombatUnit player in players)
                    {
                        bool reached = enemy.Footprint.IsSingle
                            ? GridPathfinderBfs.FindPath(enemy.Coord, player.Coord, blocked, bounds).Count > 0
                            : GridPathfinderBfs.FindFootprintApproach(enemy.Coord, enemy.Footprint, player.Coord, blocked, bounds, elevation).Count > 0;
                        if (reached)
                        {
                            canReach = true;
                            break;
                        }
                    }

                    Assert.That(canReach, Is.True,
                        $"{spec.Name}: enemy {enemy.Id.Value} at ({enemy.Coord.X},{enemy.Coord.Y}) is walled off from every player");
                }
            }
        }
    }
}
