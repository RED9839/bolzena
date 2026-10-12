# -*- coding: utf-8 -*-
"""교주 카드 · 장비 전후 표(문서용, 2026-10-13) — Kit text · Probe tsv · 사람.tsv 를 마크다운 표로.
    python 표.py <kit text 전> <kit text 뒤> <probe 전 tsv> <probe 뒤 tsv> <사람.tsv> > 표.md
문제 유형은 데이터에서 저절로 센다(아래 규칙) + build.py 의 손질 목록.
  안 쓰임   — 카드: 상점 제시 10번 이상 · 구매 1번 이하 / 장비: 상점 구매 0 · 끝까지 낀 판 3 이하(전리품 · 상점 합 10번 이상 제시)
  죽은 계기 — 엘다인 20명이 내지 않는 태그(열정 · 회수 · 결속 · 주도 · 종극)에 걸린 장비 효과 · 신탁
  규칙     — 겹친 태그(탄환 + 소멸) · 개막 + 비용 0(전투마다 공짜) · 끝없이 쌓이는 세기형
  셈       — 쥐여 주기 +5%p 이상(같은 씨앗 1,800판 · 적 피해 ×1.25)
  약함     — 전설 · 희귀 장비 효과 값 0.35 미만
  긺      — 한 마디 글 55자(강화 카드) · 45자 넘음
"""
import re, sys, os

DEAD = ('「열정」', '「회수」', '「결속」', '「주도」', '「종극」')

def kit(path):
    o = {}; cur = None
    for l in open(path, encoding='utf-8'):
        l = l.rstrip('\n')
        m = re.match(r'\[(장비|교주)\] (.+?) \((.+?)\) 값 ([\d.]+)', l)
        if m:
            cur = {'kind': m.group(1), 'name': m.group(2), 'meta': m.group(3), 'val': float(m.group(4)), 'lines': []}
            o[cur['name']] = cur; continue
        if cur and l.startswith('  ') and not l.startswith('  「'): cur['lines'].append(l.strip())
    return o

def probe(path):
    o = {}
    if not path or not os.path.exists(path): return o
    for l in open(path, encoding='utf-8'):
        p = l.rstrip('\n').split('\t')
        if len(p) < 7 or p[0] == '바탕': continue
        o[p[2]] = {'id': p[1], 'd': float(p[6]), 'kept': p[7] if len(p) > 7 else '-'}
    return o

def human(path):
    o = {}
    lines = open(path, encoding='utf-8').read().splitlines()
    head = lines[0].split('\t')
    for l in lines[1:]:
        p = l.split('\t'); o[p[0]] = dict(zip(head[1:], map(int, p[1:])))
    return o

def eff_of(x):
    if x['kind'] == '장비':
        e = x['lines'][0]
        e = re.sub(r'^((HP|공격력|방어력|치명) [^·]*· ?)*((HP|공격력|방어력|치명) [^·]*$)?', '', e).strip()
        return e or '(능력치만)'
    return x['lines'][0].replace('기본  ', '')

def long_text(x):
    if x['kind'] == '장비':
        return any(len(re.sub(r'\s+', '', p)) > 50 for p in x['lines'][0].split(' · ') if ':' in p)
    for l in x['lines']:
        m = re.match(r'(기본|신탁\d) ?(「[^」]*」)? ?(.*?)(  \(값 [\d.]+\))?$', l)
        if not m: continue
        s = m.group(3); pw = '강화 카드.' in s or '매 턴' in s or '이 전투 동안' in s
        if len(re.sub(r'\s+', '', re.sub(r'^강화 카드\.\s*', '', s))) > (55 if pw else 45): return True
    return False

def problems(x, pb, hu):
    out = []
    if x['kind'] == '장비':
        n = hu.get('shopOffer', 0) + hu.get('lootOffer', 0)
        if n >= 10 and hu.get('buy', 0) == 0 and hu.get('endEquip', 0) <= 3: out.append('안 쓰임')
        if any(t in x['lines'][0] for t in DEAD): out.append('죽은 계기')
        if ('전설' in x['meta'] or '희귀' in x['meta']) and x['val'] < 0.35: out.append('약함')
    else:
        if hu.get('shopOffer', 0) + hu.get('evOffer', 0) >= 10 and hu.get('buy', 0) + hu.get('ev', 0) <= 1: out.append('안 쓰임')
        body = ' '.join(x['lines'])
        if any(f'{t} 카드' in body for t in DEAD): out.append('죽은 계기')   # 다른 「X」 카드를 부르는 신탁(제 태그 주도 · 종극 · 열정 · 회수는 혼자 도니 아님)
        if '소멸. 탄환.' in body or '소멸. 보존. 탄환.' in body: out.append('겹친 태그')
        if re.search(r'비용 0\. / 개막', body): out.append('개막 0코')
    if pb and pb['d'] >= 5: out.append('셈')
    if long_text(x): out.append('긺')
    return out

def main():
    a, b = kit(sys.argv[1]), kit(sys.argv[2])
    pa, pb = probe(sys.argv[3]), probe(sys.argv[4])
    hu = human(sys.argv[5])
    for kind, title in (('교주', '교주 카드'), ('장비', '장비')):
        print(f'### {title}\n')
        if kind == '교주':
            print('| 이름 | 등급 · 비용 | 기본 효과(전) | 사람 상점 제시 · 산 · 이벤트 고름 · 낸 수 | 봇 값 전 → 뒤 | 쥐여 주기 전 → 뒤(%p) | 문제(전) | 기본 효과(뒤) |')
        else:
            print('| 이름 | 등급 · 부위 | 효과(전) | 사람 상점 제시 · 산 · 전리품 · 끝까지 낌 | 봇 값 전 → 뒤 | 쥐여 주기 전 → 뒤(%p) | 문제(전) | 효과(뒤) |')
        print('|---|---|---|---|---|---|---|---|')
        for n, x in a.items():
            if x['kind'] != kind: continue
            y = b.get(n, x); p0 = pa.get(n); p1 = pb.get(n)
            h = hu.get(p0['id'] if p0 else '', {}) if p0 else {}
            hs = (f"{h.get('shopOffer',0)} · {h.get('buy',0)} · {h.get('ev',0)} · {h.get('played',0)}" if kind == '교주'
                  else f"{h.get('shopOffer',0)} · {h.get('buy',0)} · {h.get('loot',0)} · {h.get('endEquip',0)}")
            e0, e1 = eff_of(x), eff_of(y)
            pv = lambda p: f"{p['d']:+.1f}" if p else '-'
            print(f"| {n} | {x['meta'].replace(' · ', ' ')} | {e0.replace('|','/')} | {hs} | {x['val']:.2f} → {y['val']:.2f} | {pv(p0)} → {pv(p1)} | {', '.join(problems(x, p0, h)) or '-'} | {'(그대로)' if y['lines'] == x['lines'] else '(기본 그대로 · 신탁 손질)' if e1 == e0 else e1.replace('|','/')} |")
        print()

if __name__ == '__main__':
    main()
