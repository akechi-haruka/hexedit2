hexedit2
2024 Akechi Haruka
Licensed under MIT.

-----------------------------------------------

Successor of hexedit-cmd.

Global switches:
-v: Verbose output
-t <Binary/StringASCII/StringUTF8/StringShiftJIS>: Select patch format

Binary is assumed default. For binary data, specify in hexadecimal lists, ex.: 0x00,0x01,0x02,...

Modes of operation:

single:
hexedit2 single <inputfile> <outputfile> <0xoffset> <patchbytes> [originalbytes]

Apply a single patch at a given offset with optional original code known. If originalbytes are not present, the patch will be applied unconditionally

Example:
hexedit2 sync_release.exe sync_release.exe 0x000698D1 0xE9,0x98,0x01,0x00 0x0F,0x86,0x97,0x01


multi:
hexedit2 single <inputfile> <outputfile> <originalbytes> <patchbytes>

Find and patch any occurrences of the given originalbytes or if the offset is unknown.

Example:
hexedit2 multi -t StringUTF8 fc.exe fc.exe データ初期化中です。暫くお待ちください。 "NOW LOADING DATA. PLEASE WAIT..."
Switches:
-m <maxhits>: Specifies the maximum amount of pattern hits that are allowed


script:
hexedit2 single <inputfile> <outputfile> <scriptfile>
Applies patches in bulk. For an example, check sample.patch.ini.
Switches:
-c: Continue even if a patch fails. (This includes 0-hits from Type=Multi)


Remarks:
* You cannot use string modes if the patch bytes are longer than the original bytes.
* Pattern matching is possible by replacing bytes within originalbytes arguments with "0x??".
 - It is also possible to carry over the pattern to patchbytes by also specifying "0x??" in patchbytes.
* Script files are expected to be encoded in UTF-8.