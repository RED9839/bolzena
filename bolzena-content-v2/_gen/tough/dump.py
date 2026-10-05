import json,glob
ROOT='C:/projects/bolzena-content-v2'
def has_tough(o):
    return 'tough' in json.dumps(o) and '"k": "tough"' in json.dumps(o)
def fxs(fx): 
    out=[]
    for f in fx or []:
        k=f.get('k'); s=k
        if k=='tough': s=f"TOUGH{f.get('v')}"+(f"@{f['target']}" if f.get('target') else '')
        elif k=='dmg': s=f"dmg{f.get('ratio')}"+(f"x{f['hits']}" if f.get('hits') else '')+(f"@{f['target']}" if f.get('target') and f['target']!='oneEnemy' else '')
        elif k in('status','stack'): s=f"{k}:{f.get('id')}{f.get('v')}"
        elif k: s=k+(':'+str(f.get('id')) if f.get('id') else '')
        out.append(s)
    return ' '.join(out)
for f in sorted(glob.glob(ROOT+'/heroes/*/*.json')):
    d=json.load(open(f,encoding='utf-8'))
    for h in d.get('heroes',[]):
        for where in ('passives',):
            for p in h.get(where) or []:
                if has_tough(p): print(f"[{h['id']}/{h.get('nature')}] PASSIVE {p.get('name')} when={json.dumps(p.get('when'),ensure_ascii=False)} :: {fxs(p.get('fx'))}")
        kw=h.get('keyword') or {}
        for r in kw.get('rules') or []:
            if has_tough(r): print(f"[{h['id']}] KWRULE {kw.get('name')}/{r.get('name')} when={json.dumps(r.get('when'),ensure_ascii=False)} :: {fxs(r.get('fx'))}")
        u=h.get('ult') or {}
        if has_tough(u): print(f"[{h['id']}] ULT {u.get('name')} :: {fxs(u.get('fx'))}")
    for c in d.get('cards',[]):
        if not has_tough(c): continue
        tags=','.join(c.get('tags') or [])
        print(f"[{c.get('hero')}] CARD {c['id']} 「{c.get('name')}」 {c.get('cost')}코 {c.get('type')} tags={tags} :: {fxs(c.get('fx'))}")
        for i,o in enumerate(c.get('oracles') or []):
            if has_tough(o): print(f"      O{i+1} 「{o.get('name')}」 {('cost'+str(o['cost'])) if 'cost' in o else ''} tags={','.join(o.get('tags') or [])} :: {fxs(o.get('fx'))}")
        for b in c.get('blesses') or []:
            if has_tough(b): print(f"      B 「{b.get('name')}」 :: {json.dumps(b,ensure_ascii=False)[:200]}")
