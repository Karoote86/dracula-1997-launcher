"""Generates the launcher's EXE patch files (data/patches/*.drp) from hires_patch.py builds.

Each .drp rebuilds one finished Dracula.exe from the user's ORIGINAL Dracula.exe (verified by SHA-256) without
storing any game bytes: every run of >= MINRUN bytes that also exists in the original EXE is stored as a
reference (COPY offset,len); the strategy-map mask pixels (taken from the user's Art\\Council.art) are a MASK hole
that the launcher fills itself. Only our own new bytes are stored literally.

Usage (developer side, needs the original game files):
    py -I make_patchset.py <original Dracula.exe> <Council.art> <hires_patch.py> <outdir>
Format (deflate-compressed after the 8-byte magic):
    'DRPATCH1' | deflate( u32 target size | 32 bytes target sha256 | u16 name len | name utf8 | ops... )
    op 1 COPY u32 off, u32 len    op 2 LIT u32 len, bytes    op 3 MASK u32 len    op 0 END
"""
import hashlib, os, struct, subprocess, sys, tempfile, zlib

MINRUN = 4
LEVELS = ['920x480', '1040x560', '1160x640', '1280x720', '1520x880', '1840x1080', '2080x1240']
DISPS = ['1600x900', '1920x1080', '2560x1440', '3840x2160', '5120x2880', '1920x1200', '2560x1080', '3440x1440']
BARS = ['blur', 'black']
NOCD = (0x1C9A0, bytes.fromhex('83ec30'), bytes.fromhex('b001c3'))


def council_mask_pixels(path):
    """Same algorithm as hires_patch.py council_mask(): art pixels of all spans, row-major, concatenated."""
    d = open(path, 'rb').read()
    assert d[0] == 10 and d[2] == 1 and d[3] == 8 and d[-769] == 12, 'unexpected Council.art format'
    bpl = struct.unpack_from('<H', d, 66)[0]
    pal = d[-768:]
    px, i = bytearray(), 128
    while len(px) < bpl * 480:
        c = d[i]; i += 1
        if c >= 0xc0: px += bytes([d[i]]) * (c & 0x3f); i += 1
        else: px.append(c)
    dark = [max(pal[3 * v:3 * v + 3]) <= 40 for v in range(256)]
    m = bytearray(640 * 480)
    st = [(x, y) for x in range(640) for y in (0, 479)] + [(x, y) for y in range(480) for x in (0, 639)]
    while st:
        x, y = st.pop()
        if 0 <= x < 640 and 0 <= y < 480 and not m[y * 640 + x] and dark[px[y * bpl + x]]:
            m[y * 640 + x] = 1
            st += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    out = bytearray()
    for y in range(480):
        for x in range(640):
            if m[y * 640 + x]: out.append(px[y * bpl + x])
    return bytes(out)


def encode(base, target, mask_at, mask_len):
    """Greedy diff: same-offset runs first, then 4-gram index matches anywhere in base, else literal."""
    idx = {}
    for i in range(len(base) - MINRUN + 1):
        idx.setdefault(base[i:i + MINRUN], []).append(i)
    ops, lit = [], bytearray()

    def flush():
        if lit: ops.append((2, bytes(lit))); lit.clear()

    i, n = 0, len(target)
    while i < n:
        if mask_at is not None and i == mask_at:
            flush(); ops.append((3, mask_len)); i += mask_len; continue
        end = n if mask_at is None or i > mask_at else mask_at
        best_off, best_len = -1, 0
        if i < len(base) and base[i] == target[i]:              # same offset
            k = i
            while k < end and k < len(base) and base[k] == target[k]: k += 1
            best_off, best_len = i, k - i
        if best_len < 64:
            for c in idx.get(target[i:i + MINRUN], ())[:256]:
                k = 0
                while i + k < end and c + k < len(base) and base[c + k] == target[i + k]: k += 1
                if k > best_len: best_off, best_len = c, k
        if best_len >= MINRUN:
            flush(); ops.append((1, best_off, best_len)); i += best_len
        else:
            lit.append(target[i]); i += 1
    flush()
    return ops


def apply(base, ops, mask):
    out = bytearray()
    for op in ops:
        if op[0] == 1: out += base[op[1]:op[1] + op[2]]
        elif op[0] == 2: out += op[1]
        elif op[0] == 3: assert op[1] == len(mask); out += mask
    return bytes(out)


def serialize(name, target, ops):
    b = bytearray(struct.pack('<I', len(target)) + hashlib.sha256(target).digest())
    nm = name.encode()
    b += struct.pack('<H', len(nm)) + nm
    for op in ops:
        if op[0] == 1: b += struct.pack('<BII', 1, op[1], op[2])
        elif op[0] == 2: b += struct.pack('<BI', 2, len(op[1])) + op[1]
        elif op[0] == 3: b += struct.pack('<BI', 3, op[1])
    b += b'\0'
    co = zlib.compressobj(9, zlib.DEFLATED, -15)
    return b'DRPATCH1' + co.compress(bytes(b)) + co.flush()


def main():
    orig_exe, council, hires, outdir = sys.argv[1:5]
    base = open(orig_exe, 'rb').read()
    assert len(base) == 611328 and base[NOCD[0]:NOCD[0] + 3] == NOCD[1], 'need the ORIGINAL Dracula.exe'
    mask = council_mask_pixels(council)
    os.makedirs(outdir, exist_ok=True)
    tmp = tempfile.mkdtemp()
    nocd = bytearray(base); nocd[NOCD[0]:NOCD[0] + 3] = NOCD[2]
    nocd_path = os.path.join(tmp, 'nocd.exe'); open(nocd_path, 'wb').write(nocd)
    game = os.path.join(tmp, 'game'); os.makedirs(os.path.join(game, 'Art'))
    open(os.path.join(game, 'Art', 'Council.art'), 'wb').write(open(council, 'rb').read())
    total = 0
    for lv in LEVELS:
        for disp in DISPS:
            for bars in BARS:
                name = f'z{lv}_d{disp}_{bars}'
                dst = os.path.join(game, 'Dracula.exe')
                w, h = lv.split('x')
                cmd = [sys.executable, '-I', hires, nocd_path, dst, w, h, f'--disp={disp}']
                if bars == 'black': cmd.append('--bars=black')
                subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
                target = open(dst, 'rb').read()
                at = target.find(mask) if bars == 'blur' else -1
                if bars == 'blur':
                    assert at > 0 and target.find(mask, at + 1) < 0, 'mask block not found exactly once'
                ops = encode(base, target, at if at >= 0 else None, len(mask))
                assert apply(base, ops, mask) == target, 'round trip failed'
                lits = sum(len(o[1]) for o in ops if o[0] == 2)
                data = serialize(name, target, ops)
                open(os.path.join(outdir, name + '.drp'), 'wb').write(data)
                total += len(data)
                print(f'{name}: {len(ops)} ops, {lits} literal bytes, {len(data)} bytes, '
                      f'sha256 {hashlib.sha256(target).hexdigest()[:16]}', flush=True)
    print(f'total {total} bytes')


if __name__ == '__main__':
    main()
