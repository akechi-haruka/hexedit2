using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Haruka.Arcade.Hexedit2 {
    class IniFile   // revision 11
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ThrowOnUnmappableChar = true)]
        public static extern int GetPrivateProfileSectionNames(byte[] lpszReturnBuffer, int nSize, string lpFileName);

        public IniFile(string IniPath = null) {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Key, string Section = null) {
            var RetVal = new StringBuilder(8192);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 8192, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null) {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null) {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null) {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(string Key, string Section = null) {
            return Read(Key, Section).Length > 0;
        }

        public virtual List<string> GetSections() {
            byte[] buf = new byte[8192];
            GetPrivateProfileSectionNames(buf, buf.Length, Path);
            string allSections = Encoding.ASCII.GetString(buf);
            string[] sectionNames = allSections.Split('\0');
            List<string> s = new List<string>();
            foreach (string sectionName in sectionNames) {
                if (sectionName != string.Empty) {
                    s.Add(sectionName);
                }
            }
            return s;
        }
    }
}