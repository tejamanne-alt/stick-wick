"""Check the V4 character files after an export (no Blender, no Unity needed):

    python3 tools/check_v4_fbx.py [an earlier single StickmanV4.fbx to compare every clip against]

- every file has the same skeleton (bone names and parents);
- every clip in the JSON is in exactly the file it names, with curves;
- with the optional file: every clip's keys are identical to it (the split changed nothing);
- every clip name the game code plays exists.
Unity binds the part files' clips to the body only because StickmanImport keeps their root (preserveHierarchy);
the scene build also fails on any clip that misses the body's bones (PrototypeBuilder.BuildMoveSet)."""
import struct, zlib, array
def parse(path):
    d = open(path, "rb").read()
    ver = struct.unpack_from("<I", d, 23)[0]
    big = ver >= 7500
    def node(o):
        if big: end, n, plen = struct.unpack_from("<QQQ", d, o); o += 24
        else: end, n, plen = struct.unpack_from("<III", d, o); o += 12
        nl = d[o]; o += 1
        if end == 0: return None, o
        name = d[o:o+nl]; o += nl
        props = []
        for _ in range(n):
            t = chr(d[o]); o += 1
            if t in "YCIFDL":
                fmt = {"Y":"<h","C":"<?","I":"<i","F":"<f","D":"<d","L":"<q"}[t]
                props.append(struct.unpack_from(fmt, d, o)[0]); o += struct.calcsize(fmt)
            elif t in "fdlib":
                ln, enc, cl = struct.unpack_from("<III", d, o); o += 12
                raw = d[o:o+cl]; o += cl
                if enc: raw = zlib.decompress(raw)
                code = {"f":"f","d":"d","l":"q","i":"i","b":"b"}[t]
                props.append(array.array(code, raw).tolist())
            elif t in "SR":
                ln = struct.unpack_from("<I", d, o)[0]; o += 4; props.append(d[o:o+ln]); o += ln
        kids = []
        while o < end:
            k, o = node(o)
            if k is None: break
            kids.append(k)
        return (name, props, kids), end
    o = 27; top = []
    while o < len(d) - 200:
        k, o = node(o)
        if k is None: break
        top.append(k)
    return top

def nm(b): return b.split(b"\x00")[0].decode()

def load(path):
    top = parse(path)
    objs = {}; models = {}; stacks = {}; layers = {}; cnodes = {}; curves = {}
    conns = []
    for name, props, kids in top:
        if name == b"Objects":
            for kn, kp, kk in kids:
                uid = kp[0]
                if kn == b"Model": models[uid] = (nm(kp[1]), kp[2].decode())
                elif kn == b"AnimationStack": stacks[uid] = nm(kp[1])
                elif kn == b"AnimationLayer": layers[uid] = nm(kp[1])
                elif kn == b"AnimationCurveNode": cnodes[uid] = nm(kp[1])
                elif kn == b"AnimationCurve":
                    sub = {k[0]: k[1] for k in kk}
                    curves[uid] = (sub.get(b"KeyTime", [[]])[0], sub.get(b"KeyValueFloat", [[]])[0])
        if name == b"Connections":
            for kn, kp, kk in kids:
                conns.append((kp[0], kp[1], kp[2], nm(kp[3]) if len(kp) > 3 else None))
    parent = {}
    for t, a, b, p in conns:
        if t == b"OO" and a in models and (b in models or b == 0): parent[a] = b
    def path(u):
        s = []
        while u in models: s.append(models[u][0]); u = parent.get(u, 0)
        return "/".join(reversed(s))
    hier = sorted(path(u) for u in models if models[u][1] in ("Null", "LimbNode"))
    layer_stack = {a: b for t, a, b, p in conns if t == b"OO" and a in layers and b in stacks}
    cn_layer = {a: b for t, a, b, p in conns if t == b"OO" and a in cnodes and b in layers}
    cn_model = {a: (models[b][0], p) for t, a, b, p in conns if t == b"OP" and a in cnodes and b in models}
    cv_cn = [(a, b, p) for t, a, b, p in conns if t == b"OP" and a in curves and b in cnodes]
    clips = {}
    for cv, cn, axis in cv_cn:
        st = stacks.get(layer_stack.get(cn_layer.get(cn)))
        if st is None or cn not in cn_model: continue
        bone, prop = cn_model[cn]
        clips.setdefault(st.split("|")[-1], {})[(bone, prop, axis)] = curves[cv]
    roots = [models[u] for u in models if parent.get(u, 0) == 0]
    return dict(hier=hier, clips=clips, roots=roots, takes=[s.split("|")[-1] for s in stacks.values()])


def main():
    import sys, os, re, glob, json, collections
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    art = os.path.join(root, "Assets/Art/StickmanV4")
    js = json.load(open(os.path.join(art, "stickman_v4_clips.json")))["clips"]
    files = sorted({m["file"] for m in js})
    new = {f: load(os.path.join(art, f)) for f in files}
    ok = True
    base = None
    for f, m in new.items():
        bones = [h.split("/", 1)[-1] for h in m["hier"]]
        base = base or bones
        print(f"{f}: roots {[r[0] for r in m['roots']]}, {len(m['takes'])} clips, {len(bones)} bones")
        if bones != base: ok = False; print("  SKELETON DIFFERS from", files[0])
    where = collections.defaultdict(list)
    for f, m in new.items():
        for c in m["takes"]: where[c].append(f)
    for c in js:
        if where.get(c["name"]) != [c["file"]]: ok = False; print("  clip", c["name"], "is in", where.get(c["name"]), "- the JSON says", c["file"])
        elif c["name"] not in new[c["file"]]["clips"]: ok = False; print("  clip", c["name"], "has no curves")
    if len(sys.argv) > 1:
        old = load(sys.argv[1]); diff = 0
        for c in old["takes"]:
            a = old["clips"].get(c, {}); b = new[where[c][0]]["clips"].get(c, {}) if where.get(c) else {}
            same = set(a) == set(b) and all(a[k][0] == b[k][0] and len(a[k][1]) == len(b[k][1]) and
                                            all(abs(x - y) <= 1e-5 for x, y in zip(a[k][1], b[k][1])) for k in a)
            if not same: diff += 1; print("  clip", c, "differs from", sys.argv[1])
        print(f"compared {len(old['takes'])} clips with {sys.argv[1]}: {diff} differ")
        ok = ok and diff == 0
    names = {c["name"] for c in js}
    call = re.compile(r'\b(?:Play|PuppetPlay|Has|HeldPlay)\s*\(\s*"([a-z][a-z0-9_]*)"|\b(?:clip|react|getupClip)\s*=\s*"([a-z][a-z0-9_]*)"')
    guarded = {"block", "hit", "punch", "scoop"}                     # behind a Has() fallback, or not a clip name
    for f in glob.glob(os.path.join(root, "Assets/Scripts/**/*.cs"), recursive=True):
        if "/Editor/" in f: continue
        for i, line in enumerate(open(f), 1):
            for a, b in call.findall(line):
                n = a or b
                if n not in names and n not in guarded: ok = False; print(f"  {os.path.basename(f)}:{i} plays '{n}': no such clip")
    print("OK" if ok else "PROBLEMS FOUND")
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
