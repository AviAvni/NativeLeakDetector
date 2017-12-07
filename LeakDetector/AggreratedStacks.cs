using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeakDetector
{
    struct AggregatedStack
    {
        public int ProcessID { get; set; }
        public ulong[] Addresses { get; set; }
        public CountedStack Count { get; set; }
    }

    struct OneStack : IEquatable<OneStack>
    {
        public ulong[] Addresses { get; set; }

        public override int GetHashCode()
        {
            // TODO Can this be sped up? E.g. using SIMD?

            int hc = Addresses.Length;
            for (int i = 0; i < Addresses.Length; ++i)
            {
                hc = unchecked((int)((ulong)hc * 37 + Addresses[i]));
            }
            return hc;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is OneStack))
                return false;

            OneStack other = (OneStack)obj;
            return Equals(other);
        }

        public bool Equals(OneStack other)
        {
            // TODO Can this be sped up? E.g. using SIMD?

            if (Addresses.Length != other.Addresses.Length)
                return false;

            for (int i = 0; i < Addresses.Length; ++i)
                if (Addresses[i] != other.Addresses[i])
                    return false;

            return true;
        }
    }

    class CountedStack
    {
        public OneStack Stack { get; set; }
        public ulong AllocateCount { get; set; }
        public ulong FreeCount { get; set; }
        public ulong AlocateSize { get; set; }
        public ulong FreeSize { get; set; }
    }
    class PidStacks
    {
        public int ProcessID { get; private set; }
        public ConcurrentDictionary<OneStack, CountedStack> CountedStacks { get; } = new ConcurrentDictionary<OneStack, CountedStack>();

        public PidStacks(int processID)
        {
            ProcessID = processID;
        }

        public void AddStack(ulong[] addresses, ulong allocateSize)
        {
            var stack = new OneStack { Addresses = addresses };
            CountedStacks.AddOrUpdate(stack, new CountedStack() { Stack = stack, AllocateCount = 1, FreeCount = 0, AlocateSize = allocateSize, FreeSize = 0 },
                (_, existingCount) =>
                {
                    existingCount.AllocateCount++;
                    existingCount.AlocateSize += allocateSize;
                    return existingCount;
                });
        }

        public void RemoveStack(ulong[] addresses, ulong freedSize)
        {
            var stack = new OneStack { Addresses = addresses };
            CountedStacks.AddOrUpdate(stack, new CountedStack() { Stack = stack, AllocateCount = 0, FreeCount = 1, AlocateSize = 0, FreeSize = 0 },
                (_, existingCount) =>
                {
                    existingCount.FreeCount++;
                    existingCount.FreeSize += freedSize;
                    return existingCount;
                });
        }
    }

    /// <summary>
    /// Contains a summary of stack occurrences, aggregated per process and counted. This class
    /// is thread-safe.
    /// </summary>
    class AggregatedStacks
    {
        private ConcurrentDictionary<int, PidStacks> _pidStacks = new ConcurrentDictionary<int, PidStacks>();

        public CountedStack AddStack(int processID, ulong[] addresses, ulong allocatedSize)
        {
            return _pidStacks.AddOrUpdate(processID,
                 pid =>
                 {
                     PidStacks stacks = new PidStacks(pid);
                     stacks.AddStack(addresses, allocatedSize);
                     return stacks;
                 },
                 (_, existing) =>
                 {
                     existing.AddStack(addresses, allocatedSize);
                     return existing;
                 }).CountedStacks[new OneStack() { Addresses = addresses }];
        }

        public void RemoveStack(int processID, ulong[] addresses, ulong freedSize)
        {
            _pidStacks.AddOrUpdate(processID,
                 pid =>
                 {
                     throw new Exception();
                 },
                 (_, existing) =>
                 {
                     existing.RemoveStack(addresses, freedSize);
                     return existing;
                 });
        }

        /// <summary>
        /// Get the top stacks from the recorded trace.
        /// </summary>
        /// <param name="top">The number of stacks to return.</param>
        /// <param name="minSamples">The minimum number of samples a returned stack must have.</param>
        /// <returns>A dictionary that contains the top <see cref="top"/> stacks
        /// by number of occurrences.</returns>
        public List<AggregatedStack> TopStacks(int top, ulong minSamples)
        {
            return (from kvp in _pidStacks
                    from stacks in kvp.Value.CountedStacks
                    let leakCount = stacks.Value.AllocateCount - stacks.Value.FreeCount
                    where leakCount >= minSamples
                    orderby leakCount descending
                    select new AggregatedStack { ProcessID = kvp.Key, Count = stacks.Value, Addresses = stacks.Key.Addresses }
                 ).Take(top).ToList();
        }

        public IDictionary<int, List<AggregatedStack>> AllStacksByProcess()
        {
            var result = new Dictionary<int, List<AggregatedStack>>();
            foreach (var kvp in _pidStacks)
            {
                List<AggregatedStack> stacks = new List<AggregatedStack>();
                foreach (var countedStack in kvp.Value.CountedStacks)
                {
                    stacks.Add(new AggregatedStack { Count = countedStack.Value, Addresses = countedStack.Key.Addresses });
                }
                result.Add(kvp.Key, stacks);
            }
            return result;
        }

        public void Clear()
        {
            _pidStacks.Clear();
        }
    }
}