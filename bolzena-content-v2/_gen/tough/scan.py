# 강인도 피해(k:tough)를 쓰는 곳을 모두 찾는다 — 사도 카드 · 신탁 · 축복 · 패시브 · 키워드 · 고학년 · 장비 · 교주 카드
import json,glob,sys
ROOT='C:/projects/bolzena-content-v2'
def walk(o,path,out):
    if isinstance(o,dict):
        if o.get('k')=='tough': out.append((path,o))
        for k,v in o.items(): walk(v,path+[k],out)
    elif isinstance(o,list):
        for i,v in enumerate(o): walk(v,path+[i],out)
rows=[]
for f in sorted(glob.glob(ROOT+'/heroes/*/*.json')+glob.glob(ROOT+'/world/**/*.json',recursive=True)):
    d=json.load(open(f,encoding='utf-8'))
    out=[]; walk(d,[],out)
    for p,o in out:
        # 위치 이름
        cur=d; ctx=[]
        card=None
        for k in p:
            cur=cur[k]
            if isinstance(cur,dict) and 'id' in cur and ('cost' in cur or 'fx' in cur or 'name' in cur) and card is None and isinstance(k,int) and p[0] in ('cards','equips','neutral','cards2'):
                card=cur
        where='/'.join(str(x) for x in p[:-1])
        rows.append((f.replace(ROOT+'/',''), where, o.get('v'), o.get('target'), card.get('id') if card else None, card.get('cost') if card else None, card.get('type') if card else None))
for r in rows: print('\t'.join(str(x) for x in r))
print(len(rows), file=sys.stderr)
