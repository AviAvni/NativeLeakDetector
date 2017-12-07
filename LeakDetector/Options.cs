using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeakDetector
{
    class Options
    {
        [Option('T', "top", Required = false, DefaultValue = 10,
            HelpText = "Print the top stacks, sorted by top leak")]
        public int TopStacks { get; set; }

        [Option('m', "minsamples", Required = false, DefaultValue = 0,
            HelpText = "The minimum number of samples a stack must have to be included in the output")]
        public ulong MinimumSamples { get; set; }

        [Option('i', "interval", Required = false, DefaultValue = 0,
            HelpText = "How often to print the stack summary (in seconds; by default, wait for Ctrl+C)")]
        public double IntervalSeconds { get; set; }

        [Option('c', "count", Required = false, DefaultValue = 0,
            HelpText = "How many times to print a summary before quitting (0 = indefinite)")]
        public int Count { get; set; }

        [Option('C', "clear", Required = false, DefaultValue = false,
            HelpText = "Clear the screen between printouts (useful for top-like display)")]
        public bool ClearScreen { get; set; }

        [Option('P', "pname", Required = false, MutuallyExclusiveSet = "pname",
            HelpText = "Display stacks only from this process (by name)")]
        public string ProcessName { get; set; }

        [Option('p', "pid", Required = false, MutuallyExclusiveSet = "pid",
            HelpText = "Display stacks only from this process (by id)")]
        public int ProcessID { get; set; }

        public IEnumerable<int> PidsToFilter
        {
            get
    
        {
                if (ProcessID != 0)
                    return new int[] { ProcessID };

                if (!String.IsNullOrEmpty(ProcessName))
                    return Process.GetProcessesByName(ProcessName).Select(p => p.Id);

                return Enumerable.Empty<int>();
            }
        }

        [HelpOption]
        public string GetUsage()
        {
            var helpText = HelpText.AutoBuild(new Options());
            helpText.Copyright = "Copyright Sasha Goldshtein & Avi Avni, 2017 under the MIT License.";
            helpText.Heading = "LiveStacks - print and aggregate live stacks from native memory de/allocations.";
            helpText.AddPostOptionsLine("Examples:");
            helpText.AddPostOptionsLine("  LeakDetector -p 7408");
            helpText.AddPostOptionsLine("  LeakDetector -c 1");
            return helpText.ToString();
        }
    }
}