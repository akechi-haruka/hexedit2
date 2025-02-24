using CommandLine;
using IniParser.Model;
using IniParser;
using System.Text;
using static Haruka.Arcade.Hexedit2.Options;
using static Haruka.Arcade.Hexedit2.Patch;

namespace Haruka.Arcade.Hexedit2 {
    internal class Program {
        static int Main(string[] args) {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return Parser.Default.ParseArguments<SingleOptions, MultiOptions, ScriptOptions, FindOptions>(args).MapResult<SingleOptions, MultiOptions, ScriptOptions, FindOptions, int>(RunSingle, RunMulti, RunScript, RunFind, errs => 1);
        }

        internal static bool IsVerbose;
        internal static bool IsSilent;

        public static void Log(string str) {
            if (!IsSilent) {
                Console.WriteLine(str);
            }
        }

        public static void LogVerbose(string str) {
            if (IsVerbose) {
                Console.WriteLine(str);
            }
        }

        public static int RunSingle(SingleOptions opts) {
            IsVerbose = opts.Verbose;
            IsSilent = opts.Silent;

            if (!File.Exists(opts.InFile)) {
                Log("Input file not found: " + opts.InFile);
                return 2;
            }

            byte[] file = File.ReadAllBytes(opts.InFile);

            long offset;
            try {
                offset = Convert.ToInt64(opts.Offset, 16);
            } catch {
                Log("Failed to parse offset");
                return 3;
            }

            List<long> unknownsPatch;
            List<long> unknownsOriginal = null;

            byte[] patch;
            try {
                patch = Patch.Parse(opts.PatchString, opts.Type, out unknownsPatch);
            } catch {
                Log("Failed to parse patch string");
                return 4;
            }

            byte[] original = null;
            if (!String.IsNullOrEmpty(opts.OriginalString)) {
                try {
                    original = Patch.Parse(opts.PatchString, opts.Type, out unknownsOriginal);
                } catch {
                    Log("Failed to parse original string");
                    return 5;
                }
            }

            LogVerbose("To (" + patch.Length + "): " + string.Join(" ", patch));
            LogVerbose("From (" + original?.Length + "): " + string.Join(" ", original ?? []));
            LogVerbose("Offset: " + offset.ToString("X2"));

            PatchResult pr = Patch.ApplyPatch(ref file, offset, patch, original, unknownsPatch, unknownsOriginal, opts.Type != PatchType.Binary);
            if (pr != PatchResult.OK && pr != PatchResult.OK_ALREADY) {
                Log("Patch failed: " + pr);
                return 6;
            }

            LogVerbose("Result: " + pr);

            Log("Saving to: " + opts.OutFile);
            File.WriteAllBytes(opts.OutFile, file);

            return 0;
        }

        public static int RunMulti(MultiOptions opts) {
            IsVerbose = opts.Verbose;
            IsSilent = opts.Silent;

            if (!File.Exists(opts.InFile)) {
                Log("Input file not found: " + opts.InFile);
                return 2;
            }

            byte[] file = File.ReadAllBytes(opts.InFile);

            List<long> unknownsPatch;
            List<long> unknownsOriginal;

            byte[] patch;
            try {
                patch = Patch.Parse(opts.PatchString, opts.Type, out unknownsPatch);
            } catch {
                Log("Failed to parse patch string");
                return 3;
            }

            byte[] original;
            try {
                original = Patch.Parse(opts.OriginalString, opts.Type, out unknownsOriginal);
            } catch {
                Log("Failed to parse original string");
                return 4;
            }

            List<long> offsets = Patch.SearchOffsets(file, original, unknownsOriginal, opts.MaximumHits);
            if (offsets.Count == 0) {
                if (Patch.SearchOffsets(file, patch, unknownsPatch, 1).Count > 0) {
                    Log("No matches, but patch was already found.");
                    return 0;
                } else {
                    Log("No matches found");
                    return 5;
                }
            }

            foreach (long offset in offsets) {
                Log("Patching offset 0x" + offset.ToString("X2"));
                PatchResult pr = Patch.ApplyPatch(ref file, offset, patch, original, unknownsPatch, unknownsOriginal, opts.Type != PatchType.Binary);
                if (pr != PatchResult.OK && pr != PatchResult.OK_ALREADY) {
                    Log("Patch failed: " + pr);
                    return 6;
                }

                LogVerbose("Patch Result: " + pr);
            }

            Log("Saving to: " + opts.OutFile);
            File.WriteAllBytes(opts.OutFile, file);

            return 0;
        }

        public static int RunScript(ScriptOptions opts) {
            IsVerbose = opts.Verbose;
            IsSilent = opts.Silent;

            if (!File.Exists(opts.InFile)) {
                Log("Input file not found: " + opts.InFile);
                return 2;
            }

            byte[] file = File.ReadAllBytes(opts.InFile);

            if (!File.Exists(opts.ScriptFile)) {
                Log("Script file not found: " + opts.ScriptFile);
                return 3;
            }

            FileIniDataParser parser = new FileIniDataParser();
            IniData script = parser.ReadFile(opts.ScriptFile, Encoding.UTF8);

            foreach (SectionData sd in script.Sections) {
                String sec = sd.SectionName;
                Log("Processing: " + sec);
                try {
                    PatchType type = Enum.Parse<PatchType>(script[sec]["Type"]);
                    String enabled = script[sec]["Enabled"];
                    String mode = script[sec]["Mode"];
                    String patchString = script[sec]["Patch"];
                    String originalString = script[sec]["Original"];

                    if (!Boolean.TryParse(script[sec]["StringTrim"], out bool stringTrim)) {
                        stringTrim = true;
                    }

                    if (stringTrim) {
                        patchString = patchString?.Trim();
                        originalString = originalString?.Trim();
                    }

                    if (!String.IsNullOrWhiteSpace(enabled) && (enabled != "1" && enabled.ToUpper() != "TRUE")) {
                        Log(sec + " is disabled, continuing...");
                        continue;
                    }

                    LogVerbose("Mode: " + mode);
                    LogVerbose("Type: " + type);
                    LogVerbose("Original: " + originalString);
                    LogVerbose("Patch:" + patchString);

                    List<long> unknownsPatch = null;
                    List<long> unknownsOriginal = null;
                    byte[] patch = null;
                    byte[] original = null;

                    if (mode != "MultiTable") {
                        if (patchString == null) {
                            throw new Exception("Patch is unset");
                        }

                        try {
                            patch = Patch.Parse(patchString, type, out unknownsPatch);
                        } catch {
                            Log("Failed to parse patch string");
                            throw;
                        }

                        if (!String.IsNullOrEmpty(originalString)) {
                            try {
                                original = Patch.Parse(originalString, type, out unknownsOriginal);
                            } catch {
                                Log("Failed to parse original string");
                                throw;
                            }
                        }
                    }

                    if (mode == "Single") {
                        long offset = 0;
                        try {
                            offset = Convert.ToInt64(script[sec]["Offset"], 16);
                        } catch {
                            Log("Failed to parse offset");
                            throw;
                        }

                        LogVerbose("To (" + patch.Length + "): " + string.Join(" ", patch));
                        LogVerbose("From (" + original?.Length + "): " + string.Join(" ", original ?? []));
                        LogVerbose("Offset: " + offset.ToString("X2"));

                        PatchResult pr = Patch.ApplyPatch(ref file, offset, patch, original, unknownsPatch, unknownsOriginal, type != PatchType.Binary);
                        if (pr != PatchResult.OK && pr != PatchResult.OK_ALREADY) {
                            throw new Exception("Patch failed: " + pr);
                        }

                        LogVerbose("Result: " + pr);
                    } else if (mode == "Multi") {

                        int hits = 0;
                        Int32.TryParse(script[sec]["MaximumHits"], out hits);
                        if (hits <= 0) {
                            hits = Int32.MaxValue;
                        }

                        List<long> offsets = Patch.SearchOffsets(file, original, unknownsOriginal, hits);
                        if (offsets.Count == 0) {
                            if (Patch.SearchOffsets(file, patch, unknownsPatch, 1).Count > 0) {
                                Log("No matches, but patch was already found.");
                                continue;
                            } else {
                                throw new Exception("No matches found");
                            }
                        }

                        foreach (long offset in offsets) {
                            Log("Patching offset 0x" + offset.ToString("X2"));
                            PatchResult pr = Patch.ApplyPatch(ref file, offset, patch, original, unknownsPatch, unknownsOriginal, type != PatchType.Binary);
                            if (pr != PatchResult.OK && pr != PatchResult.OK_ALREADY) {
                                throw new Exception("Patch failed: " + pr);
                            }

                            LogVerbose("Patch Result: " + pr);
                        }

                    } else if (mode == "MultiTable") {

                        int hits = 0;
                        Int32.TryParse(script[sec]["MaximumHits"], out hits);
                        if (hits <= 0) {
                            hits = Int32.MaxValue;
                        }

                        foreach (KeyData kd in sd.Keys) {
                            if (kd.KeyName != "Type" && kd.KeyName != "Mode" && kd.KeyName != "MaximumHits" && kd.KeyName != "Enabled" && kd.KeyName != "StringTrim") {

                                originalString = kd.KeyName;
                                patchString = kd.Value;
                                
                                if (stringTrim) {
                                    patchString = patchString?.Trim();
                                    originalString = originalString?.Trim();
                                }

                                try {
                                    patch = Patch.Parse(patchString, type, out unknownsPatch);
                                } catch {
                                    Log("Failed to parse patch string");
                                    throw;
                                }

                                try {
                                    original = Patch.Parse(originalString, type, out unknownsOriginal);
                                } catch {
                                    Log("Failed to parse original string");
                                    throw;
                                }

                                if (type != PatchType.Binary) {
                                    Log("Searching for string: " + originalString);
                                }

                                List<long> offsets = Patch.SearchOffsets(file, original, unknownsOriginal, hits);
                                if (offsets.Count == 0) {
                                    if (Patch.SearchOffsets(file, patch, unknownsPatch, 1).Count > 0) {
                                        Log("No matches, but patch was already found.");
                                        continue;
                                    } else {
                                        throw new Exception("No matches found");
                                    }
                                }

                                foreach (long offset in offsets) {
                                    Log("Patching offset 0x" + offset.ToString("X2"));
                                    PatchResult pr = Patch.ApplyPatch(ref file, offset, patch, original, unknownsPatch, unknownsOriginal, type != PatchType.Binary);
                                    if (pr != PatchResult.OK && pr != PatchResult.OK_ALREADY) {
                                        throw new Exception("Patch failed: " + pr);
                                    }

                                    LogVerbose("Patch Result: " + pr);
                                }

                            }
                        }

                    } else {
                        throw new Exception("Unknown mode");
                    }
                } catch (Exception ex) {
                    Log("Error processing patch: " + sec + ": " + ex.Message);
                    if (opts.ContinueOnError) {
                        Log("-c is set, continuing...");
                    } else {
                        throw;
                    }
                }

                if (opts.PauseAfterEach) {
                    Log("Press enter to continue...");
                    Console.ReadLine();
                }
            }

            Log("Saving to: " + opts.OutFile);
            File.WriteAllBytes(opts.OutFile, file);

            return 0;
        }

        public static int RunFind(FindOptions opts) {
            IsVerbose = opts.Verbose;
            IsSilent = opts.Silent;

            if (!File.Exists(opts.InFile)) {
                Log("Input file not found: " + opts.InFile);
                return 2;
            }

            byte[] file = File.ReadAllBytes(opts.InFile);

            List<long> unknownsOriginal;

            byte[] original;
            try {
                original = Patch.Parse(opts.OriginalString, opts.Type, out unknownsOriginal);
            } catch {
                Log("Failed to parse original string");
                return 3;
            }

            List<long> offsets = Patch.SearchOffsets(file, original, unknownsOriginal, Int32.MaxValue);
            if (offsets.Count == 0) {
                Log("No matches found");
                return 4;
            }

            foreach (long offset in offsets) {
                Console.WriteLine("0x" + offset.ToString("X2"));
            }

            return 0;
        }
    }
}
