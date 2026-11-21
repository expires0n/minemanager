"""MineManager ikon uretici.

assets/logo.png (seffaf PNG) -> assets/app.ico (16..256) + assets/logo64.png
PIL gerektirmez: saf Python PNG cozucu + alan ortalamali (box) olcekleme.
"""
import os
import struct
import zlib
from array import array

SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
FIT = 0.96  # ikon karesi icinde logoya ayrilan alan


def read_png(path):
    data = open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("PNG degil: " + path)
    pos, idat, w, h = 8, b"", 0, 0
    while pos < len(data):
        ln = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + ln]
        if tag == b"IHDR":
            w, h, depth, color, comp, filt, inter = struct.unpack(">IIBBBBB", chunk)
            if depth != 8 or color != 6 or inter != 0:
                raise ValueError("RGBA 8-bit, interlaced olmayan PNG gerekli")
        elif tag == b"IDAT":
            idat += chunk
        elif tag == b"IEND":
            break
        pos += 12 + ln

    raw = zlib.decompress(idat)
    stride = w * 4
    rows, prev, i = [], bytearray(stride), 0
    for _ in range(h):
        ft = raw[i]
        i += 1
        line = bytearray(raw[i:i + stride])
        i += stride
        if ft == 1:
            for x in range(4, stride):
                line[x] = (line[x] + line[x - 4]) & 255
        elif ft == 2:
            for x in range(stride):
                line[x] = (line[x] + prev[x]) & 255
        elif ft == 3:
            for x in range(stride):
                line[x] = (line[x] + (((line[x - 4] if x >= 4 else 0) + prev[x]) >> 1)) & 255
        elif ft == 4:
            for x in range(stride):
                a = line[x - 4] if x >= 4 else 0
                b = prev[x]
                c = prev[x - 4] if x >= 4 else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        elif ft != 0:
            raise ValueError("bilinmeyen PNG filtresi: %d" % ft)
        rows.append(line)
        prev = line
    return w, h, rows


def crop_alpha(w, h, rows):
    minx, miny, maxx, maxy = w, h, -1, -1
    for y in range(h):
        r = rows[y]
        for x in range(w):
            if r[x * 4 + 3] > 4:
                if x < minx: minx = x
                if x > maxx: maxx = x
                if y < miny: miny = y
                if y > maxy: maxy = y
    if maxx < 0:
        raise ValueError("logo tamamen seffaf")
    return minx, miny, maxx - minx + 1, maxy - miny + 1


def build_sat(w, h, rows, ox, oy):
    """Premultiplied R,G,B ve A icin toplam-alan tablosu (int64)."""
    W = w + 1
    sr = array("q", [0]) * (W * (h + 1))
    sg = array("q", [0]) * (W * (h + 1))
    sb = array("q", [0]) * (W * (h + 1))
    sa = array("q", [0]) * (W * (h + 1))
    for y in range(h):
        row = rows[oy + y]
        ar = ag = ab = aa = 0
        base = (y + 1) * W
        up = base - W
        for x in range(w):
            o = (ox + x) * 4
            a = row[o + 3]
            ar += row[o] * a
            ag += row[o + 1] * a
            ab += row[o + 2] * a
            aa += a
            sr[base + x + 1] = sr[up + x + 1] + ar
            sg[base + x + 1] = sg[up + x + 1] + ag
            sb[base + x + 1] = sb[up + x + 1] + ab
            sa[base + x + 1] = sa[up + x + 1] + aa
    return sr, sg, sb, sa


def rect_sum(sat, w, x0, y0, x1, y1):
    W = w + 1
    return sat[y1 * W + x1] - sat[y0 * W + x1] - sat[y1 * W + x0] + sat[y0 * W + x0]


def render(size, bw, bh, sat):
    """Kare, seffaf zemin; logo kutuya oranli sigar."""
    px = bytearray([255, 255, 255, 0]) * (size * size)
    box = int(round(size * FIT))
    cw, ch = box, int(round(box * bh / float(bw)))
    if ch > box:
        ch = box
        cw = int(round(ch * bw / float(bh)))
    ox, oy = (size - cw) // 2, (size - ch) // 2
    sr, sg, sb, sa = sat
    for ty in range(ch):
        sy0 = ty * bh // ch
        sy1 = (ty + 1) * bh // ch
        if sy1 <= sy0:
            sy1 = sy0 + 1
        for tx in range(cw):
            sx0 = tx * bw // cw
            sx1 = (tx + 1) * bw // cw
            if sx1 <= sx0:
                sx1 = sx0 + 1
            cnt = (sx1 - sx0) * (sy1 - sy0)
            a = rect_sum(sa, bw, sx0, sy0, sx1, sy1)
            o = ((oy + ty) * size + (ox + tx)) * 4
            if a <= 0:
                continue
            px[o + 0] = min(255, int(rect_sum(sr, bw, sx0, sy0, sx1, sy1) / a + 0.5))
            px[o + 1] = min(255, int(rect_sum(sg, bw, sx0, sy0, sx1, sy1) / a + 0.5))
            px[o + 2] = min(255, int(rect_sum(sb, bw, sx0, sy0, sx1, sy1) / a + 0.5))
            px[o + 3] = min(255, int(a / float(cnt) + 0.5))
    return bytes(px)


def write_png(path, size, rgba):
    raw = bytearray()
    stride = size * 4
    for y in range(size):
        raw.append(0)
        raw += rgba[y * stride:(y + 1) * stride]

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
           + chunk(b"IEND", b""))
    with open(path, "wb") as f:
        f.write(png)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    assets = os.path.join(root, "assets")
    src = os.path.join(assets, "logo.png")
    if not os.path.exists(src):
        raise SystemExit("assets/logo.png yok")

    w, h, rows = read_png(src)
    ox, oy, bw, bh = crop_alpha(w, h, rows)
    print("logo %dx%d, icerik %dx%d @%d,%d" % (w, h, bw, bh, ox, oy))
    sat = build_sat(bw, bh, rows, ox, oy)

    frames = []
    for s in SIZES:
        rgba = render(s, bw, bh, sat)
        frames.append((s, rgba))
        print("  %3dpx" % s)

    logo64 = dict(frames)[64]
    write_png(os.path.join(assets, "logo64.png"), 64, logo64)

    header = struct.pack("<HHH", 0, 1, len(frames))
    offset = 6 + 16 * len(frames)
    directory, payload = b"", b""
    for s, rgba in frames:
        # ICO icinde PNG frame (Vista+)
        tmp = os.path.join(assets, "_frame.png")
        write_png(tmp, s, rgba)
        data = open(tmp, "rb").read()
        os.remove(tmp)
        directory += struct.pack("<BBBBHHII",
                                 0 if s >= 256 else s,
                                 0 if s >= 256 else s,
                                 0, 0, 1, 32, len(data), offset)
        offset += len(data)
        payload += data

    ico = os.path.join(assets, "app.ico")
    with open(ico, "wb") as f:
        f.write(header + directory + payload)
    print("yazildi: %s (%d bayt), assets/logo64.png" % (ico, os.path.getsize(ico)))


if __name__ == "__main__":
    main()
