// Reads an Alcohol 120% MDS/MDF disc image: track table, ISO 9660 file system of the data track,
// raw audio of the audio tracks. Only the layout fields needed for this game's disc are interpreted.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DraculaLauncher
{
    public sealed class Track
    {
        public int Number;
        public bool IsAudio;
        public int SectorSize;      // bytes per sector in the MDF (2352 or 2448 with subchannel)
        public long Offset;         // byte offset of the first sector in the MDF
        public int Length;          // sectors
    }

    public sealed class IsoFile
    {
        public string Path;         // relative, '\' separated
        public bool IsDir;
        public int Lba;
        public long Size;
    }

    public sealed class DiscImage : IDisposable
    {
        readonly FileStream mdf;
        public readonly List<Track> Tracks = new List<Track>();

        public DiscImage(string mdsPath, string mdfPath)
        {
            byte[] d = File.ReadAllBytes(mdsPath);
            if (Encoding.ASCII.GetString(d, 0, 16) != "MEDIA DESCRIPTOR")
                throw new InvalidDataException(L.T("Not an MDS file: {0}", mdsPath));
            int sess = BitConverter.ToInt32(d, 0x50);
            int nblocks = d[sess + 10];
            int trk = BitConverter.ToInt32(d, sess + 20);
            for (int t = 0; t < nblocks; t++)
            {
                int o = trk + t * 80;
                int mode = d[o], point = d[o + 4];
                if (point == 0 || point >= 0xA0) continue;
                int extra = BitConverter.ToInt32(d, o + 0x0C);
                var tr = new Track
                {
                    Number = point,
                    IsAudio = mode == 0xA9,
                    SectorSize = BitConverter.ToUInt16(d, o + 0x10),
                    Offset = BitConverter.ToInt64(d, o + 0x28),
                    Length = extra != 0 ? BitConverter.ToInt32(d, extra + 4) : 0,
                };
                Tracks.Add(tr);
            }
            mdf = new FileStream(mdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Track DataTrack
        {
            get { foreach (var t in Tracks) if (!t.IsAudio) return t; throw new InvalidDataException(L.T("No data track")); }
        }

        // 2048 user bytes of a data sector (raw 2352: 12 sync + 4 header [+ 8 subheader for Mode 2 Form 1]).
        public byte[] ReadDataSectors(int lba, int count)
        {
            Track t = DataTrack;
            int hdr = t.SectorSize >= 2352 ? 16 : 0;
            byte[] outb = new byte[count * 2048];
            byte[] raw = new byte[t.SectorSize];
            for (int i = 0; i < count; i++)
            {
                mdf.Position = t.Offset + (long)(lba + i) * t.SectorSize;
                ReadFull(raw, t.SectorSize);
                // Mode 2 Form 1 (this disc): 8-byte subheader after the header -> data at 24.
                int off = hdr == 16 && raw[15] == 2 ? 24 : hdr;
                Buffer.BlockCopy(raw, off, outb, i * 2048, 2048);
            }
            return outb;
        }

        // Raw 16-bit stereo 44.1 kHz PCM of an audio track (subchannel bytes stripped).
        public void CopyAudio(Track t, Stream dst)
        {
            byte[] raw = new byte[t.SectorSize];
            mdf.Position = t.Offset;
            for (int i = 0; i < t.Length; i++)
            {
                ReadFull(raw, t.SectorSize);
                dst.Write(raw, 0, 2352);
            }
        }

        void ReadFull(byte[] b, int n)
        {
            int got = 0;
            while (got < n) { int r = mdf.Read(b, got, n - got); if (r <= 0) throw new EndOfStreamException(); got += r; }
        }

        // ISO 9660 (no Joliet needed: the disc uses 8.3 names).
        public List<IsoFile> ListFiles()
        {
            byte[] pvd = ReadDataSectors(16, 1);
            if (pvd[0] != 1 || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001")
                throw new InvalidDataException(L.T("No ISO 9660 file system in the data track"));
            var list = new List<IsoFile>();
            int rootLba = BitConverter.ToInt32(pvd, 156 + 2);
            int rootLen = BitConverter.ToInt32(pvd, 156 + 10);
            Walk(rootLba, rootLen, "", list);
            return list;
        }

        void Walk(int lba, int len, string prefix, List<IsoFile> list)
        {
            byte[] dir = ReadDataSectors(lba, (len + 2047) / 2048);
            int p = 0;
            while (p < len)
            {
                int rl = dir[p];
                if (rl == 0) { p = (p / 2048 + 1) * 2048; continue; }
                int flba = BitConverter.ToInt32(dir, p + 2);
                int fsize = BitConverter.ToInt32(dir, p + 10);
                bool isDir = (dir[p + 25] & 2) != 0;
                int nl = dir[p + 32];
                if (!(nl == 1 && (dir[p + 33] == 0 || dir[p + 33] == 1)))
                {
                    string name = Encoding.ASCII.GetString(dir, p + 33, nl);
                    int semi = name.IndexOf(';');
                    if (semi >= 0) name = name.Substring(0, semi);
                    if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);
                    string path = prefix + name;
                    list.Add(new IsoFile { Path = path, IsDir = isDir, Lba = flba, Size = fsize });
                    if (isDir) Walk(flba, fsize, path + "\\", list);
                }
                p += rl;
            }
        }

        public void ExtractFile(IsoFile f, string dstPath)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dstPath));
            using (var o = new FileStream(dstPath, FileMode.Create, FileAccess.Write))
            {
                long left = f.Size; int lba = f.Lba;
                while (left > 0)
                {
                    int n = (int)Math.Min(64, (left + 2047) / 2048);
                    byte[] b = ReadDataSectors(lba, n);
                    int w = (int)Math.Min(left, b.Length);
                    o.Write(b, 0, w);
                    left -= w; lba += n;
                }
            }
        }

        public void Dispose() { mdf.Dispose(); }
    }
}
