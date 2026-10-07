# 적 전면 리워크(2026-10-06 사용자) — 원작(한국 서버) 몬스터만 · 성격 갈래 없이 몬스터마다 일반 하나(+ 엘리트 하나).
#   출처: 나무위키 「트릭컬 리바이브/몬스터」(C:/projects/볼제나/.omc/research/namu/_몬스터.txt, 2026-09-23 판) · 추출 스파인(_ref/적스파인_목록.md)
#   성격은 판의 적 속성이 정한다(스킨도 그 성격 — 유니티 Look.EnemyAs). 데이터의 nature 는 도감 기본 그림의 성격일 뿐.
#   강인도: 일반 ≥ 3 · 엘리트 ≥ 5 · 보스 몸 7(보스가 세우는 소환물은 강인도 없음 — 수 summon 의 noTough). 회복 스킬은 「강인도 회복 스킬」 표에서만.
#   보스 칸은 사도 클론 하나뿐(졸개 없음). 클론은 판마다 그 판 적 속성 · 층 성급으로 엔진이 고른다(Run.PickBosses).
#   옛 디자인은 _backup/foe_rework_20261006/villages 에서 몬스터 컨셉(도감 특징)에 맞는 것을 골라 바탕으로 썼다.
# 쓰기: python _gen/foes/build.py [--dry]   — 마을 JSON 의 enemies · floors(pools · elites · boss) 필드와 _그림.json 을 다시 쓴다.
#   _gen/w/build.js · _gen/tough/enemies.py 는 옛 틀이다(돌리면 이 결과가 지워진다).
import json, copy, sys, os, collections

ROOT = 'C:/projects/bolzena-content-v2/world/villages'
OLD = 'C:/projects/_backup/foe_rework_20261006/villages'
DRY = '--dry' in sys.argv
NAT_EN = {'순수': 'naive', '광기': 'mad', '활발': 'jolly', '우울': 'gloomy', '냉정': 'cool'}

old = {}
for f in os.listdir(OLD):
    if f.startswith('_') or not f.endswith('.json'): continue
    for e in json.load(open(os.path.join(OLD, f), encoding='utf-8'))['enemies']:
        old[e['id']] = e
oldart = json.load(open(os.path.join(OLD, '_그림.json'), encoding='utf-8'))

E = {}      # 새 적 id → 정의
ART = {}    # 새 적 id → 그림
META = {}   # 새 적 id → (몬스터 이름, 종족, 등급, 컨셉, 출처)
NAMU = '나무위키 몬스터 문서'

def mk(nid, src=None, *, mon, race, grade, concept, sec, spine, nat, skin=None, icon=None, **over):
    e = copy.deepcopy(old[src]) if src else {}
    e.pop('blurb', None)
    e['id'] = nid
    for k, v in over.items():
        if v is None: e.pop(k, None)
        else: e[k] = v
    if 'nature' not in over and spine not in NURU: e['nature'] = nat
    # 키 차례를 보기 좋게
    order = ['id', 'name', 'hp', 'row', 'nature', 'weak', 'tough', 'pick', 'rush', 'tied', 'blurb', 'open', 'intents', 'phase', 'phase2', 'counters', 'passives', 'rare']
    E[nid] = {k: e[k] for k in order if k in e} | {k: v for k, v in e.items() if k not in order}
    if spine in NURU:
        ART[nid] = {'spine': 'monsterspine/' + spine, 'skin': skin, 'icon': icon}
    else:
        ART[nid] = {'spine': 'monsterspine/' + spine, 'skin': NAT_EN[nat], 'icon': icon or f'icon_{spine}{NAT_EN[nat]}'}
    ART[nid]['race'] = race   # 몬스터 종족(없음 · 요정 …) — 적 도감이 종족 / 종족 없음으로 묶는다(화면용 표라 엔진은 안 본다)
    META[nid] = (mon, race, grade, concept, f'{NAMU} §{sec} · 스파인 {spine}')
    return E[nid]

NURU = {'nururingtanker', 'nururingwarrior', 'nururingarcher', 'nururingsupporter'}

def say(e, mapping):
    """수 · 패시브 이름 바꾸기(성격 갈래의 말투를 몬스터 하나의 말투로)."""
    def walk(o):
        if isinstance(o, dict):
            if 'say' in o and o['say'] in mapping: o['say'] = mapping[o['say']]
            if 'name' in o and o.get('name') in mapping and 'do' in o: o['name'] = mapping[o['name']]
            for v in o.values(): walk(v)
        elif isinstance(o, list):
            for v in o: walk(v)
    walk(e)

# ═══════════════════════ 요정 — 에르피엔 ═══════════════════════
mk('fairymobcloserange', 'fairymobcloserange_naive', mon='저혈당 요정', race='요정', grade='일반', sec=10, spine='fairymobcloserange', nat='순수',
   concept='높은 공격력 · 낮은 HP — 허기(맞으면 빠지는 피해 증가) · 어지럼 카드', name='저혈당 요정', hp=360,
   blurb='당이 떨어져 눈이 돌아간 요정 — 「허기」 하나당 피해가 오르고 맞을 때마다 하나씩 빠집니다(턴 시작에 셋). 잘게 여러 번 쳐서 허기를 뺀 뒤 크게.')
mk('fairymobcloserange_elite', 'fairymobcloserange_naive', mon='저혈당 요정', race='요정', grade='엘리트', sec=10, spine='fairymobcloserange', nat='광기',
   concept='엘리트 · 토핑만 먹는 요정 — 허기 넷 · 토핑 폭식(회복)', name='저혈당 요정 · 토핑 폭식', hp=620, tough=5,
   intents=[{'t': 'attack', 'v': 120, 'say': '크림 범벅 주먹'}, {'t': 'multi', 'v': 45, 'n': 3, 'say': '잼 난사'},
            {'t': 'addCard', 'id': 'st_dizzy', 'n': 2, 'to': 'draw', 'say': '어지럼 옮기기'}, {'t': 'heal', 'v': 120, 'say': '토핑 폭식'}],
   counters=[{'name': '허기', 'desc': '당이 떨어져 사나움', 'start': 4, 'max': 4, 'dealt': 0.15, 'onHit': -1, 'resetTurnStart': True}],
   rare=[{'id': 'costUp'}],
   blurb='엘리트 · 빵은 버리고 토핑만 먹는 요정 — 「허기」 넷(하나당 피해 +15%, 맞으면 하나씩 빠짐). 토핑을 퍼먹어 회복합니다. 희귀종: 턴 시작에 손 2장 비용 +1.')
e = mk('fairymoblongrange', 'fairymoblongrange_jolly', mon='고혈당 요정', race='요정', grade='일반', sec=11, spine='fairymoblongrange', nat='활발',
   concept='높은 공격력 · 낮은 HP(원거리) — 공격을 보면 신나고 그 밖엔 뾰로통(착란)', name='고혈당 요정', hp=280,
   blurb='새 채소 맛에 들뜬 요정 — 공격 카드를 보면 신이 나 손 1장 비용 -1, 다른 카드를 보면 심술로 1장 비용 +1(착란). 공격으로 몰면 덕을 봅니다.')
say(e, {'사탕 던지기': '채소 던지기'})
mk('magicfork', 'magicfork_naive', mon='불효자손', race='없음', grade='일반', sec=2, spine='magicfork', nat='순수',
   concept='높은 공격력 · 낮은 HP — 스킬을 보면 심통(넷이면 내리찍기)', name='불효자손', hp=300,
   blurb='일할 밭을 잃고 방황하는 요정의 마법 괭이 — 공격이 아닌 카드를 낼 때마다 「심통」 이 붙고 넷이면 바로 내리찍습니다. 스킬을 몰아 내기 전에 쓰러뜨리는 편이 좋습니다.')
mk('magicfork_elite', 'magicfork_mad', mon='불효자손', race='없음', grade='엘리트', sec=2, spine='magicfork', nat='광기',
   concept='엘리트 · 근처의 무엇이든 갈아 버리는 괭이 — 맞을수록 폭주', name='불효자손 · 폭주',
   blurb='엘리트 · 근처의 무엇이든 갈아 버리는 괭이 — 맞을 때마다 「폭주」 가 쌓여 다섯이면 바로 세 번 찍습니다. 잘게 여러 번보다 굵게 한 번. 희귀종: 턴 시작에 손 2장 비용 +1.')
mk('ginseng', 'ginseng_gloomy', mon='산사모', race='없음', grade='일반', sec=3, spine='ginseng', nat='우울',
   concept='기절 주의 · 낮은 공격력 — 쓴 즙 · 뿌리 얽기, 쓰러지면 땅속으로 숨은 척(가사)', name='산사모', hp=320,
   intents=[{'t': 'heal', 'v': 70, 'say': '뿌리즙 나누기'}, {'t': 'back', 'v': 60, 'say': '쓴 즙 뿌리기'}, {'t': 'jam', 'v': 1, 'say': '비명 지르기'}],
   blurb='세계수 양분을 먹고 걸어 다니는 산삼 — 비명으로 다음 턴 AP 를 깎고 동료를 회복시킵니다. 쓰러지면 땅속으로 숨은 척하다가(가사) 다른 적이 회복을 쓰면 일어섭니다 — 치유사부터.')
mk('ginseng_elite', 'ginseng_mad', mon='산사모', race='없음', grade='엘리트', sec=3, spine='ginseng', nat='광기',
   concept='엘리트 · 양분 과잉 산삼 — 턴마다 회복 · 디버프를 양분으로', name='산사모 · 웃자람',
   blurb='엘리트 · 너무 오래 자라 양분이 넘치는 산삼 — 턴마다 회복하고 디버프를 받으면 양분으로 회복합니다. 쓰러질 때 쓴 즙(취약 2)을 터뜨립니다. 희귀종: 턴 시작에 손 2장에 독.')
mk('mogmaekim', 'mogmaekim_jolly', mon='목매킴', race='없음', grade='일반', sec=27, spine='mogmaekim', nat='활발',
   concept='물량 · 자폭 주의 — 쓰러지면 펑 터져 고통', name='목매킴', hp=340,
   blurb='잘못 구워진 빵 — 쓰러지면 펑 터져 파티에 고통 2 를 남깁니다. 고통에 대비하고 처치하는 편이 좋습니다.')
e = mk('buseuleogi', 'buseuleogi_mad', mon='부스러기', race='없음', grade='일반', sec=28, spine='buseuleogi', nat='광기',
   concept='물량 주의 · 낮은 HP — 설탕 범벅 · 동료가 쓰러지면 성냄', name='부스러기', hp=240,
   blurb='잘못 구워진 컵케이크 — 쓰러지면 「설탕 범벅」 을 뽑을 더미에 남깁니다. 동료가 쓰러지면 성납니다. 떼로 나오니 한꺼번에 치우는 편이 좋습니다.')
mk('goldring_elite', 'goldring_gloomy', mon='새마음금고', race='없음', grade='엘리트', sec=29, spine='goldring', nat='우울',
   concept='엘리트 · 높은 HP 금고 슬라임 — 공격 두 장째마다 뚜껑(실드)', name='새마음금고',
   intents=[{'t': 'block', 'v': 110, 'say': '자물쇠 걸기'}, {'t': 'multi', 'v': 45, 'n': 3, 'say': '동전 세례'}, {'t': 'guard', 'v': 60, 'say': '금고 문 닫기'}, {'t': 'attack', 'v': 140, 'say': '금괴 내려찍기'}],
   blurb='엘리트 · 요정 왕국 금고를 노린 슬라임 — HP 가 많고, 공격 카드 두 장째마다 뚜껑을 닫아 실드를 얻습니다(턴당 2). 격파하면 열려 취약 2. 희귀종: 전투 시작 결정화 3.')
mk('marshmallowtanker', 'marshmallowtanker_naive', mon='탱탱 멜로', race='없음', grade='일반', sec='31.1', spine='marshmallowtanker', nat='순수',
   concept='회복 · 방어 주의 — 실드가 깨지면 말랑 조각으로 적 전체 실드', name='탱탱 멜로',
   blurb='탱탱한 마시멜로 골렘 — 제 실드가 깨지면 말랑 조각을 흩어 적 전체에 실드를 줍니다. 실드는 한 번에 크게 깨는 편이 좋습니다.')
mk('marshmallowdealer', 'marshmallowdealer_mad', mon='말랑 멜로', race='없음', grade='일반', sec='31.2', spine='marshmallowdealer', nat='광기',
   concept='높은 공격력 · 낮은 HP — 맞을수록 화남(부글부글)', name='말랑 멜로', hp=340,
   blurb='뜯어먹힐까 늘 화가 나 있는 마시멜로 — 맞을 때마다 「부글부글」 이 올라 주는 피해가 커집니다(최대 5). 한 방에 크게.')
mk('marshmallowsupporter', 'marshmallowsupporter_jolly', mon='쫀득 멜로', race='없음', grade='일반', sec='31.3', spine='marshmallowsupporter', nat='활발',
   concept='회복 주의 · 낮은 공격력 — 동료 회복 · 설탕 코팅', name='쫀득 멜로',
   blurb='쫀득한 응원 마시멜로 — 동료를 회복시키고 피해 감소를 두릅니다. 디버프를 받으면 설탕 코팅으로 회복합니다. 먼저 끊는 편이 좋습니다.')

# ═══════════════════════ 수인 — 수인 부락 ═══════════════════════
mk('furrywarriorcloserange', 'furrywarriorcloserange_naive', mon='수인 광전사', race='수인', grade='일반', sec=14, spine='furrywarriorcloserange', nat='순수',
   concept='버프 주의 · 낮은 HP — 맞을수록 투지', name='수인 광전사',
   blurb='훈련에 매진하는 어린 수인 — 맞을 때마다 「투지」 가 붙어 주는 피해가 오릅니다(최대 3). 질질 끌지 않는 편이 좋습니다.')
mk('furrywarriorcloserange_elite', 'furrywarriorcloserange_cool', mon='수인 광전사', race='수인', grade='엘리트', sec=14, spine='furrywarriorcloserange', nat='냉정',
   concept='엘리트 · 실력은 수련생 이상인 만년 수련생 — 받아 막기 · 간파 연격', name='수인 광전사 · 만년 수련생',
   blurb='엘리트 · 졸업을 미룬 만년 수련생 — 맞을 때마다 받아 막습니다(실드 40, 턴당 3). 같은 사도의 카드를 잇달아 내면 바로 연격. 잘게 여러 번보다 큰 한 방, 사도를 번갈아. 희귀종: 받는 강인도 피해 -20%.')
mk('furrywarriorlongrange', 'furrywarriorlongrange_cool', mon='수인 궁수', race='수인', grade='일반', sec=15, spine='furrywarriorlongrange', nat='냉정',
   concept='후열 공격 · 치명타 주의 — 뒷줄 관통 · 겨냥', name='수인 궁수',
   blurb='활을 고른 수인 훈련생 — 뒷줄을 꿰뚫고, 제 차례가 지난 뒤에도 카드를 내면 「겨냥」 이 쌓여 넷이면 바로 관통 화살. 큰 한 발은 격파로 끊깁니다.')
mk('gluttonbear', 'gluttonbear_naive', mon='머곰', race='수인', grade='일반', sec=5, spine='gluttonbear', nat='순수',
   concept='높은 HP · 낮은 공격력 — 허기 · 당기면 음식을 지켜 실드', name='머곰',
   blurb='듬직한 먹보 곰 수인 — 「허기」 하나당 피해가 오르고 맞으면 빠집니다(턴 시작에 둘). 카드로 당기면 음식을 지키려 실드를 두릅니다.')
mk('gluttonbear_elite', 'gluttonbear_cool', mon='머곰', race='수인', grade='엘리트', sec=5, spine='gluttonbear', nat='냉정',
   concept='엘리트 · 머곰을 모아 세력을 꾸리는 두목 — 동료가 쓰러지면 사기 · 분노', name='머곰 · 두목',
   blurb='엘리트 · 다른 머곰들을 모아 세력을 꾸린 두목 — 동료가 쓰러질 때마다 적 전체 사기. 맞을 때마다 「분노」 가 쌓여 여덟이면 바로 전체를 후려칩니다. 한꺼번에 눕히거나 두목부터. 희귀종: 전투 시작 결정화 3.')
mk('foodscavenger', 'foodscavenger_naive', mon='라쿤아치', race='수인', grade='일반', sec=4, spine='foodscavenger', nat='순수',
   concept='높은 공격력 · 낮은 HP — 사료 한 줌(약화 카드)', name='라쿤아치', hp=360,
   blurb='사료에 중독된 라쿤 수인 — 턴 끝에 손에 있으면 약화를 거는 「사료 한 줌」 을 더미에 뿌립니다.')
mk('foodscavenger_elite', 'foodscavenger_cool', mon='라쿤아치', race='수인', grade='엘리트', sec=4, spine='foodscavenger', nat='냉정',
   concept='엘리트 · 비싼 사료만 먹는 미식가 — 카드 셋마다 사료 한 줌 · 안절부절', name='라쿤아치 · 미식가',
   blurb='엘리트 · 비싼 사료만 골라 먹는 미식가 — 파티가 카드를 세 장 낼 때마다 「사료 한 줌」 을 손에 넣습니다(턴당 2). 「안절부절」 을 더미에 넣고, 희귀종: 덱에 든 안절부절 수만큼 타격 +1 — 오래 끌지 않는 편이 좋습니다.')
mk('furring', 'furring_jolly', mon='퍼리', race='수인', grade='일반', sec=8, spine='furring', nat='활발',
   concept='회복 주의 · 낮은 공격력 — 한턱(회복 · 적 전체 사기)', name='퍼리', hp=380,
   blurb='갑자기 재산이 불어난 수인 — 동료를 회복시키고 적 전체 사기를 올립니다. 먼저 끊는 편이 좋습니다.')
mk('furring_elite', 'furring_mad_elite', mon='퍼리', race='수인', grade='엘리트', sec=8, spine='furring', nat='광기',
   concept='엘리트 · 투자 실패 끝의 광기 — 재기 의지 · 절반에서 거대화', name='퍼리 · 몰락한 큰손',
   blurb='엘리트 · 잇따른 투자 실패로 밑바닥에 떨어진 큰손 — 턴마다 「재기 의지」 가 올라 피해가 커지고(최대 3), 절반에서 거대화합니다. 오래 끌면 집니다. 희귀종: 턴 시작에 손 2장 비용 +1.')

# ═══════════════════════ 유령 — 유령 늪 ═══════════════════════
mk('blanketghost', 'blanketghost_jolly', mon='이불령', race='유령', grade='일반', sec=9, spine='blanketghost', nat='활발',
   concept='후열 진입 · 도발 주의 — 뒷줄로 스며들기 · 미끄러움', name='이불령', hp=320,
   blurb='모습을 갖추지 못한 어린 유령 — 뒷줄로 스며들고, 카드를 내면 손 1장이 미끄러져 떨어지는 「미끄러움」 을 겁니다.')
mk('blanketghost_elite', 'blanketghost_cool_elite', mon='이불령', race='유령', grade='엘리트', sec=9, spine='blanketghost', nat='냉정',
   concept='엘리트 · 한물간 장난으로 분위기를 깨는 이불령 — 속삭임 · 안절부절', name='이불령 · 악몽',
   blurb='엘리트 · 오래 살아 분위기 파괴에 도가 튼 이불령 — 턴 시작마다 「오싹한 속삭임」 을 손에, 「안절부절」 을 더미에 넣습니다. 희귀종: 덱의 안절부절 수만큼 타격 +1 — 짧게 끝내는 편이 좋습니다. 쓰러질 때 정신 붕괴 1.')
e = mk('shadyfollowercloserange', 'shadyfollowercloserange_mad', mon='셰이디아 극성팬', race='유령', grade='일반', sec=13, spine='shadyfollowercloserange', nat='광기',
   concept='쓰라림 주의 · 낮은 HP — 응원봉 연타마다 균열', name='셰이디아 극성팬', hp=400,
   blurb='셰이디 팬클럽의 극성 유령 — 응원봉 연타마다 균열을 새깁니다.')
say(e, {'단검봉 연타': '응원봉 연타'})
mk('shadyfollowercloserange_elite', 'shadyfollowercloserange_cool', mon='셰이디아 극성팬', race='유령', grade='엘리트', sec=13, spine='shadyfollowercloserange', nat='냉정',
   concept='엘리트 · 제 팬클럽을 꾸리는 극성팬 — 칠 때마다 충격 · 균열', name='셰이디아 극성팬 · 친위대장',
   blurb='엘리트 · 셰이디를 따라 하며 제 팬클럽을 꾸리는 극성팬 — 칠 때마다 충격을 남깁니다(실드 위로 맞으면 더 아픕니다). 실드에만 기대지 않는 편이 좋습니다. 희귀종: 행동할 때 파티에 취약 2.')
mk('shadyfollowerlongrange', 'shadyfollowerlongrange_mad', mon='셰이디아 극성팬(원거리)', race='유령', grade='일반', sec=13, spine='shadyfollowerlongrange', nat='광기',
   concept='후열 순간이동 · 쓰라림 — 뒷줄 찌르기 · 균열', name='셰이디아 극성팬 · 원거리', hp=330, rush=3,
   blurb='아공간을 넘나드는 극성팬 — 뒷줄로 순간이동해 균열을 새깁니다. 카드 세 장이면 움직이니 서두르는 편이 좋습니다.')
e = mk('pumpkin', 'pumpkin_cool', mon='호바깅', race='없음', grade='일반', sec=12, spine='pumpkin', nat='냉정',
   concept='버프 주의 · 낮은 HP — 적 전체 결의 · 실드가 남으면 사기', name='호바깅', hp=380,
   blurb='유령 늪에서 유독 잘 자라는 호박 — 적 전체 결의를 올리고, 턴이 끝날 때 제 실드가 남아 있으면 사기. 실드를 깨고 넘기는 편이 좋습니다.')
say(e, {'단단한 고랭지': '단단한 호박 껍질', '서리 맞은 껍질': '여무는 껍질'})
mk('pumpkin_elite', 'pumpkin_gloomy', mon='호바깅', race='없음', grade='엘리트', sec=12, spine='pumpkin', nat='우울',
   concept='엘리트 · 음지에서 자란 불쌍한 호박 — 면역 · 회복 · 재 속 부활', name='호바깅 · 불쌍한',
   blurb='엘리트 · 서늘한 음지에서 자라 유령들도 안 괴롭히는 호박 — 디버프를 받으면 면역 1(턴당 1) · 회복 60. 쓰러지면 재 속에서 2턴 뒤 40% 로 돌아옵니다. 희귀종: 전투 시작 결정화 3.')
mk('hatsnail', None, mon='햇팽이', race='없음', grade='일반', sec=25, spine='hatsnail', nat='순수',
   concept='높은 마법 방어 · 낮은 물리 방어 — 모자 속으로 숨기(실드) · 모자 속 쪽지', name='햇팽이', hp=420, row='front', tough=4, pick='shuffle',
   intents=[{'t': 'block', 'v': 90, 'say': '모자 속으로 숨기'}, {'t': 'attack', 'v': 80, 'say': '끈적한 몸통박치기'},
            {'t': 'addCard', 'id': 'st_note', 'n': 1, 'to': 'draw', 'say': '모자 속 쪽지 흘리기'}],
   counters=[{'name': '모자 껍질', 'desc': '모자 속에 숨은 몸', 'taken': -0.15, 'max': 2, 'clearTurnEnd': True}],
   passives=[{'name': '모자 깊이', 'on': 'guardBreak', 'do': {'t': 'count', 'id': '모자 껍질', 'v': 2, 'say': '모자 깊이 숨기'}}],
   blurb='마녀 모자를 집 삼은 달팽이 — 「모자 속 쪽지」 를 더미에 흘리고, 실드가 깨지면 모자 속으로 숨어 이번 턴 받는 피해가 줄어듭니다. 실드는 턴 끝에 깨는 편이 좋습니다.')
mk('hatsnail_elite', None, mon='햇팽이', race='없음', grade='엘리트', sec=25, spine='hatsnail', nat='냉정',
   concept='엘리트 · 마녀의 의식이 옮겨진 듯한 햇팽이 — 쪽지 · 봉쇄 · 디버프를 받으면 실드', name='햇팽이 · 마녀의 의식', hp=660, row='front', tough=6, pick='shuffle',
   intents=[{'t': 'addCard', 'id': 'st_note', 'n': 2, 'to': 'draw', 'say': '주문 쪽지 흘리기'}, {'t': 'cardDebuff', 'id': '봉쇄', 'n': 1, 'to': 'hand', 'say': '마녀의 저주'},
            {'t': 'attack', 'v': 120, 'say': '모자챙 휘두르기'}, {'t': 'multi', 'v': 40, 'n': 3, 'say': '점액 방울'}],
   passives=[{'name': '마녀 모자', 'on': 'debuffed', 'do': {'t': 'block', 'v': 70, 'say': '마녀 모자 눌러쓰기'}}],
   rare=[{'id': 'toughGuard'}],
   blurb='엘리트 · 마녀의 의식이 옮겨진 듯 구는 햇팽이 — 「모자 속 쪽지」 를 둘씩 흘리고 손 1장을 봉쇄합니다. 디버프를 받으면 모자를 눌러써 실드. 디버프보다 피해를. 희귀종: 받는 강인도 피해 -20%.')
mk('curseddoll', None, mon='누루링(인형)', race='없음', grade='일반', sec=21, spine='curseddoll', nat='광기',
   concept='소음 · 마법 면역 주의 — 왁자지껄(소음 카드) · 약화', name='누루링 인형', hp=360, row='back', tough=3, pick='shuffle',
   intents=[{'t': 'addCard', 'id': 'st_chatter', 'n': 2, 'to': 'draw', 'say': '소름 끼치는 소음'}, {'t': 'back', 'v': 80, 'say': '바늘 꽂기'},
            {'t': 'debuff', 'id': '약화', 'v': 1, 'say': '버려진 원한'}],
   passives=[{'name': '다시 꿰매기', 'on': 'lowHp', 'at': 0.4, 'do': {'t': 'heal', 'v': 90, 'say': '다시 꿰매기'}}],
   blurb='마녀의 뒤틀린 마법으로 움직이는 버려진 인형 — 「왁자지껄」 소음을 둘씩 더미에 넣고 약화를 겁니다. HP 가 40% 아래면 한 번 스스로 꿰맵니다.')

# ═══════════════════════ 엘프 — 모나티엄 ═══════════════════════
mk('elfsoldiercloserange', 'elfsoldiercloserange_naive', mon='엘프 돌격병', race='엘프', grade='일반', sec=19, spine='elfsoldiercloserange', nat='순수',
   concept='물리 면역 주의 — 방패 대열(적 전체 결의) · 전우가 쓰러지면 성냄', name='엘프 돌격병',
   blurb='모나티엄의 징병 군인 — 적 전체 결의를 올리고 전우가 쓰러지면 성납니다.')
mk('elfsoldiercloserange_elite', 'elfsoldiercloserange_cool_elite', mon='엘프 돌격병', race='엘프', grade='엘리트', sec=19, spine='elfsoldiercloserange', nat='냉정',
   concept='엘리트 · 노동반(공병) 현장 반장 — 바리케이드 · 실드가 남으면 결의', name='엘프 돌격병 · 현장 반장',
   icon='still_elfsoldiercloserange_cool',
   blurb='엘리트 · 「노동반」 공병의 현장 반장 — 턴이 끝날 때 파티에 실드가 남아 있으면 바리케이드가 두꺼워집니다(적 전체 결의). 실드에만 기대지 않는 편이 좋습니다. 희귀종: 행동할 때 파티에 약화 2.')
mk('elfsoldierlongrange', 'elfsoldierlongrange_gloomy', mon='엘프 명사수', race='엘프', grade='일반', sec=20, spine='elfsoldierlongrange', nat='우울',
   concept='높은 공격력 · 낮은 HP · 직선 공격 — 힘 모아 관통 한 발', name='엘프 명사수',
   blurb='원거리 병과로 뽑힌 엘프 군인 — 한 치 어긋남 없이 힘을 모아 관통 한 발. 모으는 동안 격파하면 끊깁니다.')
mk('elfsoldierlongrange_elite', 'elfsoldierlongrange_cool', mon='엘프 명사수', race='엘프', grade='엘리트', sec=20, spine='elfsoldierlongrange', nat='냉정',
   concept='엘리트 · 노동반 경계조장 — 봉쇄 · 당겨지지 않는 저격 · 관통', name='엘프 명사수 · 경계조장', hp=520, tough=5,
   icon='still_elfsoldierlongrange_cool',
   intents=[{'t': 'back', 'v': 130, 'say': '조준 사격', 'rush': 0}, {'t': 'cardDebuff', 'id': '봉쇄', 'n': 2, 'to': 'draw', 'say': '조준 고정'},
            {'t': 'charge', 'say': '숨 고르기', 'next': {'t': 'back', 'v': 220, 'say': '경계조장의 한 발'}, 'brk': True}, {'t': 'multi', 'v': 40, 'n': 3, 'say': '견제 사격'}],
   rare=[{'id': 'actDebuff', 'st': '취약'}],
   blurb='엘리트 · 작업하는 노동반 곁을 지키는 경계조장 — 뽑을 더미 2장을 「봉쇄」 로 묶고, 큰 한 발은 카드로 당겨지지 않습니다. 모으는 저격은 격파로 끊깁니다. 희귀종: 행동할 때 파티에 취약 2.')
mk('drones', 'drones_gloomy', mon='드론 S형', race='없음', grade='일반', sec='7.1', spine='drones', nat='냉정',
   concept='높은 공격력 · 감전 약점(비행 드론) — 카드 세 장이면 움직임 · 충격', name='드론 S형', hp=300,
   intents=[{'t': 'back', 'v': 70, 'say': '레이저 점사', 'id': '충격', 'n': 1}, {'t': 'attack', 'v': 60, 'say': '부딪히기'}, {'t': 'debuff', 'id': '충격', 'v': 2, 'say': '방전'}],
   passives=[{'name': '비상 경보', 'on': 'lowHp', 'at': 0.5, 'do': {'t': 'debuff', 'id': '충격', 'v': 1, 'say': '비상 경보'}}],
   blurb='엘프들의 비행 감시 드론 — 방전으로 충격을 겁니다. 카드 세 장이면 움직이니 서두르는 편이 좋습니다.')
mk('droneg', 'droneg_naive', mon='드론 G형', race='없음', grade='일반', sec='7.2', spine='droneg', nat='순수',
   concept='넉백 주의(보행 드론) — 방어 프로토콜(적 전체 실드) · 통제 신호', name='드론 G형',
   intents=[{'t': 'guard', 'v': 50, 'say': '방어 프로토콜', 'tough': 1}, {'t': 'attack', 'v': 90, 'say': '돌진'},
            {'t': 'addCard', 'id': 'st_signal', 'n': 1, 'to': 'draw', 'say': '통제 신호'}, {'t': 'jam', 'v': 1, 'say': '밀쳐 내기'}],
   blurb='시민 통제용 보행 드론 — 적 전체 실드를 두르고 「통제 신호」 를 넣으며, 밀쳐 내 다음 턴 AP 를 깎습니다. 동료가 쓰러지면 방어벽을 세웁니다.')
mk('droneg_elite', 'droneg_cool', mon='드론 G형', race='없음', grade='엘리트', sec='7.2', spine='droneg', nat='냉정',
   concept='엘리트 · 관리 시설 상주 경비 — 경보 단계 · 테이저', name='드론 G형 · 시설 경비',
   blurb='엘리트 · 관리가 중요한 시설에 상주하는 경비 드론 — 턴마다 「경보 단계」 가 올라 셋이면 다음 차례에 전체 경보(충격 1). 격파로는 단계가 안 내려갑니다. 희귀종: 전투 시작 결정화 3.')
mk('nependers', 'nependers_gloomy', mon='한입초', race='없음', grade='일반', sec=17, spine='nependers', nat='우울',
   concept='삼키기 주의 · 낮은 공격력 — 입 크게 벌리기 → 꿀꺽(격파로 끊김)', name='한입초', hp=440,
   blurb='힘없는 풀인 척 사냥하는 식물 — 「단단함」 이 쌓여 셋 이상이면 통째로 삼킵니다. 입을 크게 벌린 한 입은 격파로 끊깁니다.')

# ═══════════════════════ 정령 — 정령산 ═══════════════════════
e = mk('wisps', 'wisps_naive', mon='위스프', race='정령', grade='일반', sec=18, spine='wisps', nat='순수',
   concept='화상 · 광역 공격 주의 — 부유(두 번 맞으면 기절) · 스며드는 원소(고통)', name='위스프',
   blurb='순수한 에너지에 가까운 어린 정령 — 「부유」 둘: 한 턴에 두 번 맞으면 떨어져 기절합니다. 스며드는 원소로 고통 2.')
say(e, {'스며드는 불': '스며드는 원소', '불똥': '원소 불똥', '화르륵': '원소 터뜨리기'})
mk('wisps_elite', 'wisps_cool', mon='위스프', race='정령', grade='엘리트', sec=18, spine='wisps', nat='냉정',
   concept='엘리트 · 원소 핵 — 위스프를 부르고 쓰러진 원소를 삼킴', name='위스프 · 원소 핵',
   intents=[{'t': 'summon', 'id': 'wisps', 'n': 1, 'max': 1, 'say': '원소 소환'}, {'t': 'back', 'v': 100, 'say': '원소 광선'},
            {'t': 'debuff', 'id': '고통', 'v': 2, 'say': '원소 폭주'}, {'t': 'multi', 'v': 35, 'n': 3, 'say': '원소 탄'}],
   blurb='엘리트 · 원소가 뭉친 핵 — 위스프를 불러내고(최대 1), 다른 위스프가 쓰러지면 그 원소를 삼켜 회복 · 사기. 핵부터 끄거나 한꺼번에 끄는 편이 좋습니다. 희귀종: 받는 강인도 피해 -20%.')
e = mk('lupalu', 'lupalu_gloomy', mon='루파루', race='정령', grade='일반', sec=6, spine='lupalu', nat='우울',
   concept='높은 공격력 · 낮은 HP — 음료병 연타마다 균열', name='루파루', hp=340,
   blurb='물가에 사는 덩치 큰 정령 — 음료병을 휘두를 때마다 균열을 새깁니다.')
say(e, {'깨진 병 휘두르기': '음료병 휘두르기', '유리 조각': '물 뿜기'})
mk('lupalu_elite', 'lupalu_gloomy', mon='루파루', race='정령', grade='엘리트', sec=6, spine='lupalu', nat='냉정',
   concept='엘리트 · 정령산 정상의 루파루 — 멈추지 않는 몸(행동할수록 사기) · 빙결', name='루파루 · 산정', hp=600, tough=5, pick='shuffle',
   intents=[{'t': 'multi', 'v': 45, 'n': 3, 'say': '언 음료병 휘두르기', 'id': '균열', 'per': 1}, {'t': 'cardDebuff', 'id': '빙결', 'n': 2, 'to': 'draw', 'say': '얼음 숨결'},
            {'t': 'back', 'v': 120, 'say': '얼음 물 뿜기'}, {'t': 'attack', 'v': 110, 'say': '꼬리 후리기'}],
   counters=[{'name': '쉬지 않기', 'desc': '얼지 않으려 계속 움직임', 'onTurnStart': 1, 'dealt': 0.1, 'max': 4}],
   passives=[{'name': '얼어붙음', 'on': 'broken', 'do': {'t': 'count', 'id': '쉬지 않기', 'v': -4, 'say': '멈춰서 얼어붙음'}}],
   blurb='엘리트 · 정령산 정상 추위에서 얼지 않으려 쉬지 않는 루파루 — 턴마다 「쉬지 않기」 가 올라 피해가 커지고(최대 4), 격파하면 멈춰 얼어붙어 다 빠집니다. 뽑을 더미 2장을 빙결시킵니다.')
mk('oldtree', 'oldtree_naive', mon='고모구지', race='정령', grade='일반', sec=16, spine='oldtree', nat='순수',
   concept='광역 공격 주의 — 첫 턴 기습 · 나무껍질 · 가지 휩쓸기', name='고모구지', hp=420,
   intents=[{'t': 'count', 'id': '나무껍질', 'v': 2, 'say': '껍질 굳히기'}, {'t': 'attackAll', 'v': 60, 'say': '가지 휩쓸기'}, {'t': 'attack', 'v': 90, 'say': '가지 휘두르기'}],
   blurb='겉모습만 보고 지나치는 침입자를 기습하는 영악한 정령 — 첫 턴에 뒷줄을 기습하고, 가지로 파티 전체를 휩쓸며, 「나무껍질」 을 두르면 받는 피해가 줄어듭니다(턴 끝에 풀림).')
mk('oldtree_elite', 'oldtree_mad', mon='고모구지', race='정령', grade='엘리트', sec=16, spine='oldtree', nat='광기',
   concept='엘리트 · 열매 맺기 직전 — 디버프를 받으면 사기 · 마구잡이 공격', name='고모구지 · 열매 직전',
   intents=[{'t': 'attack', 'v': 110, 'say': '열매 가지'}, {'t': 'attackAll', 'v': 80, 'say': '가지 난타'}, {'t': 'block', 'v': 100, 'say': '껍질 두르기'}, {'t': 'attack', 'v': 130, 'say': '뿌리째 휘두르기'}],
   blurb='엘리트 · 열매가 맺히기 직전이라 다가오는 모두를 마구 치는 고목 — 디버프를 받으면 사기(턴당 1). 디버프를 쌓기보다 바로 때리는 편이 좋습니다. 희귀종: 턴 시작에 손 2장 비용 +1.')

# ═══════════════════════ 용족 — 용족 동굴 ═══════════════════════
mk('hatchling', 'hatchling_mad', mon='목도룡', race='용족', grade='일반', sec=22, spine='hatchling', nat='광기',
   concept='후열 공격 · 치명타 주의 — 뒷줄 노리기 · 격파에서 일어서면 사기', name='목도룡', hp=360,
   intents=[{'t': 'back', 'v': 100, 'say': '목도리 불꽃'}, {'t': 'back', 'v': 70, 'say': '뒷줄 할퀴기'}, {'t': 'block', 'v': 60, 'say': '웅크리기'}],
   blurb='용족에서 밀려나 짐승처럼 변했다는 소문의 괴물 — 뒷줄만 노립니다. 격파에서 일어서면 옛 영광을 떠올려 사기.')
mk('hatchling_pupil', 'hatchling_jolly', mon='목도룡', race='용족', grade='소환물', sec=22, spine='hatchling', nat='활발',
   concept='유파 창시자가 부르는 제자(스승이 쓰러지면 흩어짐) — 두리번 셋이면 뒷줄 강타', name='목도룡 · 제자',
   blurb='유파 창시자가 불러 모은 목도룡 제자 — 창시자가 쓰러지면 흩어집니다. 턴마다 「두리번」 이 올라 셋이면 다음 차례에 뒷줄을 크게 노립니다.')
mk('hatchling_elite', 'hatchling_cool', mon='목도룡', race='용족', grade='엘리트', sec=22, spine='hatchling', nat='냉정',
   concept='엘리트 · 새 유파를 세우는 창시자 — 제자 모집 · 제자의 원수(사기)', name='목도룡 · 유파 창시자',
   intents=[{'t': 'summon', 'id': 'hatchling_pupil', 'n': 1, 'max': 1, 'say': '제자 모집'}, {'t': 'back', 'v': 110, 'say': '창시자의 불'},
            {'t': 'multi', 'v': 45, 'n': 3, 'say': '유파 연격'}, {'t': 'buff', 'id': '불굴', 'v': 1, 'say': '유파의 가르침', 'all': True}],
   blurb='엘리트 · 새 용족 유파를 세우려는 목도룡 — 제자를 불러 모으고(최대 1), 제자가 쓰러질 때마다 적 전체 사기. 스승부터 쓰러뜨리면 부른 제자도 흩어집니다. 희귀종: 받는 강인도 피해 -20%.')
mk('imoogi', 'imoogi_naive', mon='길어용', race='용족', grade='일반', sec=26, spine='imoogi', nat='순수',
   concept='직선 공격 · 낮은 HP — 실드를 건너 꿰뚫는 돌진', name='길어용', hp=380,
   blurb='완전한 모습을 갖추지 못한 아룡 — 몸을 곧게 뻗어 실드를 건너 꿰뚫습니다.')
mk('imoogi_elite', 'imoogi_jolly', mon='길어용', race='용족', grade='엘리트', sec=26, spine='imoogi', nat='활발',
   concept='엘리트 · 궁극의 준비 상태 — 실드 보존 · 모은 힘 터뜨리기', name='길어용 · 궁극의 준비',
   blurb='엘리트 · 진짜 용족으로 인정받으려 「궁극의 준비 상태」 가 된 아룡 — 첫 턴 두꺼운 실드를 「실드 보존」 으로 넘기고, 턴이 끝날 때 실드가 남아 있으면 사기. 모은 힘을 터뜨립니다. 실드를 깨고 넘기는 편이 좋습니다. 희귀종: 전투 시작 결정화 3.')
mk('proteindragon', 'proteindragon_gloomy', mon='근육인데용', race='용족', grade='일반', sec=24, spine='proteindragon', nat='우울',
   concept='회복 주의 · 낮은 공격력 — 턴 끝마다 스스로 회복 · 프로틴', name='근육인데용',
   blurb='힘을 좇아 몸만 키운 뒤틀린 용 — 턴 끝마다 스스로 회복 50, 프로틴 셰이크로 동료도 회복시킵니다. 느린 지속 피해보다 몰아 치기.')
mk('proteindragon_elite', 'proteindragon_mad', mon='근육인데용', race='용족', grade='엘리트', sec=24, spine='proteindragon', nat='광기',
   concept='엘리트 · 남을 훈련시키는 조교 — 적 전체 사기 · 같은 사도 연속이면 자세 불량', name='근육인데용 · 조교', hp=640, tough=6,
   intents=[{'t': 'buff', 'id': '사기', 'v': 1, 'say': '하나 더!', 'all': True}, {'t': 'attack', 'v': 130, 'say': '조교의 주먹'},
            {'t': 'block', 'v': 100, 'say': '시범 자세'}, {'t': 'heal', 'v': 100, 'say': '프로틴 지급'}],
   rare=[{'id': 'crystal'}],
   blurb='엘리트 · 주변을 거칠게 훈련시키는 조교 근육인데용 — 적 전체 사기를 올리고 프로틴으로 동료를 회복시킵니다. 같은 사도의 카드를 잇달아 내면 「자세 불량!」 으로 바로 칩니다. 희귀종: 전투 시작 결정화 3.')
mk('golem', 'golem_naive', mon='동석', race='없음', grade='일반', sec=23, spine='golem', nat='순수',
   concept='높은 물리 방어 — 불순물 껍질(받는 피해 1) · 무거운 발걸음', name='동석', hp=500,
   blurb='용족이 동굴을 지키려 보석을 뭉쳐 만든 골렘 — 「불순물 껍질」 둘이 남은 동안 받는 피해가 1입니다(맞으면 하나씩 깨짐). 다시 덧씌우기 전에 벗기고 크게.')
mk('golem_elite', 'golem_cool_elite', mon='동석', race='없음', grade='엘리트', sec=23, spine='golem', nat='냉정',
   concept='엘리트 · 다야가 직접 만든 수호 골렘 — 수호 결(받는 피해 감소) · 격파하면 취약 3', name='동석 · 수호 골렘',
   blurb='엘리트 · 다야가 직접 만든 수호 골렘 — 강인도 8. 턴마다 「수호 결」 이 쌓여 받는 피해가 줄고(최대 5), 격파하면 결이 깨지며 취약 3. 강인도 피해를 넣는 손을 시험합니다. 희귀종: 전투 시작 결정화 3.')
# 크레용족 — 크레용 차원에서 넘어온 존재(옛 설정은 용족의 아종). 종족 없음, 용족 동굴(도화지의 신전)에 둔다
mk('crayonwarrior', None, mon='칠레용', race='없음', grade='일반', sec='30.1', spine='crayonwarrior', nat='광기',
   concept='높은 공격력 · 낮은 HP — 돌진해 색칠하기(카드 세 장이면 움직임)', name='칠레용', hp=330, row='front', tough=3, pick='shuffle', rush=3,
   intents=[{'t': 'attack', 'v': 110, 'say': '크레용 돌진'}, {'t': 'multi', 'v': 40, 'n': 3, 'say': '덧칠 연타'}, {'t': 'attack', 'v': 80, 'say': '꼬다리 찌르기'}],
   passives=[{'name': '영광의 꼬다리', 'on': 'allyDown', 'do': {'t': 'buff', 'id': '사기', 'v': 1, 'say': '영광의 꼬다리'}}],
   blurb='크레용 차원의 전사 — 카드 세 장이면 돌진해 칩니다. 동료가 쓰러지면 사기. 서두르는 편이 좋습니다.')
mk('crayontanker', None, mon='막을레용', race='없음', grade='일반', sec='30.2', spine='crayontanker', nat='순수',
   concept='넉백 주의 · 낮은 공격력 — 평범한 공격은 간지러움 · 손바닥 방패 · 밀치기', name='막을레용', hp=560, row='front', tough=4, pick='shuffle',
   intents=[{'t': 'guard', 'v': 50, 'say': '손바닥 방패'}, {'t': 'jam', 'v': 1, 'say': '배로 밀치기'}, {'t': 'attack', 'v': 70, 'say': '방패 밀기'}],
   counters=[{'name': '간지럼', 'desc': '평범한 공격은 간지러움', 'start': 1, 'max': 1, 'flat': 1, 'onHit': -1, 'resetTurnStart': True}],
   passives=[{'name': '상처는 훈장', 'on': 'broken', 'do': {'t': 'buff', 'id': '취약', 'v': 2, 'say': '진짜 아픔'}}],
   blurb='비대한 몸으로 눈길을 끄는 크레용 전사 — 턴마다 첫 대는 「간지럼」 으로 피해 1. 적 전체 실드를 두르고 밀쳐 내 다음 턴 AP 를 깎습니다. 가벼운 카드로 간지럼을 벗긴 뒤 크게, 격파하면 취약 2.')
mk('crayonarcher', None, mon='휠레용', race='없음', grade='일반', sec='30.3', spine='crayonarcher', nat='냉정',
   concept='높은 공격력 · 낮은 HP(원거리) — 긴 허리로 쏘기 · 앞에 동료가 있으면 느긋', name='휠레용', hp=290, row='back', tough=3, pick='shuffle',
   intents=[{'t': 'back', 'v': 90, 'say': '허리 휘어 쏘기'}, {'t': 'charge', 'say': '허리 끝까지 젖히기', 'next': {'t': 'back', 'v': 190, 'say': '한 발 더 쏘아 맞히기'}, 'brk': True},
            {'t': 'attack', 'v': 60, 'say': '크레용 화살'}],
   blurb='긴 허리로 멀리서 쏘는 크레용 전사 — 뒷줄을 노리고, 허리를 끝까지 젖힌 큰 한 발은 격파로 끊깁니다.')
mk('crayonwizard', None, mon='지원할레용', race='없음', grade='일반', sec='30.4', spine='crayonwizard', nat='순수',
   concept='버프 · 회복 주의 · 낮은 공격력 — 크레용 장로 · 사기 · 회복', name='지원할레용', hp=320, row='back', tough=3, pick='shuffle',
   intents=[{'t': 'buff', 'id': '사기', 'v': 1, 'say': '전쟁 놀이 응원', 'all': True}, {'t': 'heal', 'v': 90, 'say': '덧칠 치료'},
            {'t': 'debuff', 'id': '약화', 'v': 1, 'say': '낙서 주문'}, {'t': 'attack', 'v': 50, 'say': '지팡이 톡'}],
   blurb='크레용 부족의 장로 마법사 — 적 전체 사기를 올리고 동료를 회복시키며 약화를 겁니다. 먼저 끊는 편이 좋습니다.')

# ═══════════════════════ 누루링 시리즈(세계수 수액 · 종족 없음 · 마을 종족 스킨) ═══════════════════════
NURU_V = {'dragon': ('Dragon', 'dragon', '용족'), 'erpien': ('Fairy', 'fairy', '요정'), 'furry': ('Furry', 'furry', '수인'),
          'ghost': ('Ghost', 'ghost', '유령'), 'monatium': ('Elf', 'elf', '엘프'), 'spirit': ('Spirit', 'spirit', '정령')}
ROLE = {'nururingtanker': ('tanker', '탱커', '높은 HP 주의 · 낮은 공격력'), 'nururingwarrior': ('dealer', '전사', '높은 공격력 주의 · 낮은 HP'),
        'nururingarcher': ('wizard', '마법사', '높은 공격력 주의 · 낮은 HP'), 'nururingsupporter': ('supporter', '서포터', '회복 주의 · 낮은 공격력')}
for vid, (skin, sfx, rk) in NURU_V.items():
    for sp, (ic, rn, feat) in ROLE.items():
        oid = f'{sp}_{sfx}'
        o = old[oid]
        mk(oid, oid, mon=f'누루링-{rk} {rn}', race='없음', grade='일반', sec='32', spine=sp, nat=None, skin='Skin_' + skin, icon=f'icon_{sfx}curseddoll{ic}',
           concept=f'{feat} — 세계수 수액({rk} 땅의 맛)', blurb=o.get('blurb'))

# ═══════════════════════ 보스 몸(사도 클론) — 층마다 하나, 졸개 없음 ═══════════════════════
def boss(nid, **over):
    b = copy.deepcopy(old[nid]); b.update(over)
    E[nid] = b; ART[nid] = oldart[nid]
    META[nid] = (b['name'], '(사도)', '보스', '사도 클론 — 판마다 적 속성 · 층 성급으로 엔진이 바꿔 세움', '사도 스파인 spine/ingame')
    return b

def fix_summon(b, mapping):
    def walk(o):
        if isinstance(o, dict):
            if o.get('t') == 'summon' and o.get('id') in mapping: o['id'] = mapping[o['id']]
            if o.get('who') in mapping: o['who'] = mapping[o['who']]
            for v in o.values(): walk(v)
        elif isinstance(o, list):
            for v in o: walk(v)
    walk(b)

def strip_blurb(b, old_txt, new_txt):
    b['blurb'] = b['blurb'].replace(old_txt, new_txt)

# 1층 몸 10~12 · 2층 몸 13~14. 졸개가 빠진 몫은 HP 로 조금 메운다(시뮬로 맞춤).
b = boss('clone_rude')
b = boss('clone_daya')
b['intents'].append({'t': 'summon', 'id': 'golem', 'n': 1, 'max': 1, 'say': '골렘 깨우기', 'noTough': True})
strip_blurb(b, '골렘이 쓰러지면 연쇄 피어스.', '골렘(강인도 없음)을 깨워 세우고, 골렘이 쓰러지면 연쇄 피어스.')
b = boss('clone_carrot')
b['intents'].append({'t': 'summon', 'id': 'ginseng', 'n': 1, 'max': 1, 'say': '산사모 캐기', 'noTough': True})
b = boss('clone_erpin')
b['intents'].append({'t': 'summon', 'id': 'marshmallowtanker', 'n': 1, 'max': 1, 'say': '간식 부르기', 'noTough': True})
strip_blurb(b, '곁의 멜로가 쓰러지면 먹어 치워 크게 회복합니다 — 졸개를 남겨 두는 것도 수입니다.', '간식(탱탱 멜로 · 강인도 없음)을 불러 세우고, 그것이 쓰러지면 먹어 치워 크게 회복합니다 — 남겨 두는 것도 수입니다.')
b = boss('clone_beni', hp=2300, tough=10)
b['intents'].append({'t': 'summon', 'id': 'furrywarriorcloserange', 'n': 1, 'max': 1, 'say': '사료스탕스 호출', 'noTough': True})
b['blurb'] = '1층 보스 · 베니(클론) — 먹보 곰. 사료스탕스 수련생(강인도 없음)을 불러 세우고, 적이 쓰러질 때마다 「한 입」 을 먹어 크게 회복하고 다음 행동이 세집니다(행동하면 빠집니다). 부른 수련생은 남겨 두고 베니를 깎는 편이 좋습니다.'
b = boss('clone_tig')
b = boss('clone_spiky')
strip_blurb(b, ' 호박들은 영혼을 나눕니다.', '')
b = boss('clone_shady'); fix_summon(b, {'shadyfollowercloserange_naive': 'shadyfollowercloserange'})
b = boss('clone_canna'); fix_summon(b, {'drones_gloomy': 'drones'})
b = boss('clone_elena'); fix_summon(b, {'drones_cool': 'drones'})
strip_blurb(b, '드론 S형이 쓰러지면 자폭 기능이 작동해 파티를 칩니다. 드론을 남겨 두면 실드가, 부수면 자폭이 옵니다 — 엘레나를 먼저 쓰러뜨리면 세운 드론도 멈춥니다.', '세운 드론 S형(강인도 없음)이 쓰러지면 자폭 기능이 작동해 파티를 칩니다. 드론을 남겨 두면 실드가, 부수면 자폭이 옵니다 — 엘레나를 쓰러뜨리면 세운 드론도 멈춥니다.')
b = boss('clone_ifrit'); fix_summon(b, {'wisps_naive': 'wisps'})
b = boss('clone_sylla')
b['intents'].append({'t': 'summon', 'id': 'wisps', 'n': 1, 'max': 1, 'say': '바람 정령 부르기', 'noTough': True})

# ═══════════════════════ 보스 몸 강인도 7(사용자 2026-10-06 — 옛 10~14) ═══════════════════════
for k, e in E.items():
    if e.get('boss'): e['tough'] = 7

# ═══════════════════════ 강인도 회복 스킬(사용자 2026-10-06 — 카제나식) ═══════════════════════
# 강인도는 저절로 차지 않는다. 격파되면 다음 턴에 가득 차고, 그 밖엔 회복 스킬이 있는 적만 되찾는다(엔진 Rules.TOUGH).
# 옛 데이터의 수 tough 1(탱커 · 방패 수 14곳 — guard 면 적 전체가 찼다)을 모두 걷고, 컨셉에 맞는 몇에만 다시 준다.
def strip_tough(o):
    if isinstance(o, dict):
        if 't' in o: o.pop('tough', None)
        for v in o.values(): strip_tough(v)
    elif isinstance(o, list):
        for v in o: strip_tough(v)
for k, e in E.items():
    for f in ('open', 'intents', 'phase', 'phase2', 'passives'):
        if f in e: strip_tough(e[f])

def regain(nid, say, n, add=None):
    """수 say 에 강인도 회복 n(그 적 자신만)."""
    hit = [i for i in E[nid]['intents'] if i.get('say') == say]
    assert len(hit) == 1, (nid, say)
    hit[0]['tough'] = n
    if add: E[nid]['blurb'] = E[nid]['blurb'] + ' ' + add

# 방패 — 막는 수와 함께 제 강인도 1
regain('droneg', '방어 프로토콜', 1, '방어 프로토콜을 펼칠 때 제 강인도를 1 되찾습니다.')
regain('crayontanker', '손바닥 방패', 1, '손바닥 방패를 들 때 제 강인도를 1 되찾습니다.')
regain('elfsoldiercloserange_elite', '바리케이드 증설', 1)
E['elfsoldiercloserange_elite']['blurb'] = E['elfsoldiercloserange_elite']['blurb'].replace('실드에만 기대지 않는 편이 좋습니다.', '바리케이드를 늘릴 때 제 강인도를 1 되찾습니다. 실드에만 기대지 않는 편이 좋습니다.')
# 거대형 — 몸으로 버팀
regain('marshmallowtanker', '탱탱하게 부풀기', 1, '탱탱하게 부풀 때 제 강인도를 1 되찾습니다.')
E['golem_elite'].setdefault('passives', []).append({'name': '단단한 핵', 'on': 'turnStart', 'do': {'t': 'brace', 'v': 1, 'say': '단단한 핵'}})
E['golem_elite']['blurb'] = E['golem_elite']['blurb'].replace('강인도 8.', '강인도 8. 턴이 시작될 때마다 「단단한 핵」 으로 강인도를 1 되찾습니다.')
# 보스 — 몸으로 버티는 둘(강인도 7 기준 2)
regain('clone_rude', '근육 부풀리기', 2)
E['clone_rude']['blurb'] = E['clone_rude']['blurb'].replace('격파하면 세트가 끊깁니다.', '근육을 부풀릴 때 강인도를 2 되찾습니다. 격파하면 세트가 끊깁니다.')
regain('clone_beni', '배 내밀기', 2)
E['clone_beni']['blurb'] = E['clone_beni']['blurb'].replace('부른 수련생은', '배를 내밀 때 강인도를 2 되찾습니다. 부른 수련생은')
TOUGH_REGAIN = ['droneg', 'crayontanker', 'elfsoldiercloserange_elite', 'marshmallowtanker', 'golem_elite', 'clone_rude', 'clone_beni']

# ═══════════════════════ 평소 수는 무작위 · 예고는 고학년과 힘 모으기만(사용자 2026-10-07) ═══════════════════════
# 「고학년만 예고로 하고 … 나머지 평소 패턴은 무작위로」 · 「엘리트나 적들도 예고 패턴 같은 거 강화하고」.
#   ① 모두 pick shuffle — 여는 수(open) · 판 바뀜(phase)은 그대로. 같은 종류 세 번 잇기 금지 · 힘 모으기 간격 · 꽉 찬 소환 빼기는 엔진(Battle.ShuffleOk).
#   ② 무게: 보통 3 · 가장 센 치는 수 2 · 소환 1 · 힘 모으기(일반 2 · 엘리트 4). 회복 · 강인도 회복 수는 엔진이 형편 따라 곱한다(Battle.ShuffleW).
#   ③ 보스 몸의 힘 모으기는 걷는다(예고는 고학년만) — 모아 쏟던 수는 바로 하는 수로, 값 × BOSS_UNCHARGE.
#   ④ 엘리트는 몬스터마다 큰 힘 모으기 하나, 일반은 컨셉이 맞는 몇에만 약한 것 — 모으는 턴 · 쏟는 턴에 격파하면 끊긴다(brk).
#   단계 측정용: FOE_CHARGE=0 이면 ④ 를 건너뛴다.
W_BASE, W_TOP, W_SUMMON, W_CHARGE_MOB, W_CHARGE_ELITE = 3, 2, 1, 2, 4
BOSS_UNCHARGE = 0.65
ADD_CHARGE = os.environ.get('FOE_CHARGE', '1') != '0'

def threat(i):
    t = i.get('t')
    if t in ('attack', 'back'): return i.get('v', 0)
    if t == 'multi': return i.get('v', 0) * max(1, i.get('n', 1))
    if t == 'attackAll': return i.get('v', 0) * 2
    return 0

def uncharge(i):
    """보스 몸의 힘 모으기 → 바로 하는 수(안쪽 끝 수, 값 × BOSS_UNCHARGE)."""
    while i.get('t') == 'charge': i = i['next']
    i = copy.deepcopy(i); i.pop('brk', None)
    if isinstance(i.get('v'), (int, float)) and i['t'] in ('attack', 'back', 'multi', 'attackAll'): i['v'] = int(round(i['v'] * BOSS_UNCHARGE / 5) * 5)
    return i

def charge(warn, nxt):
    n = dict(nxt); n['brk'] = True
    return {'t': 'charge', 'say': warn, 'next': n, 'brk': True}

# 엘리트 — 몬스터마다 큰 힘 모으기 하나(원값 — 아래 BAL 이 곱한다). 평소 가장 센 수의 두 배 안팎.
ELITE_CHARGE = {
    'hatchling_elite': ('불씨 모으기', {'t': 'attackAll', 'v': 140, 'say': '창시자의 화염'}),
    'proteindragon_elite': ('마지막 한 세트 준비', {'t': 'attack', 'v': 280, 'say': '한계 돌파 펀치'}),
    'golem_elite': ('대지 끌어올리기', {'t': 'attackAll', 'v': 150, 'say': '대지 붕괴'}),
    'fairymobcloserange_elite': ('토핑 몽땅 끌어안기', {'t': 'multi', 'v': 70, 'n': 4, 'say': '토핑 폭격'}),
    'magicfork_elite': ('자루 높이 치켜들기', {'t': 'attack', 'v': 290, 'say': '밭 갈아엎기'}),
    'ginseng_elite': ('양분 끌어올리기', {'t': 'attackAll', 'v': 110, 'id': '고통', 'n': 2, 'say': '쓴 즙 폭발'}),
    'goldring_elite': ('금고 문 활짝 열기', {'t': 'multi', 'v': 65, 'n': 4, 'say': '금화 폭포'}),
    'furrywarriorcloserange_elite': ('정신 집중', {'t': 'attack', 'v': 300, 'say': '천 번째 지르기'}),
    'gluttonbear_elite': ('두목의 포효', {'t': 'attackAll', 'v': 130, 'say': '두목의 내려찍기'}),
    'foodscavenger_elite': ('식탁 차리기', {'t': 'multi', 'v': 60, 'n': 4, 'say': '풀코스 포크질'}),
    'furring_elite': ('돈다발 쌓기', {'t': 'attackAll', 'v': 125, 'say': '돈다발 폭탄'}),
    'blanketghost_elite': ('악몽 불러오기', {'t': 'attackAll', 'v': 110, 'id': '약화', 'n': 1, 'say': '가위눌림 악몽'}),
    'shadyfollowercloserange_elite': ('친위대 집결', {'t': 'multi', 'v': 55, 'n': 4, 'id': '충격', 'say': '친위대 총공격'}),
    'pumpkin_elite': ('서러움 꾹꾹 담기', {'t': 'attackAll', 'v': 130, 'say': '서러움 폭발'}),
    'hatsnail_elite': ('의식 주문 외우기', {'t': 'attackAll', 'v': 125, 'say': '마녀의 대주문'}),
    'elfsoldiercloserange_elite': ('철거 장비 돌리기', {'t': 'attack', 'v': 290, 'say': '철거 망치'}),
    'droneg_elite': ('경보 최고 단계', {'t': 'attackAll', 'v': 110, 'id': '충격', 'n': 2, 'say': '전체 진압 사격'}),
    'wisps_elite': ('원소 응축', {'t': 'attackAll', 'v': 120, 'id': '고통', 'n': 2, 'say': '원소 대폭발'}),
    'lupalu_elite': ('냉기 모으기', {'t': 'multi', 'v': 65, 'n': 4, 'id': '균열', 'say': '얼음 병 난타'}),
    'oldtree_elite': ('뿌리 깊이 박기', {'t': 'attackAll', 'v': 140, 'say': '뿌리째 휩쓸기'}),
}
# 일반 — 컨셉이 맞는 몇(돌격병 · 거대형 · 마법사)에만 약한 힘 모으기. 평소 가장 센 수의 1.6배 안팎.
MOB_CHARGE = {
    'elfsoldiercloserange': ('돌격 대열 갖추기', {'t': 'attack', 'v': 170, 'say': '대열 돌격'}),
    'golem': ('보석 주먹 치켜들기', {'t': 'attack', 'v': 180, 'say': '보석 내려찍기'}),
    'gluttonbear': ('크게 숨 들이쉬기', {'t': 'attack', 'v': 175, 'say': '곰 몸통 박치기'}),
    'marshmallowtanker': ('크게 부풀기', {'t': 'attack', 'v': 165, 'say': '말랑 깔아뭉개기'}),
    'crayonwizard': ('주문 외우기', {'t': 'attackAll', 'v': 70, 'say': '낙서 대폭발'}),
}

def charge_blurb(e, warn, nxt):
    s = f'「{warn}」 로 힘을 모으면 다음 턴 「{nxt["say"]}」 — 모으는 턴 · 쏟는 턴에 격파하면 끊깁니다.'
    b = e.get('blurb', '')
    e['blurb'] = b.replace(' 희귀종:', ' ' + s + ' 희귀종:', 1) if ' 희귀종:' in b else (b + ' ' + s).strip()

# ═══════════════════════ 카제나식 리워크 — 37종 전부(2026-10-07 사용자 결정 · _measure/적_리워크_지침.md) ═══════════════════════
# 설계는 _gen/foes/rework.py(몬스터마다 의도 · 패시브 · 격파 반응 · 방해). 힘 모으기도 거기서 넣으므로 아래 ELITE_CHARGE · MOB_CHARGE 는 쓰지 않는다.
# 전후 측정용: FOE_REWORK=0 이면 리워크 앞(시범 전) 데이터를 그대로 다시 만든다(묶음 고치기도 건너뜀).
REWORK = os.environ.get('FOE_REWORK', '1') != '0'
if REWORK:
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import rework
    rework.apply(E)
    ELITE_CHARGE.clear(); MOB_CHARGE.clear()

if ADD_CHARGE:
    for k, (warn, nxt) in list(ELITE_CHARGE.items()) + list(MOB_CHARGE.items()):
        e = E[k]
        e['intents'].append(charge(warn, nxt))
        if 'phase' in e: e['phase']['intents'].append(charge(warn, nxt))   # 판이 바뀌어도 같은 큰 수
        charge_blurb(e, warn, nxt)

for k, e in E.items():
    boss_body = bool(e.get('boss'))
    elite = k.endswith('_elite')
    e['pick'] = 'shuffle'
    lists = [e['intents']] + [e[f]['intents'] for f in ('phase', 'phase2') if f in e]
    for lst in lists:
        if boss_body: lst[:] = [uncharge(i) if i.get('t') == 'charge' else i for i in lst]
        else:
            for i in lst:   # 남은 힘 모으기 — 모으는 턴 · 쏟는 턴 모두 격파로 끊긴다
                if i.get('t') == 'charge': i['brk'] = True; i['next']['brk'] = True
        top = max((threat(i) for i in lst if i.get('t') != 'charge'), default=0)
        tops = [i for i in lst if i.get('t') != 'charge' and threat(i) == top and top > 0]
        for i in lst:
            t = i.get('t')
            if t == 'summon': w = W_SUMMON
            elif t == 'charge': w = W_CHARGE_ELITE if elite else W_CHARGE_MOB
            elif len(tops) == 1 and i is tops[0]: w = W_TOP
            else: w = W_BASE
            i['w'] = w

# ═══════════════════════ 밸런스(시뮬로 맞춤 — 졸개가 빠진 보스 · 몬스터 정리 뒤 완주율을 리워크 전 근처로) ═══════════════════════
import math
# 2026-10-07 평소 수 무작위 · 고학년 강화 · 엘리트/일반 힘 모으기 뒤 보스 몸 HP 1.25 → 1.2(숙련 · 역할 30.9% → 32.0%, _measure/적_리워크.md §8).
BAL = {'mob_hp': float(os.environ.get('FOE_MOB_HP', 1.1)), 'mob_dmg': float(os.environ.get('FOE_MOB_DMG', 1.26 if REWORK else 1.155)),
       'boss_hp': float(os.environ.get('FOE_BOSS_HP', 1.2)), 'boss_dmg': float(os.environ.get('FOE_BOSS_DMG', 1.155))}
HIT = {'attack', 'back', 'multi', 'attackAll'}
def r5(x, step): return int(round(x / step) * step)
def scale(e, hp, dmg):
    e['hp'] = r5(e['hp'] * hp, 10)
    def walk(o):
        if isinstance(o, dict):
            if o.get('t') in HIT and isinstance(o.get('v'), (int, float)): o['v'] = r5(o['v'] * dmg, 5)
            for v in o.values(): walk(v)
        elif isinstance(o, list):
            for v in o: walk(v)
    for k in ('open', 'intents', 'phase', 'phase2', 'counters', 'passives'):
        if k in e: walk(e[k])
for k, e in E.items():
    if e.get('boss'): scale(e, BAL['boss_hp'], BAL['boss_dmg'])
    else: scale(e, BAL['mob_hp'], BAL['mob_dmg'])
# 하나씩 맞춤(BAL 다음에 곱한다) — 적 id: (HP 배율, 피해 배율). 그 적 정의에만 걸린다.
#   수인 부락(2026-10-06): 완주 11.4%(다른 마을 21~26%). 2층 보스 패배 58%(다른 마을 11~45%)가 몸통 — 티그 몸(2층 보스 몸 가운데 HP 가장 높음 ·
#   간파 · 장작 패기 · 검기)을 빌린 클론이 누구든 46~91% 로 졌다. 2층 엘리트도 퍼리 · 몰락한 큰손 · 머곰 · 두목 줄이 71~80% 패배.
#   clone_tig 는 수인 부락 2층 보스 칸에만 있어 다른 마을 보스에는 안 걸린다. 측정: _measure/적_리워크.md §5.
BAL_ONE = {'clone_tig': (0.65, 0.7), 'furring_elite': (0.75, 0.75), 'gluttonbear_elite': (0.75, 0.75)}
# 카제나식 리워크 뒤 마을 맞춤(2026-10-07 — _measure/적_리워크_전체.md). 리워크 몫(REWORK)일 때만 곱한다. 값은 (HP, 피해) — 위 BAL_ONE 에 더 곱한다.
REWORK_ONE = {
    # 모나티엄(리워크 뒤 52% — 엘리트 · 일반이 약했다)
    'elfsoldiercloserange_elite': (1.15, 1.15), 'droneg_elite': (1.2, 1.25), 'elfsoldiercloserange': (1.1, 1.1), 'drones': (1.0, 1.15), 'droneg': (1.1, 1.1),
    'elfsoldierlongrange': (1.1, 1.1), 'nururingtanker_elf': (1.1, 1.1), 'nururingwarrior_elf': (1.1, 1.1), 'nururingarcher_elf': (1.1, 1.1), 'nururingsupporter_elf': (1.1, 1.1),
    # 유령 늪(45% — 1층 엘리트 패배 1% 안팎)
    'blanketghost_elite': (1.1, 1.1), 'hatsnail_elite': (1.15, 1.15), 'pumpkin_elite': (1.1, 1.15), 'blanketghost': (1.0, 1.1), 'hatsnail': (1.0, 1.1), 'clone_shady': (1.1, 1.05),
    'shadyfollowercloserange': (1.05, 1.1), 'shadyfollowerlongrange': (1.0, 1.1), 'nururingtanker_ghost': (1.1, 1.1), 'nururingwarrior_ghost': (1.1, 1.1), 'nururingarcher_ghost': (1.1, 1.1), 'nururingsupporter_ghost': (1.1, 1.1),
    # 에르피엔(28% — 1층 불효자손 · 폭주 27% · 2층 금고 62%)
    'magicfork_elite': (0.85, 0.9), 'goldring_elite': (0.8, 0.8), 'fairymobcloserange_elite': (0.9, 0.9),
    'fairymobcloserange': (1.0, 0.8), 'ginseng_elite': (0.95, 0.95), 'magicfork': (1.0, 0.95), 'buseuleogi': (1.0, 0.9), 'mogmaekim': (1.0, 0.9), 'nururingtanker_fairy': (1.0, 0.9), 'nururingwarrior_fairy': (1.0, 0.9), 'marshmallowtanker': (1.0, 0.9), 'nururingarcher_fairy': (1.0, 0.9), 'marshmallowdealer': (1.0, 0.9), 'fairymoblongrange': (0.95, 0.9),
    # 정령산(2층 산정 · 열매 직전 묶음 62~64%)
    'lupalu_elite': (0.9, 0.9), 'oldtree_elite': (0.9, 0.9), 'wisps_elite': (1.1, 1.1),
    # 용족 동굴(2층 조교 묶음 51% · 창시자 49%)
    'proteindragon_elite': (1.0, 1.0), 'hatchling_elite': (0.95, 0.95), 'imoogi_elite': (1.15, 1.15), 'golem_elite': (1.15, 1.15),
    # 수인 부락(1층 엘리트 패배 1% 안팎)
    'foodscavenger_elite': (1.15, 1.1), 'gluttonbear_elite': (1.2, 1.2), 'furrywarriorcloserange_elite': (1.05, 1.05),
}
for k, (hp, dmg) in BAL_ONE.items(): scale(E[k], hp, dmg)
if REWORK:
    for k, (hp, dmg) in REWORK_ONE.items(): scale(E[k], hp, dmg)

# ═══════════════════════ 마을 필드 ═══════════════════════
# pools[세기 약 · 중 · 강][싸움 5] · elites[4] · boss[클론 하나]. 마을 종족 몬스터 + 종족 없는 몬스터를 섞는다.
F = {}
F['erpien'] = [
    dict(pools=[[['fairymobcloserange', 'magicfork'], ['fairymoblongrange', 'ginseng'], ['buseuleogi', 'mogmaekim'], ['nururingwarrior_fairy', 'fairymoblongrange'], ['magicfork', 'buseuleogi']],
                [['fairymobcloserange', 'fairymoblongrange'], ['ginseng', 'magicfork', 'buseuleogi'], ['nururingtanker_fairy', 'nururingarcher_fairy'], ['mogmaekim', 'fairymobcloserange'], ['magicfork', 'nururingsupporter_fairy']],
                [['fairymobcloserange', 'ginseng', 'fairymoblongrange'], ['nururingtanker_fairy', 'nururingwarrior_fairy', 'nururingsupporter_fairy'], ['magicfork', 'mogmaekim', 'fairymoblongrange'], ['buseuleogi', 'buseuleogi', 'fairymobcloserange'], ['ginseng', 'nururingwarrior_fairy', 'magicfork']]],
         elites=[['magicfork_elite', 'fairymoblongrange'], ['ginseng_elite', 'magicfork'], ['fairymobcloserange_elite', 'ginseng'], ['magicfork_elite', 'nururingsupporter_fairy']],
         boss=['clone_carrot']),
    dict(pools=[[['marshmallowtanker', 'marshmallowdealer'], ['fairymobcloserange', 'buseuleogi'], ['mogmaekim', 'fairymoblongrange'], ['marshmallowdealer', 'marshmallowsupporter'], ['nururingwarrior_fairy', 'buseuleogi']],
                [['marshmallowtanker', 'fairymoblongrange', 'marshmallowdealer'], ['mogmaekim', 'buseuleogi', 'fairymobcloserange'], ['nururingtanker_fairy', 'marshmallowdealer', 'nururingarcher_fairy'], ['fairymobcloserange', 'marshmallowsupporter', 'mogmaekim'], ['marshmallowtanker', 'fairymoblongrange', 'buseuleogi']],
                [['marshmallowtanker', 'marshmallowdealer', 'marshmallowsupporter'], ['fairymobcloserange', 'fairymoblongrange', 'mogmaekim'], ['nururingtanker_fairy', 'nururingwarrior_fairy', 'marshmallowsupporter'], ['buseuleogi', 'mogmaekim', 'marshmallowdealer', 'fairymoblongrange'], ['marshmallowtanker', 'fairymobcloserange', 'nururingarcher_fairy']]],
         elites=[['goldring_elite', 'marshmallowdealer'], ['fairymobcloserange_elite', 'marshmallowsupporter'], ['goldring_elite', 'fairymoblongrange', 'buseuleogi'], ['ginseng_elite', 'marshmallowtanker', 'mogmaekim']],
         boss=['clone_erpin'])]
F['furry'] = [
    dict(pools=[[['furrywarriorcloserange', 'furrywarriorlongrange'], ['gluttonbear', 'nependers'], ['foodscavenger', 'curseddoll'], ['nururingwarrior_furry', 'furrywarriorlongrange'], ['furrywarriorcloserange', 'nependers']],
                [['gluttonbear', 'furrywarriorlongrange'], ['foodscavenger', 'furrywarriorcloserange', 'curseddoll'], ['nururingtanker_furry', 'nururingarcher_furry'], ['nependers', 'furring'], ['furrywarriorcloserange', 'nururingsupporter_furry']],
                [['gluttonbear', 'furrywarriorcloserange', 'furrywarriorlongrange'], ['nururingtanker_furry', 'nururingwarrior_furry', 'nururingsupporter_furry'], ['foodscavenger', 'nependers', 'furrywarriorlongrange'], ['furring', 'furrywarriorcloserange', 'curseddoll'], ['gluttonbear', 'foodscavenger', 'nururingarcher_furry']]],
         elites=[['furrywarriorcloserange_elite', 'furrywarriorlongrange'], ['gluttonbear_elite', 'nependers'], ['foodscavenger_elite', 'curseddoll'], ['furrywarriorcloserange_elite', 'nururingsupporter_furry']],
         boss=['clone_beni']),
    dict(pools=[[['foodscavenger', 'furring'], ['furrywarriorcloserange', 'curseddoll'], ['gluttonbear', 'furrywarriorlongrange'], ['nependers', 'foodscavenger'], ['nururingwarrior_furry', 'furring']],
                [['foodscavenger', 'furring', 'furrywarriorcloserange'], ['gluttonbear', 'curseddoll', 'furrywarriorlongrange'], ['nururingtanker_furry', 'foodscavenger', 'nururingarcher_furry'], ['nependers', 'furring', 'furrywarriorcloserange'], ['gluttonbear', 'nururingsupporter_furry']],
                [['gluttonbear', 'foodscavenger', 'furring'], ['furrywarriorcloserange', 'furrywarriorlongrange', 'curseddoll'], ['nururingtanker_furry', 'nururingwarrior_furry', 'furring'], ['foodscavenger', 'nependers', 'furrywarriorlongrange', 'curseddoll'], ['gluttonbear', 'furrywarriorcloserange', 'nururingarcher_furry']]],
         elites=[['furring_elite', 'foodscavenger'], ['gluttonbear_elite', 'furrywarriorlongrange', 'furring'], ['foodscavenger_elite', 'furrywarriorcloserange', 'curseddoll'], ['furring_elite', 'nependers', 'furrywarriorlongrange']],
         boss=['clone_tig'])]
F['ghost'] = [
    dict(pools=[[['pumpkin', 'blanketghost'], ['nependers', 'hatsnail'], ['blanketghost', 'curseddoll'], ['nururingwarrior_ghost', 'pumpkin'], ['hatsnail', 'blanketghost']],
                [['pumpkin', 'nependers', 'blanketghost'], ['hatsnail', 'curseddoll'], ['nururingtanker_ghost', 'nururingarcher_ghost'], ['blanketghost', 'pumpkin', 'hatsnail'], ['nependers', 'nururingsupporter_ghost']],
                [['pumpkin', 'hatsnail', 'blanketghost'], ['nururingtanker_ghost', 'nururingwarrior_ghost', 'nururingsupporter_ghost'], ['nependers', 'curseddoll', 'blanketghost'], ['blanketghost', 'blanketghost', 'pumpkin'], ['hatsnail', 'nururingarcher_ghost', 'curseddoll']]],
         elites=[['pumpkin_elite', 'blanketghost'], ['hatsnail_elite', 'curseddoll'], ['blanketghost_elite', 'nependers'], ['pumpkin_elite', 'nururingsupporter_ghost']],
         boss=['clone_spiky']),
    dict(pools=[[['shadyfollowercloserange', 'shadyfollowerlongrange'], ['blanketghost', 'hatsnail'], ['shadyfollowercloserange', 'pumpkin'], ['curseddoll', 'shadyfollowerlongrange'], ['nururingwarrior_ghost', 'blanketghost']],
                [['shadyfollowercloserange', 'shadyfollowerlongrange', 'curseddoll'], ['hatsnail', 'blanketghost', 'shadyfollowerlongrange'], ['nururingtanker_ghost', 'shadyfollowercloserange', 'nururingarcher_ghost'], ['pumpkin', 'nependers', 'shadyfollowerlongrange'], ['shadyfollowercloserange', 'blanketghost', 'nururingsupporter_ghost']],
                [['shadyfollowercloserange', 'shadyfollowerlongrange', 'blanketghost'], ['hatsnail', 'shadyfollowercloserange', 'curseddoll'], ['nururingtanker_ghost', 'nururingwarrior_ghost', 'shadyfollowerlongrange'], ['pumpkin', 'blanketghost', 'shadyfollowerlongrange', 'curseddoll'], ['shadyfollowercloserange', 'nependers', 'nururingsupporter_ghost']]],
         elites=[['shadyfollowercloserange_elite', 'shadyfollowerlongrange'], ['blanketghost_elite', 'shadyfollowercloserange'], ['hatsnail_elite', 'blanketghost', 'curseddoll'], ['shadyfollowercloserange_elite', 'pumpkin', 'shadyfollowerlongrange']],
         boss=['clone_shady'])]
F['monatium'] = [
    dict(pools=[[['elfsoldiercloserange', 'elfsoldierlongrange'], ['drones', 'nependers'], ['droneg', 'elfsoldierlongrange'], ['nururingwarrior_elf', 'drones'], ['elfsoldiercloserange', 'nependers']],
                [['droneg', 'drones', 'elfsoldierlongrange'], ['elfsoldiercloserange', 'nependers', 'drones'], ['nururingtanker_elf', 'nururingarcher_elf'], ['elfsoldiercloserange', 'elfsoldierlongrange', 'drones'], ['droneg', 'nururingsupporter_elf']],
                [['elfsoldiercloserange', 'droneg', 'elfsoldierlongrange'], ['nururingtanker_elf', 'nururingwarrior_elf', 'nururingsupporter_elf'], ['drones', 'drones', 'nependers'], ['elfsoldiercloserange', 'nururingarcher_elf', 'drones'], ['droneg', 'elfsoldierlongrange', 'nependers']]],
         elites=[['droneg_elite', 'drones'], ['elfsoldiercloserange_elite', 'elfsoldierlongrange'], ['elfsoldierlongrange_elite', 'elfsoldiercloserange'], ['droneg_elite', 'nururingsupporter_elf']],
         boss=['clone_canna']),
    dict(pools=[[['elfsoldiercloserange', 'drones'], ['droneg', 'elfsoldierlongrange'], ['elfsoldierlongrange', 'nependers'], ['nururingwarrior_elf', 'elfsoldiercloserange'], ['drones', 'droneg']],
                [['elfsoldiercloserange', 'elfsoldierlongrange', 'drones'], ['droneg', 'drones', 'nependers'], ['nururingtanker_elf', 'elfsoldierlongrange', 'nururingarcher_elf'], ['elfsoldiercloserange', 'droneg', 'elfsoldierlongrange'], ['drones', 'nururingsupporter_elf', 'elfsoldiercloserange']],
                [['elfsoldiercloserange', 'elfsoldiercloserange', 'elfsoldierlongrange'], ['droneg', 'drones', 'drones'], ['nururingtanker_elf', 'nururingwarrior_elf', 'elfsoldierlongrange'], ['elfsoldiercloserange', 'droneg', 'nependers', 'drones'], ['elfsoldierlongrange', 'nururingarcher_elf', 'droneg']]],
         elites=[['elfsoldiercloserange_elite', 'droneg', 'elfsoldierlongrange'], ['droneg_elite', 'elfsoldierlongrange', 'drones'], ['elfsoldierlongrange_elite', 'elfsoldiercloserange', 'drones'], ['elfsoldiercloserange_elite', 'nururingsupporter_elf', 'nependers']],
         boss=['clone_elena'])]
F['spirit'] = [
    dict(pools=[[['wisps', 'lupalu'], ['oldtree', 'wisps'], ['pumpkin', 'lupalu'], ['nururingwarrior_spirit', 'wisps'], ['nependers', 'lupalu']],
                [['oldtree', 'wisps', 'lupalu'], ['nependers', 'pumpkin'], ['nururingtanker_spirit', 'nururingarcher_spirit'], ['wisps', 'wisps', 'lupalu'], ['oldtree', 'nururingsupporter_spirit']],
                [['oldtree', 'lupalu', 'wisps'], ['nururingtanker_spirit', 'nururingwarrior_spirit', 'nururingsupporter_spirit'], ['pumpkin', 'nependers', 'wisps'], ['lupalu', 'nururingarcher_spirit', 'wisps'], ['oldtree', 'pumpkin', 'lupalu']]],
         elites=[['wisps_elite', 'lupalu'], ['oldtree_elite', 'wisps'], ['pumpkin_elite', 'lupalu'], ['wisps_elite', 'nururingsupporter_spirit']],
         boss=['clone_ifrit']),
    dict(pools=[[['lupalu', 'wisps'], ['oldtree', 'nependers'], ['wisps', 'pumpkin'], ['nururingwarrior_spirit', 'lupalu'], ['oldtree', 'wisps']],
                [['oldtree', 'lupalu', 'wisps'], ['pumpkin', 'wisps', 'lupalu'], ['nururingtanker_spirit', 'wisps', 'nururingarcher_spirit'], ['nependers', 'oldtree', 'wisps'], ['lupalu', 'nururingsupporter_spirit', 'wisps']],
                [['oldtree', 'lupalu', 'lupalu'], ['pumpkin', 'nependers', 'wisps', 'wisps'], ['nururingtanker_spirit', 'nururingwarrior_spirit', 'lupalu'], ['oldtree', 'pumpkin', 'wisps'], ['lupalu', 'nururingarcher_spirit', 'nependers']]],
         elites=[['lupalu_elite', 'wisps', 'oldtree'], ['oldtree_elite', 'lupalu', 'wisps'], ['wisps_elite', 'pumpkin', 'lupalu'], ['lupalu_elite', 'nururingsupporter_spirit', 'nependers']],
         boss=['clone_sylla'])]
F['dragon'] = [
    dict(pools=[[['imoogi', 'hatchling'], ['golem', 'crayonarcher'], ['crayonwarrior', 'proteindragon'], ['nururingwarrior_dragon', 'hatchling'], ['imoogi', 'crayonwizard']],
                [['proteindragon', 'hatchling', 'crayonwarrior'], ['golem', 'imoogi'], ['nururingtanker_dragon', 'nururingarcher_dragon'], ['crayontanker', 'crayonarcher', 'hatchling'], ['imoogi', 'nururingsupporter_dragon']],
                [['imoogi', 'hatchling', 'proteindragon'], ['nururingtanker_dragon', 'nururingwarrior_dragon', 'nururingsupporter_dragon'], ['crayontanker', 'crayonwarrior', 'crayonwizard'], ['golem', 'hatchling', 'crayonarcher'], ['proteindragon', 'imoogi', 'nururingarcher_dragon']]],
         elites=[['imoogi_elite', 'hatchling'], ['hatchling_elite', 'crayonwarrior'], ['proteindragon_elite', 'imoogi'], ['imoogi_elite', 'nururingsupporter_dragon']],
         boss=['clone_rude']),
    dict(pools=[[['golem', 'proteindragon'], ['crayonwarrior', 'crayonarcher'], ['hatchling', 'golem'], ['nururingwarrior_dragon', 'imoogi'], ['crayontanker', 'hatchling']],
                [['golem', 'proteindragon', 'hatchling'], ['crayontanker', 'crayonwarrior', 'crayonarcher'], ['nururingtanker_dragon', 'imoogi', 'nururingarcher_dragon'], ['proteindragon', 'crayonwizard', 'imoogi'], ['golem', 'hatchling', 'nururingsupporter_dragon']],
                [['golem', 'proteindragon', 'imoogi'], ['crayontanker', 'crayonwarrior', 'crayonarcher', 'crayonwizard'], ['nururingtanker_dragon', 'nururingwarrior_dragon', 'proteindragon'], ['golem', 'hatchling', 'hatchling'], ['imoogi', 'crayonwizard', 'proteindragon']]],
         elites=[['golem_elite', 'proteindragon'], ['proteindragon_elite', 'hatchling', 'imoogi'], ['hatchling_elite', 'golem', 'crayonarcher'], ['golem_elite', 'crayonwizard', 'hatchling']],
         boss=['clone_daya'])]

VILL_RACE = {'erpien': '요정', 'furry': '수인', 'ghost': '유령', 'monatium': '엘프', 'spirit': '정령', 'dragon': '용족'}

# ═══════════════════════ 묶음 고치기 — 역할 분담(사용자 2026-10-07 「관통 · 큰 수 · 방해 · 회복은 한 묶음에 하나」) ═══════════════════════
# 몬스터마다 표지(feats): P 관통(back) · C 큰 수(charge) · D 방해(상태 카드 · 카드 상태 · 손 비용 · AP · 빼앗기 — 죽을 때 것은 뺌) · H 지원(회복 · 적 전체 버프 · 적 전체 실드).
# 옛 묶음을 앞에서부터 읽어, 앞 몬스터와 표지가 겹치거나 같은 몬스터가 또 나오면(물량 컨셉 SWARM 빼고) 그 층 몬스터 가운데
# 겹치지 않는 것으로 바꾼다 — 같은 종족 / 종족 없음 쪽 · 같은 줄(앞 · 뒤) · 덜 쓴 것 먼저. 엘리트 줄은 맨 앞 엘리트를 두고 곁을 고친다.
DISRUPT_T = {'addCard', 'cardDebuff', 'handCost', 'jam', 'seize', 'reshuffle', 'autoPlay'}
SWARM = {'buseuleogi', 'mogmaekim', 'wisps'}
def feats(k):
    e = E[k]; f = set()
    ins = list(e['intents']) + ([e['open']] if e.get('open') else []) + [i for ph in ('phase', 'phase2') if ph in e for i in e[ph]['intents']]
    for i in ins:
        t = i['t']
        if t == 'charge': f.add('C'); t = i['next']['t']
        if t == 'back': f.add('P')
        if t in DISRUPT_T: f.add('D')
        if t in ('heal', 'guard') or (t == 'buff' and i.get('all')): f.add('H')
    for p in e.get('passives', []):
        if p['on'] != 'death' and p['do']['t'] in DISRUPT_T: f.add('D')
    return f
LINE_LOG = []
def fix_lines():
    for vid, floors in F.items():
        for fi, fl in enumerate(floors):
            pool = sorted({x for t in fl['pools'] for l in t for x in l})
            use = collections.Counter(x for t in fl['pools'] for l in t for x in l)
            for field in ('pools', 'elites'):
                lines = [l for t in fl['pools'] for l in t] if field == 'pools' else fl['elites']
                for l in lines:
                    old = list(l); new = []
                    for x in old:
                        taken = set().union(*[feats(y) for y in new]) if new else set()
                        if not (feats(x) & taken) and (x not in new or x in SWARM): new.append(x); continue
                        if field == 'elites' and not new: new.append(x); continue
                        grp = META[x][1] == '없음'
                        c = [y for y in pool if not (feats(y) & taken) and y not in new and y != x]
                        c.sort(key=lambda y: ((META[y][1] == '없음') != grp, E[y].get('row') != E[x].get('row'), use[y], y))
                        if c: new.append(c[0]); use[c[0]] += 1; use[x] -= 1
                    if new != old: LINE_LOG.append(f'{vid} {fi + 1}층 {field}: {"+".join(old)} → {"+".join(new)}')
                    l[:] = new
if REWORK: fix_lines()
# 2층 엘리트 묶음 가운데 패배 50% 넘는 셋짜리는 곁 하나를 뺀다(지침 §5 · 리워크 뒤 4,500판 × 씨앗 0 · 1 — _measure/적_리워크_전체.md).
TRIM = [('dragon', 'golem_elite+crayonwizard+hatchling', 'hatchling'), ('dragon', 'proteindragon_elite+hatchling+crayonwarrior', 'crayonwarrior'),
        ('erpien', 'goldring_elite+fairymobcloserange+buseuleogi', 'buseuleogi'), ('furry', 'furring_elite+nururingtanker_furry+furrywarriorlongrange', 'nururingtanker_furry'),
        ('ghost', 'shadyfollowercloserange_elite+pumpkin+shadyfollowerlongrange', 'shadyfollowerlongrange'),
        ('monatium', 'droneg_elite+elfsoldiercloserange+drones', 'drones'), ('monatium', 'elfsoldierlongrange_elite+elfsoldiercloserange+drones', 'drones'),
        ('spirit', 'lupalu_elite+wisps+oldtree', 'wisps'), ('spirit', 'oldtree_elite+lupalu+wisps', 'lupalu'), ('spirit', 'wisps_elite+pumpkin+lupalu', 'lupalu')]
if REWORK:
    for vid, line, drop in TRIM:
        hit = [l for l in F[vid][1]['elites'] if '+'.join(l) == line]
        assert len(hit) == 1, (vid, line)
        hit[0].remove(drop); LINE_LOG.append(f'{vid} 2층 elites: {line} → {"+".join(hit[0])}(곁 하나 뺌)')

# ── 검사(쓰기 전) ──
errs = []
used = collections.defaultdict(set)
for vid, floors in F.items():
    for fi, fl in enumerate(floors):
        lines = [l for t in fl['pools'] for l in t] + fl['elites'] + [fl['boss']]
        for l in lines:
            for x in l:
                if x not in E: errs.append(f'{vid} {fi + 1}층: 없는 적 {x}')
                else:
                    used[vid].add(x)
                    r = META[x][1]
                    if r not in ('없음', '(사도)', VILL_RACE[vid]): errs.append(f'{vid}: 다른 종족 몬스터 {x}({r})')
        for l in fl['elites']:
            if max(E[x].get('tough', 0) for x in l) < 5: errs.append(f'{vid} 엘리트 {l}: 강인도 5 이상 몬스터가 없다')
        if len(fl['boss']) != 1 or not E[fl['boss'][0]].get('boss'): errs.append(f'{vid}: 보스 칸은 보스 하나')
    races = {META[x][1] for x in used[vid]}
    if VILL_RACE[vid] not in races or '없음' not in races: errs.append(f'{vid}: 종족 · 무종족을 섞지 않았다 {races}')
for k, e in E.items():
    g = META[k][2]
    t = e.get('tough', 0)
    if g == '일반' and t < 3: errs.append(f'{k}: 일반 강인도 {t} < 3')
    if g == '엘리트' and t < 5: errs.append(f'{k}: 엘리트 강인도 {t} < 5')
    if g == '보스' and t < 7: errs.append(f'{k}: 보스 강인도 {t} < 7')
# 역할 분담 검사(리워크 뒤) — 한 묶음에 같은 표지 둘 · 같은 몬스터 둘(물량 빼고)이면 오류
if REWORK:
    for vid, floors in F.items():
        for fi, fl in enumerate(floors):
            for l in [l for t in fl['pools'] for l in t] + fl['elites']:
                fs = [feats(x) for x in l]
                for tag in 'PCDH':
                    if sum(tag in f for f in fs) > 1: errs.append(f'{vid} {fi + 1}층 {"+".join(l)}: 표지 {tag} 둘')
                if any(l.count(x) > 1 and x not in SWARM for x in l): errs.append(f'{vid} {fi + 1}층 {"+".join(l)}: 같은 몬스터 둘')
                if len(l) < 2: errs.append(f'{vid} {fi + 1}층 {l}: 몬스터 하나')
    print(f'묶음 고침 {len(LINE_LOG)}')
if errs:
    print('\n'.join(errs)); sys.exit(1)

# ── 쓰기(필드 단위 — 쓰기 직전에 다시 읽는다) ──
# 몬스터는 처음 나오는 마을 파일에 둔다(같은 몬스터를 여러 마을이 쓴다 — 정의는 하나).
order = ['erpien', 'furry', 'ghost', 'monatium', 'spirit', 'dragon']
home = {}
for vid in order:
    for x in sorted(used[vid]):
        home.setdefault(x, vid)
# 마을 줄에 없는 적(소환물)은 세운 적의 집에
for k in E:
    if k not in home:
        for s, e in E.items():
            if s in home and k in json.dumps(e, ensure_ascii=False): home[k] = home[s]; break
        else: errs.append(f'{k}: 어느 마을에도 없다')
if errs:
    print('\n'.join(errs)); sys.exit(1)

for vid in order:
    p = os.path.join(ROOT, vid + '.json')
    d = json.load(open(p, encoding='utf-8'))
    v = d['villages'][0]
    for fi, fl in enumerate(F[vid]):
        v['floors'][fi]['pools'] = fl['pools']
        v['floors'][fi]['elites'] = fl['elites']
        v['floors'][fi]['boss'] = fl['boss']
        v['floors'][fi].pop('bossElite', None)
    d['enemies'] = [E[k] for k in E if home[k] == vid]
    if not DRY:
        with open(p, 'w', encoding='utf-8') as f: json.dump(d, f, ensure_ascii=False, indent=2); f.write('\n')
    print(vid, '적', len(d['enemies']))

ap = os.path.join(ROOT, '_그림.json')
art = json.load(open(ap, encoding='utf-8'))
art = {k: v for k, v in art.items() if k.startswith('clone_') and k in E}
for k in E:
    art[k] = ART[k]
if not DRY:
    with open(ap, 'w', encoding='utf-8') as f: json.dump(art, f, ensure_ascii=False, indent=2); f.write('\n')

# 몬스터 표(문서용)
with open('C:/projects/bolzena-content-v2/_gen/foes/table.json', 'w', encoding='utf-8') as f:
    json.dump({k: {'name': E[k]['name'], 'mon': META[k][0], 'race': META[k][1], 'grade': META[k][2], 'tough': E[k].get('tough'), 'hp': E[k]['hp'],
                   'concept': META[k][3], 'src': META[k][4], 'home': home[k], 'icon': ART[k].get('icon'), 'spine': ART[k]['spine'], 'skin': ART[k].get('skin')} for k in E},
              f, ensure_ascii=False, indent=1)
print('적 합계', len(E), '(DRY)' if DRY else '')
