using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeakDetector
{
    class AllocationData
    {
        public AllocationData(ulong allocSize, ulong allocAddress)
        {
            AllocSize = allocSize;
            AllocAddress = allocAddress;
        }

        public ulong AllocSize { get; }
        public ulong AllocAddress { get; }
        public CountedStack Stack { get; set; }
    }
    class LiveSession : IDisposable
    {
        private TraceEventSession _session;
        private Dictionary<int, AllocationData> _lastAllocationDataByThread = new Dictionary<int, AllocationData>();


        public ConcurrentDictionary<ulong, AllocationData> Summary { get; } = new ConcurrentDictionary<ulong, AllocationData>();

        public AggregatedStacks Stacks { get; } = new AggregatedStacks();

        public void Start(int pid)
        {
            TraceEventSession.GetActiveSession("HeapSession")?.Dispose();

            _session = new TraceEventSession("HeapSession");

            _session.EnableWindowsHeapProvider(pid);
            _session.Source.Dynamic.All += Source_AllEvents;
            _session.Source.Kernel.StackWalkStack += Kernel_StackWalkStack;

            Task.Factory.StartNew(() => _session.Source.Process());
        }

        private void Kernel_StackWalkStack(StackWalkStackTraceData stack)
        {
            _lastAllocationDataByThread.TryGetValue(stack.ThreadID, out var lastAllocationData);

            ulong[] addresses = new ulong[stack.FrameCount];
            for (int i = 0; i < addresses.Length; ++i)
            {
                addresses[i] = stack.InstructionPointer(i);
            }

            if (lastAllocationData != null && lastAllocationData.Stack == null)
            {
                lastAllocationData.Stack = Stacks.AddStack(stack.ProcessID, addresses, lastAllocationData.AllocSize);
            }
        }

        private void StopProcessing()
        {
            _session.Source.StopProcessing();
        }

        private void Source_AllEvents(Microsoft.Diagnostics.Tracing.TraceEvent obj)
        {
            // Events we care about:
            //  Heap/Alloc
            //  Heap/Realloc
            //  Heap/Free
            //  StackWalk/Stack (it's associated with the previous event)

            switch (obj.EventName)
            {
                case "Heap/Alloc":
                    var lastAllocationData = AggregateAlloc((ulong)obj.PayloadByName("AllocSize"), (ulong)obj.PayloadByName("AllocAddress"));
                    _lastAllocationDataByThread[obj.ThreadID] = lastAllocationData;
                    break;
                case "Heap/ReAlloc":
                    AggregateRealloc((ulong)obj.PayloadByName("NewAllocSize"), (ulong)obj.PayloadByName("OldAllocSize"), (ulong)obj.PayloadByName("NewAllocAddress"), (ulong)obj.PayloadByName("OldAllocAddress"));
                    break;
                case "Heap/Free":
                    AggregateFree(obj.ProcessID, _lastAllocationDataByThread[obj.ThreadID], (ulong)obj.PayloadByName("FreeAddress"));
                    break;
            }
        }

        private void AggregateFree(int processId, AllocationData lastAllocationData, ulong freeAddress)
        {
            if (!Summary.TryRemove(freeAddress, out lastAllocationData))
            {
                throw new Exception();
            }
            Stacks.RemoveStack(processId, lastAllocationData.Stack.Stack.Addresses, lastAllocationData.AllocSize);
            lastAllocationData = null;
        }

        private void AggregateRealloc(ulong newAllocSize, ulong oldAllocSize, ulong newAllocAddress, ulong oldAllocAddress)
        {
            // TODO: For now not need to handle there are also Alloc and Free event

            //Console.WriteLine($"{nameof(AggregateRealloc)} {newAllocSize} {oldAllocSize} {newAllocAddress} {oldAllocAddress}");

            //if (!_summary.TryRemove(oldAllocAddress, out _))
            //{
            //    throw new Exception();
            //}
            //if (!_summary.TryAdd(newAllocAddress, new AllocationData(newAllocSize)))
            //{
            //    throw new Exception();
            //}
        }

        private AllocationData AggregateAlloc(ulong allocSize, ulong allocAddress)
        {
            //Console.WriteLine($"{nameof(AggregateAlloc)} {allocSize} {allocAddress}");
            var lastAllocationData = new AllocationData(allocSize, allocAddress);
            if (!Summary.TryAdd(allocAddress, lastAllocationData))
            {
                throw new Exception();
            }
            return lastAllocationData;
        }

        public void Dispose()
        {
            StopProcessing();
            _session.Dispose();
        }
    }
}
