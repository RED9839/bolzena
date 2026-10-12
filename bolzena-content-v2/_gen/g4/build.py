# -*- coding: utf-8 -*-
"""교주 카드 · 장비 리뉴얼(2026-10-13 넷째) — 엘다인 20명 갈래에 맞물리게.

    python _gen/g4/build.py [출력 데이터 폴더]   (기본: 이 저장소 C:/projects/bolzena-content-v2)

src/ 는 리뉴얼 전 데이터(2026-10-13 백업 C:/projects/_backup/neutral_equip_20261013 과 같음 — g3 출력 그대로)다.
그 위에 아래 EQ · CARD 표의 손질만 얹어 world/equips/{무기,방어구,장신구}.json · world/neutral/교주카드.json 을 쓴다.
  · id · 이름 · 등급 · 칸 · 능력치 · 값은 그대로(그림짝 itemart.json · 저장 · 기록이 id 를 본다).
  · 표에 없는 장비 · 카드는 손대지 않는다.
원칙 · 전후 표: _measure/교주카드_장비_리뉴얼_20261013.md · 측정 도구: _gen/g4/측정/
"""
import copy, importlib.util, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'src')
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(HERE, '..', '..'))

# g3 의 효과 조각(st · dmg · rule · O …)을 그대로 쓴다 — main 은 돌리지 않는다.
_spec = importlib.util.spec_from_file_location('g3build', os.path.join(HERE, '..', 'g3', 'build.py'))
g3 = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(g3)
st, dmg, extra, shield, heal, draw, ap = g3.st, g3.dmg, g3.extra, g3.shield, g3.heal, g3.draw, g3.ap
tough, empower, mod, pull, power, rule, O, B, keep_tags = g3.tough, g3.empower, g3.mod, g3.pull, g3.power, g3.rule, g3.O, g3.B, g3.keep_tags
def flag(k): return {'k': k}   # ifKill · ifBreak · ifBroken 같은 조건 마디

# ── 장비 ────────────────────────────────────────────────────────────────
# id → (효과 규칙 목록, 설명 · None 이면 그대로). 엘다인 20명에 없는 태그(열정 · 회수 · 결속 · 주도 · 종극) 계기를 갈래 계기로.
ANY = {'who': 'any'}
EQ = {
    # 무기
    'eq_furnace': ([   # 용광검(전설) — 열정 계기 → 붕괴(격파)
        rule('turnStart', [mod('atkMod', 0.04, run=True)], '달아오르는 날'),
        rule('break', [mod('atkMod', 0.05, run=True)], '쇳물 붓기'),
    ], '싸울수록 달아오르는 검. 적이 무너질 때마다 한층 더 달아오릅니다.'),
    'eq_steelsword': ([   # 강철 대검(고급) — 쥐여 주기 +5.4~5.6%p(고급 가운데 가장 셈) → 비용 2 이상 공격 2장마다
        rule('play', [tough(1)], '무게 싣기', type='공격', minCost=2, every=2),
    ], None),
    'eq_firebow': ([   # 활활 불타활(고급) — 열정 계기 → 공격 3장마다(원작 30% 확률 × 2)
        rule('play', [st('그을림', 2, 'oneEnemy')], '불화살', type='공격', every=3),
    ], '세 발에 한 발은 불이 붙는 활. 맞은 자리가 한동안 그을립니다.'),
    'eq_cloakdagger': ([   # 망토와 단검(희귀) — 쥐여 주기 +6~8%p → 다음 카드 효과 +25% → +15%
        rule('play', [empower(0.15)], '망토 속 단검', maxCost=0, **ANY),
    ], None),
    # 방어구
    'eq_squishyvest': ([   # 말랑말랑한 조끼(전설) — 위기 한 번 → 실드가 깨질 때마다(탱커 피해 감소 갈래)
        rule('shieldBreak', [st('피해 감소', 1)], '눌린 자국'),   # 전투 시작 실드 80% 는 뺐다(둘이면 쥐여 주기 +6%p)
    ], '눌려도 다시 부풀어 오르는 조끼. 막이 터질 때마다 한 겹 더 폭신해집니다.'),
    'eq_rubbergauntlet': ([   # 고무고무 건틀릿(희귀) — 회수 계기 → 효과로 버리기(탄환 · 버리기 갈래)
        rule('discard', [shield(0.4)], '튕겨 돌아오기', **ANY),
    ], '던져도 튕겨 돌아오는 장갑. 손에서 내던진 만큼 막이 됩니다.'),
    'eq_safetybelt': ([   # 긴급 보호 벨트(희귀) — 쥐여 주기 +6~8%p(희귀 가운데 가장 셈) → 실드 150% → 100%
        rule('fightStart', [shield(1.0), st('실드 유지', 1)], '비상 실드'),
    ], None),
    # 장신구
    'eq_combatguide': ([   # 전투 지침서(희귀) — 전투 시작 약점 노출(첫 턴 모든 카드가 약점)이 쥐여 주기 +10~12%p → 격파한 적에게(격파 갈래)
        rule('break', [st('약점 노출', 2, 'oneEnemy')], '급소 복습'),
    ], '급소 그림이 가득한 지침서. 한 번 무너뜨린 적은 어디를 쳐야 할지 다 압니다.'),
    'eq_healpendant': ([   # 치유의 펜던트(희귀) — 쥐여 주기 +6~8%p → 회복 40% → 20%
        rule('play', [heal(0.2)], '피어나는 빛', type='공격'),
    ], None),
    'eq_volcanijuice': ([   # 볼케니카 특산 과일 주스(전설) — 턴 시작 실드 20% → 넘친 회복을 실드로(서포터 회복 갈래)
        rule('overheal', [{'k': 'shield', 'ratio': 1, 'ofEvent': 0.6, 'fixed': True}], '과즙 코팅', **ANY),
    ], '넘치게 마셔도 버릴 게 없는 주스. 차고 넘친 기운이 끈적한 막으로 남습니다.'),
    'eq_jadebook': ([   # 옥빛 마법서(희귀) — 전투 시작 드로우 → 생성 갈래
        rule('make', [empower(0.2, 'any')], '펼쳐 둔 장', **ANY),
    ], '펼쳐 두면 새 장이 저절로 채워지는 마법서. 새로 생긴 카드에 힘을 실어 줍니다.'),
    'eq_diabracelet': ([   # 다이아몬드 팔찌(고급) — 결속 계기 → 보존 카드를 쥐고 턴을 마치면(보존 갈래)
        rule('keepAp', [shield(0.6)], '겹쳐 박은 다이아', kind='keep'),
    ], '단단히 쥐고 있을수록 빛나는 팔찌. 손에 남겨 둔 카드만큼 막이 됩니다.'),
    'eq_worshipframe': ([   # 숭배용 액자(고급) — 종극 계기 → 파괴(처치 갈래)
        rule('kill', [heal(0.3)], '마지막 장면'),
    ], '적이 쓰러지는 장면을 담는 액자. 한 장 걸릴 때마다 모두 숨을 돌립니다.'),
    'eq_cutletpin': ([   # 돈까스 모양 머리핀(고급) — 주도 계기 → AP 를 남기고 턴을 마치면
        rule('keepAp', [st('다음 턴 드로우', 1)], '배꼽시계', kind='ap'),
    ], '배꼽시계가 울리면 손을 멈추고 쉽니다. 아껴 둔 만큼 다음 손이 넉넉합니다.'),
}

# ── 교주 카드 ───────────────────────────────────────────────────────────
CARD = {}
def card(cid):
    def deco(fn): CARD[cid] = fn; return fn
    return deco
def each(c):   # 기본 + 신탁 모두
    yield c
    for o in c.get('oracles', []): yield o

@card('n_coin')   # 랜덤 코인 — 탄환 키워드가 「사용하면 소멸」 을 품으니 소멸 태그를 걷는다(「소멸. 탄환.」 겹침)
def _(c):
    keep_tags(c, drop=('소멸',))

@card('n_rearguard')   # 후발대 — 회수 · 결속(엘다인에 없음)을 끌어오던 신탁 → 탄환 · 고유 카드
def _(c):
    c['oracles'][2] = O('신호탄', [pull('discard', n=2, tag='탄환')], tags=['소멸'])
    c['oracles'][4] = O('한 발만', [pull('discard', n=1, unique=True), draw(1)], tags=['소멸'])

@card('n_nat_lively')   # 성격의 카드[활발] — 신탁4 「주도 카드를 낼 때마다」(교주 카드뿐) → 매 턴 시작
def _(c):
    c['oracles'][3] = O('먼저 뛰기', [draw(2), st('협공', 1), power('먼저 뛰기', rule('turnStart', [st('협공', 1)]))], pw=True)

@card('n_meditate')   # 명상의 시간 — 신탁3 「종극 카드를 낼 때마다」(교주 카드뿐) → 아껴 쥐면 실드도
def _(c):
    c['oracles'][2] = O('호흡 세기', [st('집중', 1), power('호흡 세기', rule('keepAp', [st('다음 턴 드로우', 1), shield(0.5)]))])

# 개막 + 비용 0 = 전투마다 공짜로 도는 장비가 된다 → 0코 신탁을 다른 것으로(개막은 첫 턴 AP 를 치른다)
@card('n_potion')   # 수상한 물약
def _(c):
    c['oracles'][4] = O('쏟아진 병', [dmg(0.4, 'allEnemies'), st('고통', 2, 'allEnemies'), st('약화', 1, 'allEnemies')], tags=['개막', '소멸'])

@card('n_surprise')   # 깜짝 상자
def _(c):
    c['oracles'][4] = O('용수철 상자', [dmg(0.4, 'allEnemies'), st('둔화', 1, 'allEnemies'), draw(1)], tags=['개막', '소멸'])

@card('n_openrun')   # 오픈런 — 0코 「한정판」 → 처치 갈래(근면)
def _(c):
    c['oracles'][3] = O('한정판', [draw(2), st('다음 턴 드로우', 1), st('근면', 1)], tags=['개막', '소멸'])

@card('n_snap')   # 이성의 끈 — 개막이라 전투마다 HP 60 을 저절로 치러 판 내내 HP 가 샌다(쥐여 주기 −6~8%p) → HP 20 · 0코 「끝까지 가기」 → 불굴
def _(c):
    HP = {60: 20, 30: 10, 90: 40}
    for x in each(c):
        for f in x['fx']:
            if f.get('k') == 'payHp': f['v'] = HP[f['v']]
    o = c['oracles'][4]; o.pop('cost', None)
    o['fx'] = [f for f in o['fx'] if f.get('id') not in ('고동', '사기')] + [st('사기', 3), st('불굴', 1)]   # 효과 셋까지

@card('n_picnic')   # 파스텔빛 소풍 — 쥐여 주기 +7~8%p(카드 가운데 가장 셈) → 피해 · 회복 75%(85% 로는 +6.8%p)
def _(c):
    for x in each(c):
        for f in x['fx']:
            if f.get('k') == 'dmg': f['ratio'] = round(f['ratio'] * 0.75 * 20) / 20
            if f.get('k') == 'heal' and f['ratio'] >= 1: f['ratio'] = round(f['ratio'] * 0.75 * 20) / 20

@card('n_nat_gloomy')   # 성격의 카드[우울] — 손의 카드를 태우기만 하는 기본이 쥐여 주기 −4%p · 사람 31번 제시 1번 구매 → 실드 유지 대신 드로우 1(태운 만큼 채움)
def _(c):
    c['fx'] = [g3.burn(1), shield(1.2), draw(1)]
    o = c['oracles']
    o[1] = O('깊은 우울', [g3.burn(1), shield(1.8), draw(1)])
    o[3] = O('그늘', [shield(1.2), draw(1), power('그늘', rule('exhaust', [st('결정화', 1)], who='any'))], pw=True)
    o[4] = O('작은 한숨', [g3.burn(1), shield(1.2), draw(1)], cost=0)

@card('n_leave')   # 조기 퇴근 — 0코 「마지막 결재」 피해 180% 그대로는 0코 공격 가운데 가장 셈
def _(c):
    for f in c['oracles'][4]['fx']:
        if f.get('k') == 'dmg': f['ratio'] = 1.1

@card('n_bully')   # 약자무시 — 사람 31번 제시 · 0번 구매, 쥐여 주기 −3%p. 처치 갈래(근면 · 에르핀 · 란)로: 쓰러뜨리면 AP · 드로우
def _(c):
    K = lambda *more: [flag('ifKill'), ap(1)] + list(more)
    c['fx'] = [dmg(1.1)] + K(draw(1))
    c['oracles'] = [
        O('비웃기', [dmg(1.1), st('취약', 2, 'oneEnemy')] + K()),
        O('짓밟기', [dmg(1.5)] + K(draw(1))),
        O('약점 찌르기', [st('취약', 1, 'oneEnemy'), {'k': 'perDebuff'}, dmg(1.0)] + K()),
        O('줄 세우기', [dmg(1.0, 'allEnemies')] + K()),
        O('콧방귀', [dmg(0.8)] + K(), cost=0),
    ]
    c['blurb'] = '약한 녀석부터 골라 쓰러뜨립니다. 쓰러뜨리면 손이 빨라집니다.'

@card('n_critical')   # 회심의 일격 — 사람 21번 제시 · 0번 구매. 격파 갈래(아야 · 네르)로: 충격 → 강인도 피해 + 붕괴 드로우.
# 회수 3 에 강인도를 얹으면 네 번 나가 쥐여 주기 +6~7%p → 회수는 신탁 「다시 노리기」 하나로
def _(c):
    B_ = lambda r=1.0, t=1, t_='oneEnemy': [dmg(r, t_), tough(t, None if t_ == 'oneEnemy' else t_), flag('ifBreak'), draw(1)]
    c['tags'] = []
    c['fx'] = B_()
    c['oracles'] = [
        O('번쩍', B_(t=2)),
        O('틈새', B_(r=1.5)),
        O('다시 노리기', [dmg(0.9), tough(1)], tags=['회수 3']),
        O('모두에게 한 번씩', B_(r=0.9, t_='allEnemies')),
        O('노리고 기다리기', B_(r=0.8), cost=0),
    ]
    c['blurb'] = '노린 데로 정확히. 버티던 적이 무너지면 다음 수가 손에 잡힙니다.'

@card('n_recovery')   # 효율적인 회복 — 신탁3 「넘친 회복마다 결정화 1」 은 결정화가 끝없이 쌓인다 → 실드
def _(c):
    c['oracles'][2] = O('식단표', [g3.cheaper(1), power('식단표', rule('overheal', [shield(0.5)], who='any'))], pw=True)

# ── 만들기 ──────────────────────────────────────────────────────────────
def load(name): return json.load(open(os.path.join(SRC, name), encoding='utf-8'))
save = g3.save

def main():
    seen = set()
    for slot in ('무기', '방어구', '장신구'):
        d = load(slot + '.json')
        for e in d['equips']:
            seen.add(e['id'])
            if e['id'] in EQ:
                eff, blurb = EQ[e['id']]
                e['effect'] = copy.deepcopy(eff)
                if blurb: e['blurb'] = blurb
        save(os.path.join(OUT, 'world', 'equips', slot + '.json'), d)
    miss = set(EQ) - seen
    if miss: raise SystemExit(f'EQ 표에 없는 장비 id: {miss}')
    d = load('교주카드.json')
    miss = set(CARD) - {c['id'] for c in d['cards']}
    if miss: raise SystemExit(f'CARD 표에 없는 카드 id: {miss}')
    for c in d['cards']:
        if c['id'] in CARD: CARD[c['id']](c)
    save(os.path.join(OUT, 'world', 'neutral', '교주카드.json'), d)
    print('장비', len(seen), f'(손질 {len(EQ)})', '· 교주 카드', len(d['cards']), f'(손질 {len(CARD)})', '→', OUT)

if __name__ == '__main__':
    main()
