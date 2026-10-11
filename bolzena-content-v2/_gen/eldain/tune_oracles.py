# 신탁 맞춤 — bz check 의 「기본보다 낫지 않다(코스트 기준 x배)」 를 읽어 oracle_tune.json 에 배율을 곱해 쌓는다(4차 덧붙임 10-11).
#   python tune_oracles.py   → 그 뒤 node build.mjs --apply all · 다시 돌리면 남은 것만 더 올린다(한 번에 ×1.6 까지)
import json, os, re, subprocess, sys
sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
BZ = r'C:/projects/bolzena-core/Tools~/Dev/bz.cmd'
out = subprocess.run([BZ, 'check', '--data', r'C:/projects/bolzena-content-v2'], capture_output=True, text=True, encoding='utf-8').stdout
p = os.path.join(HERE, 'oracle_tune.json')
tune = json.load(open(p, encoding='utf-8')) if os.path.exists(p) else {}
n = 0
for m in re.finditer(r'주의 카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 ([\d.]+)배', out):
    cid, k, x = m.group(1), m.group(2), float(m.group(3))
    f = min(1.6, 1.2 / max(x, 0.75))
    if f <= 1.0: continue
    tune.setdefault(cid, {})[k] = round(min(1.6, tune.get(cid, {}).get(k, 1) * f), 3)   # 한 신탁 합쳐 ×1.6 까지(피해 몫이 작은 신탁이 터무니없이 커지지 않게)
    n += 1
json.dump(tune, open(p, 'w', encoding='utf-8'), ensure_ascii=False, indent=1, sort_keys=True)
print('올린 신탁', n)
