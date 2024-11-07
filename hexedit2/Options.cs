using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haruka.Arcade.Hexedit2 {
    internal class Options {

        [Verb("single", HelpText = "Patch a single instance")]
        public class SingleOptions {

            [Option('v', Required = false, HelpText = "Verbose output")]
            public bool Verbose { get; set; }

            [Option('s', Required = false, HelpText = "Silent output")]
            public bool Silent { get; set; }

            [Option('t', Required = false, HelpText = "Type of the patch data (Binary, StringASCII, StringUTF8, StringShiftJIS)", Default = PatchType.Binary)]
            public PatchType Type { get; set; }

            [Value(1, Required = true, HelpText = "The input file")]
            public string InFile { get; set; }

            [Value(2, Required = true, HelpText = "The output file")]
            public string OutFile { get; set; }

            [Value(3, Required = true, HelpText = "The offset to patch (0x12345678)")]
            public string Offset { get; set; }

            [Value(4, Required = true, HelpText = "The data to patch (0x00,0x01,0x02...) Specify wildcards as \"0x??\".")]
            public string PatchString { get; set; }

            [Value(5, Required = false, HelpText = "The original data (0x00,0x01,0x02...) Specify wildcards as \"0x??\".")]
            public string OriginalString { get; set; }
        }

        [Verb("multi", HelpText = "Patch all instances")]
        public class MultiOptions {

            [Option('v', Required = false, HelpText = "Verbose output")]
            public bool Verbose { get; set; }

            [Option('s', Required = false, HelpText = "Silent output")]
            public bool Silent { get; set; }

            [Option('m', Required = false, HelpText = "Maximum hits", Default = int.MaxValue)]
            public int MaximumHits { get; set; }

            [Option('t', Required = false, HelpText = "Type of the patch data (Binary, StringASCII, StringUTF8, StringShiftJIS)", Default = PatchType.Binary)]
            public PatchType Type { get; set; }

            [Value(1, Required = true, HelpText = "The input file")]
            public string InFile { get; set; }

            [Value(2, Required = true, HelpText = "The output file")]
            public string OutFile { get; set; }

            [Value(3, Required = true, HelpText = "The original data (0x00,0x01,0x02...) Specify wildcards as \"0x??\".")]
            public string OriginalString { get; set; }

            [Value(4, Required = true, HelpText = "The data to patch (0x00,0x01,0x02...). Specify wildcards as \"0x??\".")]
            public string PatchString { get; set; }

        }

        [Verb("script", HelpText = "Patch multiple things at once")]
        public class ScriptOptions {

            [Option('v', Required = false, HelpText = "Verbose output")]
            public bool Verbose { get; set; }

            [Option('s', Required = false, HelpText = "Silent output")]
            public bool Silent { get; set; }

            [Option('c', Required = false, HelpText = "Continue even if a patch fails")]
            public bool ContinueOnError { get; set; }

            [Value(1, Required = true, HelpText = "The input file")]
            public string InFile { get; set; }

            [Value(2, Required = true, HelpText = "The output file")]
            public string OutFile { get; set; }

            [Value(3, Required = true, HelpText = "The script file")]
            public string ScriptFile { get; set; }

        }

        public enum PatchType {
            Binary, StringASCII, StringUTF8, StringShiftJIS
        }

        [Verb("find", HelpText = "Find an offset")]
        public class FindOptions {

            [Option('v', Required = false, HelpText = "Verbose output")]
            public bool Verbose { get; set; }

            [Option('s', Required = false, HelpText = "Silent output")]
            public bool Silent { get; set; }

            [Option('t', Required = false, HelpText = "Type of the patch data (Binary, StringASCII, StringUTF8, StringShiftJIS)", Default = PatchType.Binary)]
            public PatchType Type { get; set; }

            [Value(1, Required = true, HelpText = "The input file")]
            public string InFile { get; set; }

            [Value(2, Required = false, HelpText = "The data to find (0x00,0x01,0x02...) Specify wildcards as \"0x??\".")]
            public string OriginalString { get; set; }
        }

    }
}
