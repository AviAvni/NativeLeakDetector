# NativeLeakDetector

This tool records, aggregates, and displays call stacks from specific ETW allocation events. It works only in live mode, when the collection and analysis happens on target process. Importantly, if the target process exits before the tool had a chance to print stacks, symbol resolution will fail, so it is more suitable for longer-running processes.

> NOTE: This project is not done. There are still some unimplemented features, and the code hasn't been extensively tested. Caveat emptor, and pull requests welcome!

## Running

Open a command prompt window as administrator, and try some of the following examples.

Collect allocation events print the top leaked stacks when Ctrl+C is hit:

```
LeakDetector -p 7408
```

## Example Output

Native process, heavy CPU consumption:

```
18:26:32
AllocateCount: 30
FreeCount: 0
AlocateSize: 5040
FreeSize: 0
        72016463
        720F1923
        7202AC12
        7201BCF0
    7FFF234B9314
    7FFF234B920B
    7FFF234B91BE
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
        72016463
        720F1923
        7202AC12
        7201BCF0
    7FFF234B9314
    7FFF234B920B
    7FFF234B91BE
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
        72016463
        720F1923
        7202AC12
        7201BCF0
    7FFF234B9314
    7FFF234B920B
    7FFF234B91BE
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
        72016463
        720F1923
        7202AC12
        7201BCF0
    7FFF234B9314
    7FFF234B920B
    7FFF234B91BE
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
TBD

## Building

To build the tool, you will need Visual Studio 2015/2017, and the Windows SDK installed (for the symsrv.dll and dbghelp.dll files).
