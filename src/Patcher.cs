// Applies the launcher's .drp EXE patches (see tools\make_patchset.py) and the small file patches.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace DraculaLauncher
{
    public static class Patcher
    {
        public static string Sha256(byte[] b)
        {
            using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
        }

        // Rebuilds the patched EXE from the ORIGINAL Dracula.exe bytes and the user's Art\Council.art.
        public static byte[] ApplyExePatch(byte[] drp, byte[] origExe, byte[] councilArt)
        {
            if (drp.Length < 8 || Encoding.ASCII.GetString(drp, 0, 8) != "DRPATCH1")
                throw new InvalidDataException("Not a patch file");
            byte[] body;
            using (var ms = new MemoryStream(drp, 8, drp.Length - 8))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
            using (var o = new MemoryStream()) { ds.CopyTo(o); body = o.ToArray(); }

            var r = new BinaryReader(new MemoryStream(body));
            int size = r.ReadInt32();
            byte[] hash = r.ReadBytes(32);
            r.ReadBytes(r.ReadUInt16());   // name
            byte[] mask = null;
            var outp = new MemoryStream(size);
            while (true)
            {
                byte op = r.ReadByte();
                if (op == 0) break;
                if (op == 1)
                {
                    int off = r.ReadInt32(), len = r.ReadInt32();
                    outp.Write(origExe, off, len);
                }
                else if (op == 2)
                {
                    int len = r.ReadInt32();
                    outp.Write(r.ReadBytes(len), 0, len);
                }
                else if (op == 3)
                {
                    int len = r.ReadInt32();
                    if (mask == null) mask = CouncilMaskPixels(councilArt);
                    if (mask.Length != len) throw new InvalidDataException("Council.art does not match the patch");
                    outp.Write(mask, 0, len);
                }
                else throw new InvalidDataException("Patch file damaged");
            }
            byte[] result = outp.ToArray();
            if (result.Length != size || Sha256(result) != BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant())
                throw new InvalidDataException("Result checksum does not match");
            return result;
        }

        // Black torn edges of the strategy-map parchment: flood fill of near-black pixels (max(r,g,b) <= 40)
        // from the picture border in the first 480 rows of Council.art (PCX). Returns the pixels of the
        // filled area in row-major order (same as hires_patch.py council_mask()).
        public static byte[] CouncilMaskPixels(byte[] d)
        {
            if (!(d[0] == 10 && d[2] == 1 && d[3] == 8 && d[d.Length - 769] == 12))
                throw new InvalidDataException("Council.art has an unexpected format");
            int bpl = BitConverter.ToUInt16(d, 66);
            int palAt = d.Length - 768;
            var px = new byte[bpl * 480 + 64];
            int n = 0, i = 128;
            while (n < bpl * 480)
            {
                int c = d[i++];
                if (c >= 0xC0) { byte v = d[i++]; for (int k = 0; k < (c & 0x3F); k++) px[n++] = v; }
                else px[n++] = (byte)c;
            }
            var dark = new bool[256];
            for (int v = 0; v < 256; v++)
                dark[v] = Math.Max(d[palAt + 3 * v], Math.Max(d[palAt + 3 * v + 1], d[palAt + 3 * v + 2])) <= 40;
            var m = new bool[640 * 480];
            var st = new Stack<int>();
            for (int x = 0; x < 640; x++) { st.Push(x); st.Push(479 * 640 + x); }
            for (int y = 0; y < 480; y++) { st.Push(y * 640); st.Push(y * 640 + 639); }
            while (st.Count > 0)
            {
                int p = st.Pop(), x = p % 640, y = p / 640;
                if (m[p] || !dark[px[y * bpl + x]]) continue;
                m[p] = true;
                if (x < 639) st.Push(p + 1);
                if (x > 0) st.Push(p - 1);
                if (y < 479) st.Push(p + 640);
                if (y > 0) st.Push(p - 640);
            }
            var outp = new MemoryStream();
            for (int y = 0; y < 480; y++)
                for (int x = 0; x < 640; x++)
                    if (m[y * 640 + x]) outp.WriteByte(px[y * bpl + x]);
            return outp.ToArray();
        }

        // Cinema\*.cin (Smacker SMK2): header flags 0x2 (interlaced, black lines) -> 0x4 (line-doubled).
        public static bool PatchVideo(string path)
        {
            using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                var h = new byte[0x18];
                if (f.Read(h, 0, 0x18) != 0x18 || Encoding.ASCII.GetString(h, 0, 4) != "SMK2") return false;
                int flags = BitConverter.ToInt32(h, 0x14);
                if (flags == 4) return true;
                if (flags != 2) return false;
                f.Position = 0x14;
                f.Write(BitConverter.GetBytes(4), 0, 4);
                return true;
            }
        }
    }
}
