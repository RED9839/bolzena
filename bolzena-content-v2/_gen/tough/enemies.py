# 적 강인도(tough) 다시 — 2026-10-05 새 단위(약점 공격 AP 1 = 1, 아니면 1/3). 기준은 bolzena-core/Docs/데이터.md 「강인도 기준」.
#   일반 3(작음/날렵/소환물/영혼도 — 사용자 규칙: 모든 적 최소 3) · 단단한 몸 4
#   엘리트 몸 6 · 날렵/영혼 5 · 단단한 몸 7~8   (엘리트 싸움에 같이 선 여린 적은 엔진이 +1)
#   보스 1층 10 · 단단 12 / 2층 13 · 단단 14   (둘이 함께 서는 보스는 나눠 합 12)
# w/build.js 로 마을을 다시 쓰면 이 값이 옛 것으로 돌아간다 — 그 뒤 이 스크립트를 다시 돌린다. python _gen/tough/enemies.py [--dry]
import json,glob,io,sys,os
ROOT='C:/projects/bolzena-content-v2/world/villages'
DRY='--dry' in sys.argv

PREFIX=[  # (앞부분, 칸, 컨셉) — 앞의 것이 먼저
 ('nururingtanker_',4,'단단(탱커)'),('nururingwarrior_',3,'보통'),('nururingarcher_',3,'여림(마법사)'),('nururingsupporter_',3,'보통'),
 ('hatchling_',3,'작음(새끼 용)'),('imoogi_',3,'보통'),('proteindragon_',4,'단단(근육)'),('golem_',4,'단단(돌)'),
 ('fairymoblongrange_',3,'작음(요정)'),('fairymobcloserange_',3,'보통'),('magicfork_',3,'보통'),('ginseng_',3,'보통'),('lupalu_',3,'보통'),
 ('buseuleogi_',3,'작음(부스러기)'),('mogmaekim_',3,'보통'),('marshmallowtanker_',4,'단단(탱커)'),('marshmallowdealer_',3,'보통'),('marshmallowsupporter_',3,'보통'),
 ('furrywarriorcloserange_',3,'보통'),('furrywarriorlongrange_',3,'날렵(궁수)'),('oldtree_',4,'단단(나무)'),('gluttonbear_',4,'단단(곰)'),
 ('nependers_',3,'보통'),('foodscavenger_',3,'보통'),('furring_',3,'보통'),('pumpkin_',3,'보통'),('blanketghost_',3,'영혼(이불령)'),
 ('shadyfollowercloserange_',3,'보통'),('shadyfollowerlongrange_',3,'날렵(원거리)'),('elfsoldiercloserange_',3,'보통'),('elfsoldierlongrange_',3,'날렵(명사수)'),
 ('drones_',3,'소환물(작은 드론)'),('droneg_',4,'단단(큰 드론)'),('wisps_',3,'영혼(위스프)'),
]
EXACT={
 # 일반 — 예외
 'pumpkin_jolly':(3,'영혼(soul) · 최소치'),'shadyfollowercloserange_naive':(3,'소환물(극성팬) · 최소치'),
 # 엘리트 몸
 'hatchling_cool':(5,'엘리트 · 날렵(유파 창시자 · 받는 강인도 -20%)'),'imoogi_jolly':(6,'엘리트'),'golem_cool_elite':(8,'엘리트 · 단단(수호 골렘)'),
 'magicfork_mad':(6,'엘리트'),'ginseng_mad':(6,'엘리트'),'ginseng_cool':(6,'엘리트(받는 강인도 -20%)'),'goldring_gloomy':(7,'엘리트 · 단단(금고)'),'goldring_jolly':(7,'엘리트 · 단단(금고)'),
 'gluttonbear_cool':(7,'엘리트 · 단단(두목 곰)'),'furrywarriorcloserange_cool':(6,'엘리트(받는 강인도 -20%)'),'foodscavenger_cool':(6,'엘리트'),'furring_mad_elite':(6,'엘리트'),
 'pumpkin_gloomy':(7,'엘리트 · 단단(결정화)'),'blanketghost_cool':(5,'엘리트 · 영혼'),'blanketghost_cool_elite':(5,'엘리트 · 영혼'),'shadyfollowercloserange_cool':(6,'엘리트'),
 'droneg_mad':(6,'엘리트(받는 강인도 -20%)'),'elfsoldiercloserange_cool_elite':(6,'엘리트 · 현장 반장(노동반)'),'droneg_cool':(7,'엘리트 · 단단(시설 경비)'),'elfsoldiercloserange_honor':(6,'엘리트'),
 'oldtree_mad':(7,'엘리트 · 단단(나무)'),'wisps_cool':(5,'엘리트 · 영혼(원소 핵 · 받는 강인도 -20%)'),'pumpkin_cool_elite':(8,'엘리트 · 단단(만년설 껍질)'),
 # 보스(클론)
 'clone_rude':(12,'1층 보스 · 단단'),'clone_carrot':(10,'1층 보스'),'clone_beni':(7,'1층 보스(둘 중 하나 · 합 12)'),'clone_rufo':(5,'1층 보스(둘 중 하나 · 합 12)'),
 'clone_spiky':(10,'1층 보스'),'clone_canna':(10,'1층 보스'),'clone_ifrit':(10,'1층 보스'),
 'clone_daya':(14,'2층 보스 · 단단(다이아)'),'clone_erpin':(13,'2층 보스'),'clone_tig':(14,'2층 보스 · 단단(홀로 선다)'),'clone_shady':(13,'2층 보스'),
 'clone_elena':(13,'2층 보스'),'clone_sylla':(13,'2층 보스'),
}
def pick(e):
    if e['id'] in EXACT: return EXACT[e['id']]
    for p,t,c in PREFIX:
        if e['id'].startswith(p): return (t,c)
    raise SystemExit('기준 없음: '+e['id'])

CLONE={'clone_rude':'루드','clone_daya':'다야','clone_carrot':'캬롯','clone_erpin':'에르핀','clone_beni':'베니','clone_rufo':'루포','clone_tig':'티그',
       'clone_spiky':'스피키','clone_shady':'셰이디','clone_canna':'칸나','clone_elena':'엘레나','clone_ifrit':'이프리트','clone_sylla':'실라'}
SUMMONS=[]
rows=[]
for f in sorted(glob.glob(ROOT+'/[a-z]*.json')):
    raw=io.open(f,encoding='utf-8').read(); d=json.loads(raw); ch=False
    for e in d['enemies']:
        t,c=pick(e)
        rows.append((os.path.basename(f)[:-5], e['id'], e.get('name'), e.get('hp'), e.get('tough'), t, c))
        if e.get('tough')!=t:
            # tough 칸은 weak 다음 · boss 앞 자리(w/build.js ORDER)
            items=list(e.items()); e.clear()
            placed=False
            for k,v in items:
                if k=='tough': continue
                if not placed and k in ('toughTaken','boss','pick','rush','soul','tied','blurb','open','intents','phase','phase2','counters','passives','rare'):
                    e['tough']=t; placed=True
                e[k]=v
            if not placed: e['tough']=t
            ch=True
        # 사도 클론 — 판의 적 속성에 안 맞춘다(사용자): clone = 사도 키(이름 「X (클론)」 의 X), nature 다음 자리
        if e['id'].startswith('clone_') and e.get('clone')!=CLONE[e['id']]:
            items=list(e.items()); e.clear()
            for k,v in items:
                if k=='clone': continue
                e[k]=v
                if k=='nature': e['clone']=CLONE[e['id']]
            if 'clone' not in e: e['clone']=CLONE[e['id']]
            ch=True
        # 보스가 세우는 소환물은 강인도 없음(사용자 — 최소치 3 의 예외): 보스의 수 summon 에 noTough
        if e.get('boss'):
            def walk(o):
                global ch
                if isinstance(o,dict):
                    if o.get('t')=='summon' and not o.get('noTough'): o['noTough']=True; ch=True; SUMMONS.append((e['id'],o.get('id')))
                    for v in o.values(): walk(v)
                elif isinstance(o,list):
                    for v in o: walk(v)
            walk(e)
    if ch and not DRY:
        lines=raw.splitlines(); ind=(len(lines[1])-len(lines[1].lstrip(' '))) if len(lines)>1 else 2
        io.open(f,'w',encoding='utf-8',newline='\n').write(json.dumps(d,ensure_ascii=False,indent=ind or 2)+'\n')
assert all(r[5]>=3 for r in rows), '최소치 3'
for r in rows: print('\t'.join(str(x) for x in r))
