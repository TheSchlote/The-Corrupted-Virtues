using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins MovementRules.ComputeReachableSteps — the reach-capping shared by
    // the player's path preview and the enemy AI.
    public class MovementRulesTests
    {
        // A straight path of `steps` edges along +X starting at (0,0).
        private static List<GridCoord> Line(int steps)
        {
            List<GridCoord> path = new List<GridCoord>();
            for (int x = 0; x <= steps; x++)
            {
                path.Add(new GridCoord(x, 0));
            }
            return path;
        }

        [Test]
        public void TrivialPath_IsZero()
        {
            Assert.That(
                MovementRules.ComputeReachableSteps(
                    new List<GridCoord> { new GridCoord(0, 0) }, 4, new GridOccupancy(), new GridCoord(0, 0)),
                Is.EqualTo(0));
            Assert.That(
                MovementRules.ComputeReachableSteps(null, 4, new GridOccupancy(), new GridCoord(0, 0)),
                Is.EqualTo(0));
        }

        [Test]
        public void WithinRange_AllStepsReachable()
        {
            Assert.That(
                MovementRules.ComputeReachableSteps(Line(4), 4, new GridOccupancy(), new GridCoord(0, 0)),
                Is.EqualTo(4));
        }

        [Test]
        public void BeyondRange_CappedAtMoveRange()
        {
            Assert.That(
                MovementRules.ComputeReachableSteps(Line(4), 2, new GridOccupancy(), new GridCoord(0, 0)),
                Is.EqualTo(2));
        }

        [Test]
        public void OccupiedDestination_StopsOneShort()
        {
            GridOccupancy occ = new GridOccupancy();
            occ.Add(new GridCoord(3, 0)); // someone standing on the destination

            // min(min(3 steps, range 4), 3 - 1) = 2
            Assert.That(
                MovementRules.ComputeReachableSteps(Line(3), 4, occ, new GridCoord(0, 0)),
                Is.EqualTo(2));
        }
    }
}
