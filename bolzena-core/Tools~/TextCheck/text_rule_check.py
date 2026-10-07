# 설명 글 ↔ 규칙 대조(Docs/설명글.md) — 사도 135명
#  ① 부제(desc) 검사: 숫자 · 결과 낱말(AP · 드로우 · 사기 N · 최대 …)을 쓰면 규칙과 어긋날 수 있다 → 걸린다
#  ② 엔진 글 검사: 규칙(keyword cap · decay · endClear · per · rules, 패시브 when · fx · limit, 고학년 fx)의 수치가 그 사도의 글(CardText.Traits)에 다 나오나
#  쓰는 법: python text_rule_check.py <엔진 글 json(Dump json)> [콘텐츠 폴더]
import json, glob, re, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
traits = {h['id']: h for h in json.load(open(sys.argv[1], encoding='utf-8-sig'))}
ROOT = sys.argv[2] if len(sys.argv) > 2 else r'C:\projects\bolzena-content-v2'
def pct(x): return f'{round(x * 100)}%'
def num(v): return str(int(v)) if float(v).is_integer() else str(v)
problems = []; heroes = 0; checked = 0
def tok_fx(f, out):
    k = f.get('k'); v = f.get('v')
    if f.get('ofEvent'):   # 일의 값 · 저장한 겹으로 정하는 고정값 — ratio 를 글에 쓰지 않는다
        if f.get('ofStack'): out.append(f'「{f["ofStack"]}」 1당'); return
        if k in ('dmg', 'shield'): return
    if k == 'dmg' and 'ratio' in f: out.append(pct(f['ratio']))
    if k in ('shield', 'block', 'heal', 'extra', 'drain', 'recast') and 'ratio' in f: out.append(pct(f['ratio']))
    if k == 'draw' and v: out.append(f'드로우 {num(v)}' if not any(x in f for x in ('who', 'type', 'tag', 'unique', 'basic')) else f'{num(v)}장')
    if k in ('ap', 'nextAp') and v: out.append(f'AP {"+" if v > 0 else ""}{num(v)}')
    if k == 'status' and f.get('id') == '기절': out.append('기절')
    elif k == 'status' and v and f.get('id'): out.append(f'{f["id"]} {num(v)}')
    if k == 'stack' and v and f.get('id'): out.append(f'「{f["id"]}」 {num(v)}')   # 2026-10-07 쌓는 수는 + 없이(「홀로그램」 2)
    if k == 'spend' and f.get('id'): out.append(('ALL', f['id']) if f.get('all') else f'「{f["id"]}」')
    if k in ('dealtMod', 'takenMod', 'atkMod', 'defMod') and v is not None: out.append(f'{round(v * 100)}%')
    if k == 'tough' and v: out.append(f'강인도 피해 {num(v)}')
    for t in f.get('then') or []: tok_fx(t, out)
for f in sorted(glob.glob(ROOT + '/heroes/*/*.json')):
    for h in json.load(open(f, encoding='utf-8')).get('heroes', []):
        heroes += 1
        tr = traits.get(h['id'])
        if tr is None: problems.append((h['id'], '엔진 글 없음')); continue
        body = '\n'.join((t.get('Body') or '') + '\n' + (t.get('Sub') or '') for t in tr['traits'])
        def need(tok, where):
            global checked
            checked += 1
            if isinstance(tok, tuple):   # 「X」 전부 소모 — 자기 규칙에서는 「모두 써서」 로 읽힌다
                if f'「{tok[1]}」 전부 소모' in body or '모두 써서' in body: return
                tok = f'「{tok[1]}」 전부 소모'
            if tok not in body: problems.append((h['id'], f'{where}: 「{tok}」 가 글에 없음'))
        kws = ([h['keyword']] if h.get('keyword') else []) + (h.get('keywords') or [])
        for k in kws:
            d = k.get('desc') or ''
            # ① 부제
            bad = [w for w in ('AP', '드로우', '최대', '턴당', '피해 +', '%') if w in d] + re.findall(r'(?<![가-힣A-Za-z\d])\d+(?![\dV])', d)
            if bad: problems.append((h['id'], f'부제 「{k["name"]}」 에 수치 · 결과 낱말 {bad}: {d}'))
            if len(d) > 24: problems.append((h['id'], f'부제 「{k["name"]}」 가 김({len(d)}자): {d}'))
            # ② 키워드 성질
            cap = k.get('cap')
            if cap and not k.get('mode'): need(f'최대 {cap}' if not k.get('wrap') and not (k.get('carrier') == 'hero' and cap == 1) else ((' → '.join(k['stages'][:1]) if k.get('stages') else f'→ {cap}') if k.get('wrap') else '사도마다 하나'), f'「{k["name"]}」 cap')   # 단계 이름이 있으면 글은 「전채 → 메인 → …」(2026-10-08)
            if k.get('endClear'): need('내 턴이 끝나면', f'「{k["name"]}」 endClear')
            if k.get('decay') or k.get('decayAll'): need('적의 차례가 끝나면', f'「{k["name"]}」 decay')
            if k.get('weakens'): need('약점으로 맞음', f'「{k["name"]}」 weakens')
            if k.get('hunt'): need('한 번에 아군 1명에게만' if k.get('carrier') == 'hero' else '한 번에 적 1명에게만', f'「{k["name"]}」 hunt')
            if k.get('guard'): need('적의 공격 한 대', f'「{k["name"]}」 guard')
            for p in k.get('per') or []:
                need(pct(p['ratio']) if p.get('stat') in ('dot', 'hot') else f'{round(p.get("v", 0) * 100)}%', f'「{k["name"]}」 per {p.get("stat")}')
            for r in k.get('rules') or []:
                toks = []; [tok_fx(x, toks) for x in r.get('fx', [])]
                for t in toks: need(t, f'「{k["name"]}」 규칙 「{r.get("name")}」')
                if r.get('limit'): need(f'({"전투당" if r["limit"].get("per") == "fight" else "턴당"} {r["limit"].get("n", 1)}회)', f'「{k["name"]}」 규칙 제한')
        for r in h.get('passives') or []:
            toks = []; [tok_fx(x, toks) for x in r.get('fx', [])]
            for t in toks: need(t, f'패시브 「{r.get("name")}」')
            if r.get('limit'): need(f'({"전투당" if r["limit"].get("per") == "fight" else "턴당"} {r["limit"].get("n", 1)}회)', f'패시브 「{r.get("name")}」 제한')
        if h.get('ult'):
            toks = []; [tok_fx(x, toks) for x in h['ult'].get('fx', [])]
            for t in toks: need(t, '고학년')
            need(f'게이지 {h["ult"]["cost"]}%', '고학년 게이지')
print(f'사도 {heroes} · 확인한 수치 {checked} · 걸린 것 {len(problems)}')
for hid, p in problems: print(f'{hid}\t{p}')
