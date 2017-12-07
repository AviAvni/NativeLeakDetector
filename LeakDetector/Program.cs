using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakDetector
{
    class Program
    {
        private const double DefaultIntervalSeconds = 5.0;

        private static Options _options;
        private static LiveSession _session;
        private static NativeTarget _nativeTarget;
        private static Timer _timer;
        private static int _invocationsLeft;
        private static TimeSpan _interval = TimeSpan.Zero;
        private static DateTime _lastTimerInvocation = DateTime.Now;

        static void Main()
        {
            ParseCommandLineArguments();
            SetIntervalAndRepetitions();

            _nativeTarget = new NativeTarget(_options.PidsToFilter.First());
            _session = new LiveSession();

            Console.CancelKeyPress += (sender, args) =>
            {
                Console.Error.WriteLine("Ctrl+C pressed, stopping...");
                args.Cancel = true;
                _session?.Dispose();
                _nativeTarget?.Dispose();
                _timer?.Dispose();
            };

            SetupTimer();
            _session.Start(_options.PidsToFilter.First());

            if (_interval == TimeSpan.Zero)
            {
                OnTimer();
            }

            Console.ReadLine();
        }

        private static void PrintAllocatedStack()
        {
            if( _options.TopStacks > 0 )
            {
                foreach (var item in _session.Stacks.TopStacks(_options.TopStacks, _options.MinimumSamples))
                {
                    PrintStack(item);
                }
            }
            else
            {
                foreach (var process in _session.Stacks.AllStacksByProcess())
                {
                    foreach (var item in process.Value)
                    {
                        Console.WriteLine("PID" + process.Key);
                        PrintStack(item);
                    }
                }
            }            
        }

        private static void PrintStack(AggregatedStack item)
        {
            Console.WriteLine("AllocateCount: " + item.Count.AllocateCount);
            Console.WriteLine("FreeCount: " + item.Count.FreeCount);
            Console.WriteLine("AlocateSize: " + item.Count.AlocateSize);
            Console.WriteLine("FreeSize: " + item.Count.FreeSize);
            foreach (var address in item.Addresses)
            {
                Console.WriteLine(_nativeTarget.ResolveSymbol(address));
            }
            Console.WriteLine("------------");
        }

        private static void SetIntervalAndRepetitions()
        {
            if (_options.Count == 0)
            {
                // The user wants an indefinite number of repetitions if an interval is specified,
                // or run without printing until Ctrl+C if no interval is specified.
                _invocationsLeft = int.MaxValue;
                if (_options.IntervalSeconds != 0.0)
                    _interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
            }
            else
            {
                // A number of printouts was specified, set a default interval if one was not provided.
                _invocationsLeft = _options.Count;
                _interval = TimeSpan.FromSeconds(_options.IntervalSeconds == 0.0 ? DefaultIntervalSeconds : _options.IntervalSeconds);
            }
        }

        private static void ParseCommandLineArguments()
        {
            var parser = new Parser(ps =>
            {
                ps.CaseSensitive = true;
                ps.IgnoreUnknownArguments = false;
                ps.HelpWriter = Console.Out;
            });
            _options = new Options();
            if (!parser.ParseArguments(Environment.GetCommandLineArgs(), _options))
                Environment.Exit(1);
        }

        private static void SetupTimer()
        {
            // If there is no interval, we don't need to print at timed intervals. Just wait for the
            // user to hit Ctrl+C and exit the session.
            if (_interval == TimeSpan.Zero)
                return;

            object timerSyncObject = new object();
            _timer = new Timer(_ =>
            {
                // Prevent multiple invocations of the timer from running concurrently,
                // and if not enough time has elapsed, don't run the timer procedure again.
                // This may happen if there are a lot of symbols to resolve, and the timer
                // can't keep up (at least at first).
                lock (timerSyncObject)
                {
                    if (DateTime.Now - _lastTimerInvocation < _interval)
                        return;

                    _lastTimerInvocation = DateTime.Now;
                    OnTimer();
                    if (--_invocationsLeft == 0)
                    {
                        _session.Dispose();
                        _nativeTarget.Dispose();
                        _timer.Dispose();
                        _session = null;
                        _nativeTarget = null;
                        _timer = null;
                    }
                }
            }, null, _interval, _interval);
        }

        private static void OnTimer()
        {
            if (_options.ClearScreen)
            {
                Console.Clear();
            }
            Console.Error.WriteLine(DateTime.Now.ToLongTimeString());
            Stopwatch sw = Stopwatch.StartNew();
            PrintAllocatedStack();
            Console.Error.WriteLine($"  Time aggregating/resolving: {sw.ElapsedMilliseconds}ms");
        }
    }
}