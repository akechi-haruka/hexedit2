
using System.Text;
using static Haruka.Arcade.Hexedit2.Options;

namespace Haruka.Arcade.Hexedit2 {
    internal class Patch {

        public enum PatchResult {
            OK, OK_ALREADY, LENGTH_MISMATCH, BYTE_MISMATCH, STRING_TOO_LONG
        }

        internal static byte[] Parse(string patchString, PatchType type, out List<long> unknownBytes) {

            unknownBytes = new List<long>();

            if (type == PatchType.Binary) {
                List<byte> tobytes = new List<byte>();
                unknownBytes = new List<long>();
                string[] array = patchString.Split(new char[] { ',', ' ' });
                for (int i = 0; i < array.Length; i++) {
                    string s = array[i];
                    if (s.Equals("??") || s.Equals("0x??")) {
                        tobytes.Add(0);
                        unknownBytes.Add(i);
                    } else {
                        tobytes.Add(Convert.ToByte(s, 16));
                    }
                }
                return tobytes.ToArray();
            } else if (type == PatchType.StringASCII) {
                return Encoding.ASCII.GetBytes(patchString);
            } else if (type == PatchType.StringUTF8) {
                return Encoding.UTF8.GetBytes(patchString);
            } else if (type == PatchType.StringShiftJIS) {
                return Encoding.GetEncoding("SHIFT-JIS").GetBytes(patchString);
            } else {
                throw new Exception("invalid patch type: " + type);
            }
        }

        internal static PatchResult ApplyPatch(ref byte[] data, long offset, byte[] patch, byte[] original, List<long> unknownBytesPatch, List<long> unknownBytesOriginal, bool isString = false) {
            if (original != null) {
                if (isString) {
                    if (patch.Length > original.Length) {
                        Program.Log("String length ("+patch.Length+") is too long for original ("+original.Length+")");
                        return PatchResult.STRING_TOO_LONG;
                    }
                } else if (original.Length != patch.Length) {
                    Program.Log("patch and original have different length");
                    return PatchResult.LENGTH_MISMATCH;
                }
            }

            bool allMatch = true;
            for (int i = 0; i < patch.Length; i++) {
                if (!unknownBytesPatch.Contains(i) && data[offset + i] != patch[i]) {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) {
                Program.Log("Patch at 0x" + offset.ToString("X2") + " is already applied");
                return PatchResult.OK_ALREADY;
            }

            if (original != null) {
                for (int i = 0; i < original.Length; i++) {
                    if (!unknownBytesOriginal.Contains(i) && data[offset + i] != original[i]) {
                        Program.Log("Byte mismatch at location " + $"0x{(offset + i):X}" + ", expected: " + $"0x{(original[i]):X}" + ", got: " + $"0x{(data[offset + i]):X}");
                        return PatchResult.BYTE_MISMATCH;
                    }
                }
            }

            int j;
            for (j = 0; j < patch.Length; j++) {
                if (!unknownBytesPatch.Contains(j)) {
                    data[offset + j] = patch[j];
                }
            }
            if (isString && original != null) {
                for (; j < original.Length; j++) {
                    data[offset + j] = 0x00;
                }
            }

            return PatchResult.OK;
        }

        internal static List<long> SearchOffsets(byte[] data, byte[] patch, List<long> unknownBytes, int maxHits) {
            if (unknownBytes == null) {
                unknownBytes = new List<long>();
            }
            List<long> results = new List<long>();
            if (unknownBytes.Contains(0)) {
                Program.Log("The first byte of a search list can't be unknown!");
                return results;
            }
            if (Program.IsVerbose) {
                StringBuilder str = new StringBuilder();
                for (int i = 0; i < patch.Length; i++) {
                    if (unknownBytes.Contains(i)) {
                        str.Append("0x?? ");
                    } else {
                        str.Append("0x");
                        str.Append(patch[i].ToString("X2"));
                        str.Append(' ');
                    }
                }
                Program.LogVerbose("Performing search in " + data.Length + " bytes for " + str);
                Program.LogVerbose("Unknown bytes are: " + string.Join(" ", unknownBytes));
            }
            int hitOffset = 0;
            for (int i = 0; i < data.Length - patch.Length; i++) {
                if (data[i] == patch[hitOffset]) {
                    for (hitOffset = 1; i + hitOffset < data.Length - patch.Length && hitOffset < patch.Length; hitOffset++) {
                        if (unknownBytes.Contains(hitOffset) || data[i + hitOffset] == patch[hitOffset]) {
                            if (hitOffset == patch.Length - 1) {
                                Program.Log("Successful hit at 0x" + i.ToString("X2"));
                                results.Add(i);
                                if (results.Count >= maxHits) {
                                    Program.Log("Max hits reached: " + maxHits);
                                    return results;
                                }
                                break;
                            }
                        } else {
                            if (hitOffset > 3) {
                                Program.LogVerbose("Possible hit failed at 0x" + i.ToString("X2"));
                            }
                            break;
                        }
                    }
                    i += hitOffset;
                    hitOffset = 0;
                }
            }

            Program.Log(results.Count + " hits");
            return results;
        }

    }
}