# 적 리뉴얼 결과를 원본 마을에 합친다(사용자 승인 2026-10-10). 클론 항목은 건드리지 않는다.
#   python apply.py <마을>         — _gen/foes/villages/<마을>.json 의 클론 아닌 적(id 단위) · floors 의 pools · elites 를
#                                    world/villages/<마을>.json 에 덮는다. 원본은 쓰기 직전에 다시 읽는다(3성 담당이 클론 항목을 고치는 중).
#   새 id(마을판으로 나눈 공용 적)는 enemies 끝에 붙이고, 그림은 ART_FROM(새 id → 바탕 id)으로 _그림.json 에 같은 그림을 붙인다.
# 원본 백업: C:/projects/_backup/villages_before_foes_20261010/
import json, sys, os
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))

def apply(v, art_from=None, drop=()):
    gen = json.load(open(os.path.join(ROOT, '_gen', 'foes', 'villages', v + '.json'), encoding='utf-8'))
    dst_p = os.path.join(ROOT, 'world', 'villages', v + '.json')
    dst = json.load(open(dst_p, encoding='utf-8'))        # 쓰기 직전 다시 읽기
    new = {e['id']: e for e in gen['enemies'] if 'clone' not in e}
    out, seen, n_rep = [], set(), 0
    for e in dst['enemies']:
        if 'clone' in e: out.append(e); continue
        if e['id'] in drop: continue
        if e['id'] in new: out.append(new[e['id']]); seen.add(e['id']); n_rep += 1
        else: out.append(e)
    added = [i for i in new if i not in seen]
    for i in added: out.append(new[i])
    dst['enemies'] = out
    for fd, fg in zip(dst['villages'][0]['floors'], gen['villages'][0]['floors']):
        fd['pools'] = fg['pools']; fd['elites'] = fg['elites']
    json.dump(dst, open(dst_p, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    if art_from:
        ap = os.path.join(ROOT, 'world', 'villages', '_그림.json')
        art = json.load(open(ap, encoding='utf-8'))
        for nid, base in art_from.items():
            if nid not in art and base in art: art[nid] = dict(art[base])
        json.dump(art, open(ap, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'{v}: 바꾼 적 {n_rep} · 새 적 {len(added)} {added} · 뺀 적 {list(drop)} · 판 조합 덮음')

if __name__ == '__main__':
    apply(sys.argv[1])
