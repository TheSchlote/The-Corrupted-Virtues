using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // Pins the Speed-based round computation: highest Speed first, ties
    // broken by lower Id (deterministic), dead units excluded.
    public class TurnOrderTests
    {
        [Test]
        public void Empty_ReturnsEmpty()
        {
            var result = TurnOrder.ComputeRound(new List<TurnOrderEntry>());
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SingleAlive_ReturnsThatUnit()
        {
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 7, speed: 10, isAlive: true)
            };
            Assert.That(TurnOrder.ComputeRound(entries), Is.EqualTo(new[] { 7 }));
        }

        [Test]
        public void DifferentSpeeds_HighestSpeedFirst()
        {
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 1, speed: 5, isAlive: true),
                new TurnOrderEntry(id: 2, speed: 12, isAlive: true),
                new TurnOrderEntry(id: 3, speed: 8, isAlive: true)
            };
            Assert.That(TurnOrder.ComputeRound(entries), Is.EqualTo(new[] { 2, 3, 1 }));
        }

        [Test]
        public void EqualSpeeds_TieBreakByLowerId()
        {
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 3, speed: 10, isAlive: true),
                new TurnOrderEntry(id: 1, speed: 10, isAlive: true),
                new TurnOrderEntry(id: 2, speed: 10, isAlive: true)
            };
            Assert.That(TurnOrder.ComputeRound(entries), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void DeadUnits_Excluded()
        {
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 1, speed: 10, isAlive: false),
                new TurnOrderEntry(id: 2, speed: 8, isAlive: true),
                new TurnOrderEntry(id: 3, speed: 12, isAlive: false),
                new TurnOrderEntry(id: 4, speed: 5, isAlive: true)
            };
            Assert.That(TurnOrder.ComputeRound(entries), Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void AllDead_ReturnsEmpty()
        {
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 1, speed: 10, isAlive: false),
                new TurnOrderEntry(id: 2, speed: 8, isAlive: false)
            };
            Assert.That(TurnOrder.ComputeRound(entries), Is.Empty);
        }

        [Test]
        public void MixedSpeedsAndTies_ProducesDeterministicOrder()
        {
            // Same input, repeated calls should yield the same order every
            // time — guards against List.Sort being unstable on ties.
            var entries = new List<TurnOrderEntry>
            {
                new TurnOrderEntry(id: 5, speed: 10, isAlive: true),
                new TurnOrderEntry(id: 2, speed: 10, isAlive: true),
                new TurnOrderEntry(id: 4, speed: 15, isAlive: true),
                new TurnOrderEntry(id: 1, speed: 10, isAlive: true),
                new TurnOrderEntry(id: 3, speed: 5, isAlive: true)
            };

            var first = TurnOrder.ComputeRound(entries);
            var second = TurnOrder.ComputeRound(entries);

            Assert.That(first, Is.EqualTo(new[] { 4, 1, 2, 5, 3 }));
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
