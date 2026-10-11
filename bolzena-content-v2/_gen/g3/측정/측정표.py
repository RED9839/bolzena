import sys,re
sys.path.insert(0,'C:/projects/bolzena-content-v2/_gen/g3/측정')
from 표 import kit, probe, GROUP
b=kit('before_text.txt'); a=kit('final_text.txt')
P={}
for tag,f in [('전','probe_before.tsv'),('1차','probe_after1.tsv'),('2차','probe_after2.tsv'),('3차','probe_after3.tsv'),('4차','probe_after4.tsv'),('5차','probe_after5.tsv')]:
    P[tag]=probe(f)[0]
def last(n):
    for t in ('5차','4차','3차','2차'):
        if n in P[t]: return f"{P[t][n]['d']:+.1f}({t})"
    return '-'
for kind,title in (('장비','장비 64'),('교주','교주 카드 43')):
    print(f'### {title}\n')
    print('| 이름 | 등급 | 갈래 | 효과(뒤) | 봇 값 전→뒤 | 쥐여 주기 전 | 뒤(최종 측정) |'); print('|---|---|---|---|---|---|---|')
    for n,x in a.items():
        if x['kind']!=kind: continue
        eff=x['lines'][0].replace('기본  ','')
        if kind=='장비': eff=re.sub(r'^(HP|공격력|방어력|치명) [^·]*(· (HP|공격력|방어력|치명) [^·]*)*(· )?','',eff) or '(능력치만)'
        pb=P['전'].get(n)
        print(f"| {n} | {x['grade']} | {GROUP.get(n,'-')} | {eff.replace('|','/')} | {b[n]['val'] if n in b else '-'}→{x['val']} | {pb['d']:+.1f} | {last(n)} |" if pb else f"| {n} | {x['grade']} | {GROUP.get(n,'-')} | {eff} | - | - | {last(n)} |")
    print()
