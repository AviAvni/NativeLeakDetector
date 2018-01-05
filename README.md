# Win32 Leak Detector

This is a Win32 memory leak detector that instruments the Windows heap allocation APIs and collects real-time allocation information. It aggregates allocation stacks in real-time, and can display any memory allocated by an application that was not yet freed, helping identify and resolve memory leaks. It works only in live mode, and collects the `HeapAlloc` and `HeapFree` ETW events for a specified process. Unlike some other tools, this tool does not record every allocation and free event to a file on disk and analyzes them later -- for production processes with a heavy allocation load, this makes the difference between a working tool and a gigantic disk hog.

Importantly, if the target process exits before the tool had a chance to print stacks, symbol resolution will fail, so it is more suitable for longer-running processes.

> NOTE: This project is not done. There are still some unimplemented features, and the code hasn't been extensively tested. Caveat emptor, and pull requests welcome!

## Running

Open a command prompt window as administrator, and try the example [Demo](Demo) program.

Collect allocation events and print the top leaked stacks when Ctrl+C is hit:

```
LeakDetector -p 7408
```

Print the top stacks, sorted by top leak

```
  LeakDetector -T -p 7408
```

How often to print the stack summary in seconds

```
  LeakDetector -i 5 -p 7408
```

How many times to print a summary before quitting

```
  LeakDetector -c 5 -p 7408
```

Clear the screen between printouts

```
  LeakDetector -C -i 5 -p 7408
```

## Example Output

```
18:26:32
AllocateCount: 30
FreeCount: 0
AlocateSize: 5040
FreeSize: 0
ntdll.dll!NtTraceEvent+0xC
ntdll.dll!RtlpLogHeapAllocateEvent+0x5F
ntdll.dll!RtlpAllocateHeapInternal+0x411
ntdll.dll!RtlAllocateHeap+0x3E
ucrtbased.dll!heap_alloc_dbg_internal+0x198
ucrtbased.dll!heap_alloc_dbg+0x36
ucrtbased.dll!_malloc_dbg+0x1A
ucrtbased.dll!malloc+0x14
Demo.exe!+0x11F0D
Demo.exe!+0x11AB2
Demo.exe!+0x119D0
KERNEL32.DLL!BaseThreadInitThunk+0x24
ntdll.dll!__RtlUserThreadStart+0x2F
ntdll.dll!_RtlUserThreadStart+0x1B
------------
AllocateCount: 20
FreeCount: 0
AlocateSize: 4000
FreeSize: 0
ntdll.dll!NtTraceEvent+0xC
ntdll.dll!RtlpLogHeapAllocateEvent+0x5F
ntdll.dll!RtlpAllocateHeapInternal+0x411
ntdll.dll!RtlAllocateHeap+0x3E
ntdll.dll!RtlpReAllocateHeap+0x1C2
ntdll.dll!RtlpReAllocateHeapInternal+0x660
ntdll.dll!RtlReAllocateHeap+0x43
Demo.exe!+0x11C1D
Demo.exe!+0x119D0
KERNEL32.DLL!BaseThreadInitThunk+0x24
ntdll.dll!__RtlUserThreadStart+0x2F
ntdll.dll!_RtlUserThreadStart+0x1B
------------
AllocateCount: 30
FreeCount: 24
AlocateSize: 3000
FreeSize: 2400
ntdll.dll!NtTraceEvent+0xC
ntdll.dll!RtlpLogHeapAllocateEvent+0x5F
ntdll.dll!RtlpAllocateHeapInternal+0x411
ntdll.dll!RtlAllocateHeap+0x3E
Demo.exe!+0x11BF0
Demo.exe!+0x119D0
KERNEL32.DLL!BaseThreadInitThunk+0x24
ntdll.dll!__RtlUserThreadStart+0x2F
ntdll.dll!_RtlUserThreadStart+0x1B
------------
AllocateCount: 4
FreeCount: 0
AlocateSize: 800
FreeSize: 0
ntdll.dll!NtTraceEvent+0xC
ntdll.dll!RtlpLogHeapAllocateEvent+0x5F
ntdll.dll!RtlpAllocateHeapInternal+0x411
ntdll.dll!RtlAllocateHeap+0x3E
ntdll.dll!RtlpReAllocateHeap+0xA7C
ntdll.dll!RtlpReAllocateHeapInternal+0x660
ntdll.dll!RtlReAllocateHeap+0x43
Demo.exe!+0x11C1D
Demo.exe!+0x119D0
KERNEL32.DLL!BaseThreadInitThunk+0x24
ntdll.dll!__RtlUserThreadStart+0x2F
ntdll.dll!_RtlUserThreadStart+0x1B
```

## Requirements/Limitations

Kernel symbols are currently not resolved, and filtered out by default.

## Overhead

This tool does not inject anything into the target process, and relies only on ETW events. Furthermore, it does not use disk buffers, and processes events in real-time. Still, very high allocation rates combined with an otherwise loaded system can introduce additional overhead due to the event processing and aggregation. Further benchmarking is needed to establish more accurate estimates.

## Building

To build the tool, you will need Visual Studio 2015/2017, and the Windows SDK installed (for the symsrv.dll and dbghelp.dll files).
