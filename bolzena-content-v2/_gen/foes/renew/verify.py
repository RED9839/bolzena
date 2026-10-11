# 원본 마을에 적 리뉴얼(클론 아닌 적 · pools · elites)이 그대로 들어 있는지 _gen/foes/villages/<마을>.json 과 대조한다.
#   python verify.py [마을 …]   — 다르면 「python apply.py <마을>」 로 다시 합친다(클론 항목은 그대로 둔다).
import json, os, sys
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
vs = sys.argv[1:] or [f[:-5] for f in os.listdir(os.path.join(ROOT, '_gen', 'foes', 'villages')) if f.endswith('.json')]
bad = 0
for v in vs:
    a = json.load(open(os.path.join(ROOT, 'world', 'villages', v + '.json'), encoding='utf-8'))
    b = json.load(open(os.path.join(ROOT, '_gen', 'foes', 'villages', v + '.json'), encoding='utf-8'))
    na = {e['id']: e for e in a['enemies'] if 'clone' not in e}; nb = {e['id']: e for e in b['enemies'] if 'clone' not in e}
    diff = sorted(k for k in set(na) | set(nb) if na.get(k) != nb.get(k))
    pools = [(f['pools'], f['elites']) for f in a['villages'][0]['floors']] == [(f['pools'], f['elites']) for f in b['villages'][0]['floors']]
    ok = not diff and pools; bad += not ok
    print(f"{v}: {'그대로' if ok else '다름'}" + (f" · 적 {diff}" if diff else "") + ("" if pools else " · 판 조합 다름"))
sys.exit(1 if bad else 0)
