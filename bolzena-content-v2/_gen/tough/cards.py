# 강인도 덤(k:tough) 리뉴얼 — 2026-10-05 새 강인도 단위(약점 공격 AP 1 = 1, 아니면 1/3)에 맞춰.
# 원칙: 덤은 그 카드(사도)의 「격파」 개성일 때만. 단일 1 · 조건/소비/큰 한 방 2 · 광역은 그 절반 · 고학년 단일 2 / 광역 1.
# 다시 돌려도 같은 결과(값을 덮어쓴다). python _gen/tough/cards.py [--dry]
import json,glob,sys,io
ROOT='C:/projects/bolzena-content-v2'
DRY='--dry' in sys.argv
log=[]

def tfx(fx): return [f for f in (fx or []) if f.get('k')=='tough']

def set_all(fxlist, v, where, only=None):
    for f in tfx(fxlist):
        if only and not only(f): continue
        if f.get('v')!=v: log.append((where, f.get('v'), v)); f['v']=v

def drop(holder, where):
    fx=holder.get('fx') or []
    n=[f for f in fx if f.get('k')!='tough']
    if len(n)!=len(fx): log.append((where, [f.get('v') for f in tfx(fx)][0], '뺌')); holder['fx']=n

area=lambda f: f.get('target')=='allEnemies'
single=lambda f: f.get('target') in (None,'oneEnemy')

# 카드 id → (base 와 신탁 모두에 할 일)
def card_rule(c):
    cid=c['id']; parts=[('기본',c)]+[(f"신탁{i+1}「{o.get('name')}」",o) for i,o in enumerate(c.get('oracles') or [])]
    for nm,h in parts:
        w=f"{cid} {nm}"
        if cid in ('베니_u2','티그_u3'): drop(h,w)                                  # 격파 개성이 아니다 — 뺀다
        elif cid in ('델리아_u1','디아나_왕년_u4','란_u2','란_u4','루포_u3','리온_u3','쵸피_u1','티그_영웅_u2'):
            set_all(h.get('fx'),1,w,single)                                          # 격파 사도의 단일 덤 0.5 → 1
        elif cid=='루포_u1':
            fx=h.get('fx') or []
            # 조건 없는 앞의 0.5 → 1 (계획 조건 뒤의 1 은 그대로)
            for f in fx:
                if f.get('k')=='tough':
                    if f.get('v')==0.5: log.append((w,0.5,1)); f['v']=1
                    break
        elif cid=='아야_u2': set_all(h.get('fx'),0.5,w,area)
        elif cid=='라이카_u2': set_all(h.get('fx'),1.5 if h.get('cost')==3 else 0.5,w,area)   # 풀 스로틀(3코) 은 큰 한 방
        elif cid=='리츠_u3': set_all(h.get('fx'),0.5,w,area)
        elif cid=='네르_빡침_u2': set_all(h.get('fx'),1,w,area)
        elif cid=='n_surprise':
            v={'기본':1}.get(nm, None)
            o=h.get('name'); m={'두 겹 상자':2,'리본을 풀면':1,'또 상자':1,'상자 속 상자':1.5,'작은 상자':0.5}
            set_all(h.get('fx'),1 if nm=='기본' else m[o],w,area)
        elif cid=='n_strongman':
            o=h.get('name'); m={'기왓장 열 장':2,'쇠사슬 끊기':1,'관객 호응':1,'맨손 격파':0.5,'앙코르':1}
            set_all(h.get('fx'),1 if nm=='기본' else m[o],w)

ULT={'디아나_왕년':(1,area),'티그_영웅':(1,area),'마고':(2,single),'사리':(2,single)}

files=sorted(glob.glob(ROOT+'/heroes/*/*.json'))+[ROOT+'/world/neutral/교주카드.json']
for f in files:
    raw=io.open(f,encoding='utf-8').read(); d=json.loads(raw)
    before=len(log)
    for h in d.get('heroes',[]) or []:
        if h['id'] in ULT and h.get('ult'):
            v,ok=ULT[h['id']]; set_all(h['ult'].get('fx'),v,f"{h['id']} 고학년「{h['ult'].get('name')}」",ok)
    for c in d.get('cards',[]) or []: card_rule(c)
    if len(log)!=before and not DRY:
        lines=raw.splitlines(); ind=(len(lines[1])-len(lines[1].lstrip(' '))) if len(lines)>1 else 2   # 원래 들여쓰기 그대로
        io.open(f,'w',encoding='utf-8',newline='\n').write(json.dumps(d,ensure_ascii=False,indent=ind or 2)+'\n')
for w,a,b in log: print(f"{w}\t{a}\t→ {b}")
print(len(log),'곳', '(dry)' if DRY else '', file=sys.stderr)
