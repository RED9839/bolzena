import os as _os, sys as _sys
if not _os.environ.get("BZ_OLD_GEN"): _sys.exit("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1")
import json,glob,sys,copy,collections
ROOT='C:/projects/bolzena-content-v2/'
NUM={'dmg','shield','heal','extra','drain'}
def files():
    out=[]
    for f in glob.glob(ROOT+'heroes/**/*.json',recursive=True):
        d=json.load(open(f,encoding='utf-8'))
        if any(h.get('star') in (1,2) and h['id']!='마리' for h in d.get('heroes',[])): out.append(f)
    out.append(ROOT+'world/neutral/교주카드.json'); return out
def S(fx):
    t=0.0
    def w(o):
        nonlocal t
        if isinstance(o,dict):
            if o.get('k') in NUM and isinstance(o.get('ratio'),(int,float)): t+=o['ratio']*(o.get('hits',1) if o['k']=='dmg' else 1)
            for v in o.values(): w(v)
        elif isinstance(o,list):
            for x in o: w(x)
    w(fx); return t
def sig(fx):
    def w(o):
        if isinstance(o,dict): return {k:w(v) for k,v in o.items() if not (o.get('k') in NUM and k in ('ratio','hits'))}
        if isinstance(o,list): return [w(x) for x in o]
        return o
    return json.dumps(w(fx),sort_keys=True,ensure_ascii=False)
def scale(fx,k):
    def w(o):
        if isinstance(o,dict):
            if o.get('k') in NUM and isinstance(o.get('ratio'),(int,float)): o['ratio']=round(o['ratio']*k,2)
            for v in o.values(): w(v)
        elif isinstance(o,list):
            for x in o: w(x)
    w(fx)
rows=[]; cnt=collections.Counter()
for f in files():
    raw=open(f,'rb').read().decode('utf-8'); d=json.loads(raw); changed=False
    for c in d.get('cards',[]):
        if c.get('x') or c.get('X') or not isinstance(c.get('cost'),int): continue
        sb=S(c.get('fx'))
        if sb<=0: continue
        for i,o in enumerate(c.get('oracles') or []):
            fx=o.get('fx',c.get('fx')); so=S(fx)
            if so<=0: continue
            dc=o.get('cost',c['cost'])-c['cost']; r=so/sb; same=sig(fx)==sig(c.get('fx')) and all(json.dumps(o.get(k) or None,sort_keys=True)==json.dumps(c.get(k) or None,sort_keys=True) for k in o if k not in ('name','fx','cost'))
            tgt=None
            if dc==0 and same: kind='수치만'; tgt=1.5
            elif dc==0: kind='갈래·덤'; tgt=1.33 if r<1.0 else None
            elif dc==-1: kind='비용-1'; tgt=1.0
            elif dc<=-2: kind='비용-2'; tgt=0.85
            elif dc==1: kind='비용+1'; tgt=1.5
            else: kind=f'비용{dc:+d}'
            if tgt is None or r>=tgt-0.02: cnt[(kind,'그대로')]+=1; rows.append((c['id'],i+1,kind,r,r)); continue
            nfx=copy.deepcopy(fx); scale(nfx,tgt/r); o['fx']=nfx; r2=S(nfx)/sb
            rows.append((c['id'],i+1,kind,r,r2)); cnt[(kind,'바꿈')]+=1; changed=True
    if changed and '--write' in sys.argv:
        crlf='\r\n' in raw; ind=1 if raw.startswith('{\r\n "') or raw.startswith('{\n "') else 2
        out=json.dumps(d,ensure_ascii=False,indent=ind)
        if crlf: out=out.replace('\n','\r\n')
        if raw.endswith('\n'): out+=('\r\n' if crlf else '\n')
        open(f,'wb').write(out.encode('utf-8'))
print(sorted(cnt.items()))
by=collections.defaultdict(list)
for _,_,k,a,b in rows: by[k].append((a,b))
for k,v in by.items(): print(k,len(v),'전 평균 ×%.2f → 뒤 ×%.2f'%(sum(a for a,_ in v)/len(v),sum(b for _,b in v)/len(v)))
if '--table' in sys.argv:
    for r in rows:
        if abs(r[3]-r[4])>=0.005: print('| %s | 신탁%d | %s | ×%.2f | ×%.2f |'%r)
