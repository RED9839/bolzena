// 손 만들기형 14명 — 카드를 만들고 · 뽑고 · 되돌려 손을 굴린다.
// 공용 계기(이 방식 안에서 주고받는 것): ① 0코 「만든 카드」 — 같은 성격 카드라 「연속:」 의 다리가 된다
// ② 「영감:」 — 효과 드로우가 깨운다(만드는 쪽이 드로우를 뿌리고 쓰는 쪽 카드에 영감이 붙는다).
import { d, dd, all, rnd, blk, sh, heal, st, stk, spend, draw, ap, make, cheap, tough, gauge, rushDown, cleanse, strip,
  chain, brk, tune, insp, has, hasNot, per, atkRun, defRun, critRun, taken, O, B, P, turn1, turnN, fight1, hero, U, TOK, write } from './lib.mjs';

const DIR = 'C:/projects/bolzena-content/heroes/make';
const STYLE = '손 만들기형';
const out = {};

// ───────────────────────── 순수 ─────────────────────────
// 스패럿 — 만드는 쪽. 디버프를 걸 때마다 전리품을 챙기고, 전리품을 「금은보화」 로 바꿔 파티에 나눠 준다.
{
  const k = '스패럿', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '오색앵무 해적 선장. 털어 온 전리품을 금은보화 카드로 바꿔 부하들 손에 쥐여 준다.',
    keyword: { name: '전리품', desc: '털어 온 금은보화 — 선장이 나눠 줄 몫', carrier: 'self', cap: 5 },
    passives: [
      P('약탈', { on: 'debuff' }, [stk('전리품', 1)], null, turnN(2)),
      P('출항 의식', { on: 'fightStart' }, [make(T1, 1)]),
    ],
    ult: { name: '야아알~! 돌격!', cost: 200, fx: [rnd(0.5, 4), st('약화', 1, 'allEnemies'), make(T1, 2)] },
    cards: [
      TOK(T1, '금은보화', 0, '스킬', ['소멸'], [draw(1), chain, ap(1)], '선장이 통 크게 나눠 준 몫. 받은 녀석은 한 수 더 움직인다'),
      U(`${k}_u1`, '오늘은 저녀석이다!', 1, '공격', [], [d(0.4, 3), st('약화', 1), make(T1, 1)], [
        O('앵무새 정찰', null, ['보존'], [d(0.45, 3), st('약화', 1), make(T1, 1)]),
        O('해적의 낙인', 2, [], [d(0.6, 4), st('약화', 2), make(T1, 2)]),
        O('부하들아 저놈이다', null, [], [d(0.4, 3), st('약화', 1), make(T1, 2)]),
        O('보물 냄새', null, [], [d(0.4, 3), st('약화', 1), stk('전리품', 2)]),
        O('볼레!', null, [], [d(0.4, 3), st('약화', 3), make(T1, 1)]),
      ], [B('두 마리 앵무새', 'power'), B('사냥감 지목', null, [], ['개전']), B('보물 지도', null, [stk('전리품', 1)])],
      { signature: true, blurb: '앵무새를 보내 쪼아 대고 낙인을 찍는다. 그 녀석 몫은 벌써 부하들 차지다' }),
      U(`${k}_u2`, '약탈한 전리품', 1, '스킬', [], [heal(1.5), has('전리품', 2), spend('전리품', 2), make(T1, 2)], [
        O('가벼운 보따리', 0, [], [has('전리품', 2), spend('전리품', 2), make(T1, 2)]),
        O('보물 상자째로', 2, [], [heal(3.0), has('전리품', 2), spend('전리품', 2), make(T1, 3)]),
        O('나눠 갖는 해적', null, [], [heal(1.5), stk('전리품', 1), make(T1, 2)]),
        O('금은보화 산더미', null, [], [heal(1.8), has('전리품', 2), spend('전리품', 2), make(T1, 3)]),
        O('찬란한 금은보화', null, [], [defRun(0.15), heal(1.5), make(T1, 2)], true),
      ], [B('사략 해적의 몫', 'heal'), B('빵주 선장실', null, [], ['보존']), B('아이들 몫은 따로', null, [sh(0.6)])],
      { blurb: '빼앗은 건 부하들과 나눈다. 겉으로는 악당인 척해도' }),
      U(`${k}_u3`, '해적의 언어', 1, '공격', ['연계'], [dd(0.6), st('약화', 2)], [
        O('짧은 은어', 0, ['연계'], [st('약화', 2), stk('전리품', 1)]),
        O('온갖 욕설', null, ['연계'], [dd(0.6), st('약화', 2, 'allEnemies')]),
        O('선장의 호령', null, ['연계'], [dd(0.6), st('약화', 2), make(T1, 1)]),
        O('뱃고동 소리', null, ['연계'], [dd(0.8), st('약화', 2), stk('전리품', 1)]),
        O('갈고리 의수', null, ['연계'], [dd(0.6), st('약화', 2), st('손상', 2)]),
      ], [B('야아알!', 'power'), B('요호호!', null, [stk('전리품', 1)]), B('소음 공해', null, [], ['개전'])],
      { blurb: '부하가 칼을 뽑으면 선장은 옆에서 알아듣지도 못할 은어를 쏟아붓는다' }),
      U(`${k}_u4`, '플라즈마 포 일제사격', 2, '공격', ['분쇄'], [dd(0.9, 1, 'allEnemies'), st('약화', 1, 'allEnemies'), chain, make(T1, 1)], [
        O('한 발만 장전', 1, ['분쇄'], [dd(0.7, 1, 'allEnemies'), st('약화', 1, 'allEnemies'), chain, make(T1, 1)]),
        O('전 포문 개방', 3, ['분쇄'], [dd(1.6, 1, 'allEnemies'), st('약화', 2, 'allEnemies'), make(T1, 2)]),
        O('돌격 신호탄', null, ['분쇄'], [dd(0.9, 1, 'allEnemies'), st('약화', 1, 'allEnemies'), make(T1, 2)]),
        O('배신자 몫까지', null, ['분쇄'], [dd(0.9, 1, 'allEnemies'), st('약화', 2, 'allEnemies'), chain, make(T1, 2)]),
        O('선상반란 진압', null, ['분쇄'], [dd(1.2, 1, 'allEnemies'), st('손상', 1, 'allEnemies'), chain, make(T1, 1)]),
      ], [B('독수리의 후예', 'power'), B('앵무 해적단', 'cost'), B('깃털의 후예', null, [rushDown(1, 'allEnemies')])],
      { blurb: '마지막 해적질로 배신자들을 날려 버린 그 포다. 방패를 든 녀석부터 녹는다' }),
    ],
  });
}

// 헤일리 — 쓰는 쪽. 느닷없이 작전을 선포한다 — 플랜 A · B · C 를 손에 깔고 하나를 고르면 나머지는 접는다(연결).
{
  const k = '헤일리', A = `${k}_t1`, Bp = `${k}_t2`, Cp = `${k}_t3`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '지구 침공의 엘프 선봉 장교. 느닷없이 작전을 선포하고, 손에 깔린 플랜 가운데 하나를 골라 밀어붙인다.',
    keyword: { name: '작전', desc: '선포해 둔 작전 — 다음 일격에 지휘가 실린다', carrier: 'self', cap: 3, consumeAll: true, per: [{ stat: 'dealt', v: 0.15 }] },
    passives: [
      P('작전 선포', { on: 'fightStart' }, [make(A, 1), make(Bp, 1), make(Cp, 1)]),
      P('경례', { on: 'play', every: 3, minCost: 0 }, [stk('작전', 1)]),
    ],
    ult: { name: '플랜 B', cost: 250, fx: [all(1.2), st('고통', 3, 'allEnemies'), stk('작전', 3)] },
    cards: [
      TOK(A, '플랜 A: 정면 돌파', 0, '공격', ['연결', '소멸'], [d(0.9), stk('작전', 1)], '돌격 앞으로. 나머지 플랜은 접는다'),
      TOK(Bp, '플랜 B: 포위 섬멸', 0, '공격', ['연결', '소멸'], [st('고통', 2, 'allEnemies'), stk('작전', 1)], '적을 둘러싸고 천천히 조인다'),
      TOK(Cp, '플랜 C: 전략적 후퇴', 0, '스킬', ['연결', '소멸'], [blk(1.5), draw(1)], '물러나 다시 짠다. 부하들을 살리는 게 먼저다'),
      U(`${k}_u1`, '논 그라타', 2, '공격', [], [d(0.35, 3, 'allEnemies'), st('고통', 1, 'allEnemies'), chain, stk('작전', 2)], [
        O('경례 후 돌입', null, ['개전'], [d(0.4, 3, 'allEnemies'), st('고통', 1, 'allEnemies'), chain, stk('작전', 2)]),
        O('전 함대 집중', 3, [], [d(0.6, 3, 'allEnemies'), st('고통', 2, 'allEnemies'), stk('작전', 3)]),
        O('아군기지 보호작전', null, [], [d(0.35, 3, 'allEnemies'), st('피해 감소', 2), chain, stk('작전', 2)]),
        O('작전명 선포', null, [], [d(0.35, 3, 'allEnemies'), st('고통', 1, 'allEnemies'), make(A, 1)]),
        O('쓰라린 훈계', null, [], [d(0.45, 3, 'allEnemies'), st('고통', 3, 'allEnemies'), chain, stk('작전', 2)]),
      ], [B('엘프 선봉', 'power'), B('기함의 포문', 'weakSpot'), B('앨리어스 동맹', null, [stk('작전', 1)])],
      { signature: true, blurb: '침공군의 선봉이 내리꽂는 일제 사격. 앞 작전이 맞아떨어지면 다음 작전이 곧장 선다' }),
      U(`${k}_u2`, '작전 브리핑', 1, '스킬', [], [make(A, 1), make(Bp, 1), make(Cp, 1)], [
        O('긴급 브리핑', 0, [], [make(A, 1), make(Bp, 1)]),
        O('전군 작전 회의', 2, [], [make(A, 2), make(Bp, 2), make(Cp, 1)]),
        O('플랜 A 고수', null, [], [make(A, 2), make(Bp, 1), make(Cp, 1)]),
        O('작전 지도', null, ['보존'], [make(A, 1), make(Bp, 1), make(Cp, 2)]),
        O('그럴듯한 오판', null, [], [make(A, 2), make(Bp, 1), draw(1)]),
      ], [B('정신이 맑은 날', 'draw'), B('비상 소집', null, [], ['개전']), B('원칙주의자', null, [stk('작전', 1)])],
      { blurb: '느닷없는 작전 선포. 부하들은 무슨 작전인지 모르지만 일단 경례부터 붙인다' }),
      U(`${k}_u3`, '플랜 A: 돌파 개시', 1, '공격', [], [d(1.2), chain, ap(1), insp, make(A, 1)], [
        O('돌파 직전', 0, [], [d(0.7), chain, ap(1), insp, make(A, 1)]),
        O('전면 돌격', 2, [], [d(2.8), chain, ap(1), insp, make(A, 1)]),
        O('포로는 정중히', null, [], [d(1.2), st('약화', 2), chain, ap(1)]),
        O('다음 작전 준비', null, [], [d(1.2), stk('작전', 1), chain, ap(1)]),
        O('뇌운 돌파', null, [], [d(1.5), chain, ap(1), insp, make(A, 1)]),
      ], [B('채찍 끝', 'power'), B('선봉의 발', 'ap'), B('외계 알 감별', null, [st('취약', 1)])],
      { blurb: '돌파가 뚫리면 다음 작전이 숨 돌릴 틈 없이 이어진다' }),
      U(`${k}_u4`, '엘프군 최전선', 2, '강화', [], [atkRun(0.15), stk('작전', 3)], [
        O('현역 복귀', null, ['개전'], [atkRun(0.15), stk('작전', 3)]),
        O('최전선 사령관', 3, [], [atkRun(0.2), stk('작전', 3), make(A, 2)]),
        O('부하들을 위해', null, [], [atkRun(0.15), stk('작전', 3), st('피해 감소', 2)]),
        O('작전 지휘관', null, [], [atkRun(0.15), make(A, 1), make(Bp, 1)]),
        O('선한 원칙주의자', 1, [], [atkRun(0.1), stk('작전', 3)]),
      ], [B('훈장', 'atkUp'), B('경례!', null, [], ['개전']), B('작전 일지', null, [draw(1)])],
      { blurb: '정신이 맑을 땐 누구보다 선한 원칙주의자. 지휘봉을 쥐면 부하들이 먼저 움직인다' }),
    ],
  });
}

// 셰럼 — 만드는 쪽(탱커). 맞은 장면까지 받아 적는다 — 피해를 받으면 「흑역사 원고」 가 손에 들어오고, 원고 속 인물이 걸어 나와 싸운다.
{
  const k = '셰럼', T1 = `${k}_t1`, T2 = `${k}_t2`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '궁정 서기관 마녀. 맞은 장면까지 받아 적고, 원고 속 인물을 현실로 불러낸다.',
    keyword: { name: '집필', desc: '받아 적는 중인 원고의 쪽수', carrier: 'self', cap: 5 },
    passives: [
      P('받아 적기', { on: 'hurt' }, [make(T1, 1)], null, turn1),
      P('사료 인용', { on: 'play', type: '공격', every: 2, minCost: 0 }, [stk('집필', 1)]),
    ],
    ult: { name: '생생한 흑역사', cost: 250, fx: [dd(0.3, 6, 'allEnemies'), st('약화', 2, 'allEnemies'), make(T2, 2)] },
    cards: [
      TOK(T1, '흑역사 원고', 0, '공격', ['소멸'], [dd(0.5), chain, blk(1.0)], '방금 맞은 장면을 그대로 적었다. 읽히면 아프다'),
      TOK(T2, '걸어 나온 어둠 정령', 0, '공격', ['소멸'], [dd(0.8), st('약화', 1)], '소설 「어둠 정령의 상처」 의 주인공. 작가보다 진지하다'),
      U(`${k}_u1`, '위치 아카이브', 2, '스킬', [], [sh(3.0), make(T1, 2)], [
        O('간추린 기록', 1, [], [sh(2.0), make(T1, 1)]),
        O('왕실 사료 전권', 3, [], [sh(4.5), make(T1, 2), make(T2, 1)]),
        O('기밀 문서', null, [], [sh(3.0), make(T1, 1), make(T2, 1)]),
        O('흑역사 보관함', null, [], [sh(3.0), make(T1, 3)]),
        O('여왕 폐하 열람용', null, [], [sh(3.6), make(T1, 2)]),
      ], [B('두꺼운 표지', 'guard'), B('색인', null, [], ['보존']), B('각주', null, [stk('집필', 1)])],
      { signature: true, blurb: '마녀 왕실의 모든 일을 적어 둔 서고. 펼치면 그날의 흑역사가 쏟아진다' }),
      U(`${k}_u2`, '어둠 정령의 상처', 2, '공격', [], [per('집필'), dd(0.3, 1, 'allEnemies'), spend('집필', 'all'), make(T2, 1)], [
        O('습작', 1, [], [per('집필'), dd(0.25, 1, 'allEnemies'), spend('집필', 'all'), make(T2, 1)]),
        O('완결편', 3, [], [per('집필'), dd(0.5, 1, 'allEnemies'), spend('집필', 'all'), make(T2, 2)]),
        O('작가의 말', null, [], [dd(0.5, 1, 'allEnemies'), per('집필'), dd(0.3, 1, 'allEnemies'), spend('집필', 'all')]),
        O('2쇄 증보판', null, [], [per('집필'), dd(0.3, 1, 'allEnemies'), spend('집필', 'all'), make(T2, 2)]),
        O('비밀 동인지', null, ['보존'], [per('집필'), dd(0.35, 1, 'allEnemies'), spend('집필', 'all'), make(T2, 1)]),
      ], [B('클리셰 폭발', 'power'), B('삽화', null, [st('약화', 1, 'allEnemies')]), B('연재 마감', 'cost')],
      { blurb: '몰래 쓴 소설의 인물들이 원고 밖으로 걸어 나온다. 적어 둔 쪽수만큼 진하게' }),
      U(`${k}_u3`, '미행과 잠입', 1, '스킬', [], [blk(2.5), stk('집필', 1), insp, make(T1, 1)], [
        O('숨어 엿보기', 0, [], [blk(1.3), stk('집필', 1), insp, make(T1, 1)]),
        O('장기 잠입', 2, [], [blk(4.5), stk('집필', 2), insp, make(T1, 2)]),
        O('현장 기록', null, [], [blk(2.5), make(T1, 1), insp, make(T1, 1)]),
        O('발소리 지우기', null, [], [blk(2.5), st('피해 감소', 1), insp, make(T1, 1)]),
        O('목격 진술', null, [], [blk(3.0), stk('집필', 2), insp, make(T1, 1)]),
      ], [B('검은 망토', 'guard'), B('메모장', 'draw'), B('발끝 걸음', null, [], ['개전'])],
      { blurb: '미행 · 잠입 · 받아쓰기. 들키면 맞는 것까지 기록에 넣는다' }),
      U(`${k}_u4`, '서기관의 기록법', 1, '강화', [], [defRun(0.15), stk('집필', 2)], [
        O('정리된 서가', null, ['개전'], [defRun(0.15), stk('집필', 2)]),
        O('궁정 서기관', 2, [], [defRun(0.2), stk('집필', 3), make(T2, 2)]),
        O('기록체', null, [], [defRun(0.15), stk('집필', 2), st('반격', 1)]),
        O('사료와 전례', null, [], [defRun(0.15), make(T1, 2)]),
        O('흑역사 수집가', null, [], [atkRun(0.2), stk('집필', 2)]),
      ], [B('함.', 'defUp'), B('인 듯함.', null, [], ['개전']), B('정중한 존댓말', null, [blk(1.0)])],
      { blurb: '「~함」 하고 바뀌는 기록체. 모든 일은 사료가 된다' }),
    ],
  });
}

// 오팔 — 만드는 쪽(서포터). 울다가 뚝 그치고 탭댄스 — 「탭」 카드를 깔고, 탭을 이어 밟을수록 손이 빨라진다(연속: 드로우).
{
  const k = '오팔', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '반짝이는 아기 용. 울음이 뚝 그치면 탭댄스가 시작되고, 스텝이 이어질수록 파티의 손이 빨라진다.',
    keyword: { name: '울먹', desc: '터지기 직전의 울음 — 그치는 순간 스텝이 된다', carrier: 'self', cap: 3 },
    passives: [
      P('울다 뚝', { on: 'hurt' }, [stk('울먹', 1)], null, turn1),
      P('선배님~!', { on: 'turnStart' }, [spend('울먹', 'all'), make(T1, 1)], [{ c: 'stack', id: '울먹', n: 1 }]),
    ],
    ult: { name: '오팔 파우더', cost: 200, fx: [sh(3.0), st('약화', 2, 'allEnemies'), make(T1, 3)] },
    cards: [
      TOK(T1, '탭', 0, '스킬', ['소멸'], [sh(0.6), chain, draw(1)], '딱! 다음 스텝은 더 빠르다'),
      U(`${k}_u1`, '가십 드래곤', 2, '스킬', [], [sh(2.5), st('약화', 1, 'allEnemies'), make(T1, 2)], [
        O('귓속말', 1, [], [sh(1.5), st('약화', 1, 'allEnemies'), make(T1, 1)]),
        O('소문이 소문을', 3, [], [sh(4.0), st('약화', 2, 'allEnemies'), make(T1, 3)]),
        O('에헤 에헤', null, [], [sh(2.5), st('약화', 1, 'allEnemies'), make(T1, 3)]),
        O('어허!', null, [], [sh(2.5), st('약화', 2, 'allEnemies'), make(T1, 2)]),
        O('반짝이는 비늘', null, [], [sh(3.2), st('약화', 1, 'allEnemies'), make(T1, 2)]),
      ], [B('보석 반사', 'guard'), B('선배님 이야기', null, [draw(1)]), B('눈물 자국', null, [], ['보존'])],
      { signature: true, blurb: '들은 이야기를 다 퍼뜨리는 아기 용. 소문이 돌수록 발도 빨라진다' }),
      U(`${k}_u2`, '탭댄스', 1, '스킬', [], [make(T1, 3)], [
        O('한 스텝', 0, [], [make(T1, 2)]),
        O('쇼 타임', 2, [], [make(T1, 5), sh(1.0)]),
        O('뚝 그치고', null, [], [make(T1, 3), sh(1.0)]),
        O('박수 유도', null, [], [make(T1, 3), draw(1)]),
        O('구두 소리', null, ['보존'], [make(T1, 3), sh(0.6)]),
      ], [B('반짝 구두', 'cost'), B('리듬 타기', null, [sh(0.6)]), B('앙코르', null, [], ['개전'])],
      { blurb: '울다 말고 갑자기 춤춘다. 바닥을 딱딱 밟는 소리에 다들 정신을 차린다' }),
      U(`${k}_u3`, '보석 던지기', 2, '공격', [], [rnd(0.5, 4), chain, make(T1, 1)], [
        O('작은 원석', 1, [], [rnd(0.5, 2), chain, make(T1, 1)]),
        O('보석함 통째로', 3, [], [rnd(0.55, 6), make(T1, 2)]),
        O('오팔 파편', null, [], [rnd(0.5, 4), st('약화', 1, 'allEnemies'), chain, make(T1, 1)]),
        O('던지고 또 던지고', null, [], [rnd(0.5, 5), chain, make(T1, 1)]),
        O('반짝 반짝', null, [], [rnd(0.5, 4), make(T1, 2)]),
      ], [B('정조준', 'power'), B('보석 가루', 'frost'), B('줍기', null, [draw(1)])],
      { blurb: '아끼던 보석을 마구 던진다. 던지고 나서 운다' }),
      U(`${k}_u4`, '고무줄 총', 1, '공격', [], [d(1.0), st('약화', 1), insp, make(T1, 2)], [
        O('튕기기', 0, [], [d(0.6), st('약화', 1), insp, make(T1, 2)]),
        O('왕고무줄', 2, [], [d(2.2), st('약화', 2), insp, make(T1, 2)]),
        O('연발', null, [], [d(0.55, 2), st('약화', 1), insp, make(T1, 2)]),
        O('놀이터 대장', null, [], [d(1.0), make(T1, 1), insp, make(T1, 2)]),
        O('강화 고무줄', null, [], [atkRun(0.1), d(1.0), make(T1, 1)], true),
      ], [B('팽팽하게', 'power'), B('선배님 겨냥', 'weakSpot'), B('주머니 속 구슬', null, [], ['보존'])],
      { blurb: '아기 용의 장난감. 맞으면 아프고, 손에 잡히면 신이 난다' }),
    ],
  });
}

// 마고 — 만드는 쪽(서포터). 부르면 모여드는 목장 친구들 — 아기 염소(공격) · 아기 양(회복) · 아기 병아리(드로우)를 골라 부른다.
{
  const k = '마고', G = `${k}_t1`, S = `${k}_t2`, C = `${k}_t3`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '산양 목장주. 부르면 동물 친구들이 손에 모여들고, 아이들을 건드리면 비스트 로드가 된다.',
    keyword: { name: '목장', desc: '울타리 안에 모인 친구들의 수', carrier: 'self', cap: 3 },
    passives: [
      P('아침 먹이', { on: 'turnStart' }, [make(C, 1)], [{ c: 'firstTurn' }]),
      P('비스트 로드', { on: 'lowHp', pct: 0.5 }, [make(G, 2), st('불굴', 1)]),
    ],
    ult: { name: '메⋯에~류겐!', cost: 300, fx: [dd(3.0), tough(3), make(G, 2)] },
    cards: [
      TOK(G, '아기 염소', 0, '공격', ['소멸'], [dd(0.6), chain, tough(1)], '작은 뿔로 들이받는다. 마고를 쏙 닮았다'),
      TOK(S, '아기 양', 0, '스킬', ['소멸'], [heal(1.0), chain, cleanse(1)], '폭신한 털에 기대면 아픈 게 덜하다'),
      TOK(C, '아기 병아리', 0, '스킬', ['소멸'], [draw(1), chain, sh(0.6)], '삐약. 모이를 찾아 이리저리'),
      U(`${k}_u1`, '마고 마구 회복해', 2, '스킬', [], [heal(3.0), make(S, 1), make(C, 1)], [
        O('토닥토닥', 1, [], [heal(2.0), make(S, 1)]),
        O('목장 대잔치', 3, [], [heal(4.5), make(S, 2), make(C, 1)]),
        O('양털 담요', null, [], [heal(3.0), make(S, 2)]),
        O('아이들 먼저', null, [], [heal(3.6), make(S, 1), make(C, 1)]),
        O('목장 식구', null, [], [heal(3.0), make(G, 1), make(C, 2)]),
      ], [B('햇살 목초지', 'heal'), B('울타리 손질', null, [sh(0.6)]), B('따뜻한 우유', null, [], ['보존'])],
      { signature: true, blurb: '아픈 아이를 보면 마고는 일단 다 끌어안는다. 동물 친구들도 따라 모여든다' }),
      U(`${k}_u2`, '사료가 제일 맛있어', 1, '스킬', [], [make(G, 1), make(S, 1), make(C, 1)], [
        O('한 줌 사료', 0, [], [make(C, 1), make(G, 1)]),
        O('사료 포대', 2, [], [make(G, 2), make(S, 2), make(C, 1)]),
        O('염소 사료', null, [], [make(G, 2), make(S, 1), make(C, 1)]),
        O('양 사료', null, [], [make(G, 1), make(S, 2), make(C, 1)]),
        O('모이 뿌리기', null, [], [make(G, 1), make(S, 1), make(C, 2)]),
      ], [B('특제 사료', 'draw'), B('사료 창고', null, [], ['보존']), B('휘파람', null, [], ['개전'])],
      { blurb: '사료 통을 흔들면 어디선가 다 모여든다. 마고도 한 입 먹는다' }),
      U(`${k}_u3`, '내 목장에서 나가', 2, '공격', [], [dd(1.5), tough(1), chain, make(G, 1)], [
        O('경고', 1, [], [dd(0.9), tough(1), chain, make(G, 1)]),
        O('영역 선포', 3, [], [dd(2.5), tough(2), make(G, 2)]),
        O('뿔 세우기', null, ['분쇄'], [dd(1.5), tough(1), chain, make(G, 1)]),
        O('목장 지킴이', null, [], [dd(1.5), tough(1), make(G, 2)]),
        O('낫 빼앗기', null, [], [dd(1.5), strip(), tough(1)]),
      ], [B('단단한 뿔', 'power'), B('낮아진 목소리', null, [st('약화', 1)]), B('발굽', null, [tough(1)])],
      { blurb: '영역 이야기만 나오면 목소리가 낮아진다. 그다음은 뿔이다' }),
      U(`${k}_u4`, '아기 동물 돌보기', 1, '강화', [], [defRun(0.15), make(S, 1), make(C, 1)], [
        O('아침 산책', null, ['개전'], [defRun(0.15), make(S, 1), make(C, 1)]),
        O('목장주의 하루', 2, [], [defRun(0.2), make(S, 2), make(G, 2)]),
        O('예방 접종', null, [], [defRun(0.15), make(S, 2), cleanse(1)]),
        O('셋 이상은 불편해', null, [], [defRun(0.2), make(C, 2)]),
        O('목동의 지팡이', null, [], [atkRun(0.15), make(G, 1), make(C, 1)]),
      ], [B('조용한 성격', 'defUp'), B('히힛', null, [], ['개전']), B('건초 더미', null, [sh(0.6)])],
      { blurb: '셋 이상 모이면 불편해하면서도 동물 친구들은 늘 곁에 둔다' }),
    ],
  });
}

// 스피키 — 쓰는 쪽(서포터). 남의 정체성을 뒤집어쓴다 — 같은 성격의 카드에 이어 내면(연속) 앞 사람 흉내를 한 번 더.
{
  const k = '스피키', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '정체성의 유령. 방금 지나간 동료를 흉내 내며 끼어들고, 호박 친구만큼은 목숨처럼 아낀다.',
    keyword: { name: '변장', desc: '뒤집어쓴 남의 얼굴 — 다음 일격은 그 사람인 척', carrier: 'self', cap: 3, consumeAll: true, per: [{ stat: 'dealt', v: 0.2 }] },
    passives: [
      P('흉내쟁이', { on: 'play', nth: 4, who: 'any' }, [make(T1, 1), stk('변장', 1)]),
      P('호박 친구', { on: 'fightStart' }, [make(T1, 1)]),
    ],
    ult: { name: '트릭 오어 트릿~☆', cost: 200, fx: [st('피해 감소', 2), heal(3.0), stk('변장', 3)] },
    cards: [
      TOK(T1, '호박 친구', 0, '공격', ['소멸'], [d(0.7), chain, heal(0.6)], '스피키의 둘도 없는 친구. 머리로 들이받는다'),
      U(`${k}_u1`, '펌킨 매직', 2, '스킬', [], [heal(2.5), make(T1, 1), chain, stk('변장', 2)], [
        O('작은 호박', 1, [], [heal(1.6), make(T1, 1), chain, stk('변장', 1)]),
        O('호박 마차', 3, [], [heal(4.0), make(T1, 2), chain, stk('변장', 3)]),
        O('잭 오 랜턴', null, [], [heal(2.5), make(T1, 2), chain, stk('변장', 2)]),
        O('최면 걸기', null, [], [heal(2.5), stk('변장', 2), chain, draw(2)]),
        O('완벽한 분장', null, [], [heal(3.0), make(T1, 1), chain, stk('변장', 3)]),
      ], [B('달콤한 사탕', 'heal'), B('호박 수프', null, [sh(0.6)]), B('흐에엥', null, [], ['보존'])],
      { signature: true, blurb: '호박 친구를 부르면 다들 기운이 난다. 앞 사람 흉내까지 내면 더' }),
      U(`${k}_u2`, '사제장 대리', 1, '스킬', [], [draw(1), chain, ap(1), stk('변장', 1)], [
        O('대리 서명', 0, [], [draw(1), chain, ap(1)]),
        O('사제장 행세', 2, [], [draw(3), chain, ap(2), stk('변장', 1)]),
        O('네르 흉내', null, [], [draw(2), chain, ap(1), stk('변장', 1)]),
        O('마요 흉내', null, [], [draw(1), make(T1, 1), chain, ap(1)]),
        O('인식 비틀기', null, ['보존'], [draw(1), chain, ap(1), stk('변장', 2)]),
      ], [B('어설픈 분장', 'ap'), B('목소리 흉내', null, [draw(1)]), B('가짜 사제복', null, [], ['개전'])],
      { blurb: '앞사람이 한 일을 그대로 따라 한다. 어설픈 분장도 완벽해 보인다' }),
      U(`${k}_u3`, '호박 바구니', 1, '스킬', [], [make(T1, 2), insp, stk('변장', 1)], [
        O('호박 한 개', 0, [], [make(T1, 1), insp, stk('변장', 1)]),
        O('호박밭', 2, [], [make(T1, 4), insp, stk('변장', 2)]),
        O('잘 익은 호박', null, [], [make(T1, 2), heal(1.0), insp, stk('변장', 1)]),
        O('바구니 가득', null, [], [make(T1, 3), insp, stk('변장', 1)]),
        O('호박 등불', null, ['보존'], [make(T1, 2), stk('변장', 1)]),
      ], [B('친구 부르기', 'draw'), B('밭 지키기', null, [sh(0.6)]), B('리본 달기', null, [], ['개전'])],
      { blurb: '바구니 속 호박들이 하나같이 친구다. 하나라도 건드리면 큰일 난다' }),
      U(`${k}_u4`, '한 번 맞아보실래요오?', 2, '공격', [], [d(1.2), chain, st('약화', 2)], [
        O('콩 한 대', 1, [], [d(1.0), chain, st('약화', 2)]),
        O('작정하고', 3, [], [d(2.8), chain, st('약화', 3), stk('변장', 2)]),
        O('입이 험해요오', null, [], [d(1.6), chain, st('약화', 2), st('취약', 1)]),
        O('교주님만 빼고', null, [], [d(1.6), chain, st('약화', 2), make(T1, 1)]),
        O('흉내 낸 일격', null, [], [d(1.2), stk('변장', 2), chain, st('약화', 2)]),
      ], [B('주먹 꽉', 'power'), B('째려보기', 'frost'), B('뒤로 숨기', null, [blk(1.0)])],
      { blurb: '입은 험해도 손은 작다. 그래도 맞으면 꽤 아프다' }),
    ],
  });
}

// ───────────────────────── 냉정 ─────────────────────────
// 제이드 — 쓰는 쪽(딜러). 책벌레 — 효과로 뽑힐 때(영감) 마법서가 펼쳐진다. 모은 지식은 옥장판 한 방에.
{
  const k = '제이드', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '비취옥의 용족 책벌레. 책장이 넘어가는 순간(영감)마다 지식이 쌓이고, 모은 지식은 누구에게도 안 나눠 준다.',
    keyword: { name: '지식', desc: '읽고 또 읽어 모은 지식 — 컬렉션은 나누지 않는다', carrier: 'self', cap: 5 },
    passives: [
      P('독서광', { on: 'turnStart' }, [draw(1)], [{ c: 'firstTurn' }]),
      P('절판 도서', { on: 'kill', mine: true }, [make(T1, 1)], null, turn1),
    ],
    ult: { name: '게르마늄 탐지법', cost: 150, fx: [d(0.25, 4, 'allEnemies'), stk('지식', 3), draw(1)] },
    cards: [
      TOK(T1, '펼쳐 둔 마법서', 0, '스킬', ['소멸'], [draw(1), stk('지식', 1)], '아무 데나 펼쳐도 읽을 거리가 있다'),
      U(`${k}_u1`, '게르마늄 옥장판', 3, '공격', [], [d(2.0), per('지식'), d(0.5), spend('지식', 'all')], [
        O('휴대용 옥장판', 2, [], [d(1.4), per('지식'), d(0.5), spend('지식', 'all')]),
        O('옥장판 공장', 4, [], [d(3.2), per('지식'), d(0.7), spend('지식', 'all')]),
        O('원적외선', null, [], [d(2.0), per('지식'), d(0.5), insp, stk('지식', 3)]),
        O('따끈한 바닥', null, [], [per('지식'), d(0.8), spend('지식', 'all'), st('취약', 2)]),
        O('광역 난방', null, [], [all(1.4), per('지식'), d(0.5, 1, 'allEnemies'), spend('지식', 'all')]),
      ], [B('게르마늄 함량', 'power'), B('보증서', 'cost'), B('사은품', null, [draw(1)])],
      { signature: true, blurb: '책에서 본 대로 만든 옥장판. 읽은 만큼 뜨거워진다' }),
      U(`${k}_u2`, '책에서 봤는데', 1, '스킬', [], [draw(2), insp, stk('지식', 2)], [
        O('목차만 봤는데', 0, [], [draw(1), insp, stk('지식', 2)]),
        O('전집을 봤는데', 2, [], [draw(3), stk('지식', 1), insp, stk('지식', 3)]),
        O('확신에 차서', null, [], [draw(2), stk('지식', 1), insp, stk('지식', 2)]),
        O('잘못된 상식', null, [], [draw(2), insp, stk('지식', 2), ap(1)]),
        O('서열 꼴찌', null, ['보존'], [draw(2), insp, stk('지식', 2)]),
      ], [B('밑줄', 'draw'), B('책갈피', null, [], ['보존']), B('독서등', null, [stk('지식', 1)])],
      { blurb: '무엇이든 책에서 봤다. 맞는 것도 있다' }),
      U(`${k}_u3`, '내 돈 가져가', 1, '공격', [], [d(1.1), insp, d(1.2)], [
        O('잔돈', 0, [], [d(0.6), insp, d(1.2)]),
        O('통장째로', 2, [], [d(2.4), insp, d(2.4)]),
        O('시스트에게 속아서', null, [], [d(1.1), stk('지식', 1), insp, d(1.2)]),
        O('환불 불가', null, [], [d(1.1), insp, d(1.2), stk('지식', 2)]),
        O('뜯긴 돈', null, [], [d(1.4), insp, d(1.4)]),
      ], [B('비취 동전', 'power'), B('영수증', null, [draw(1)]), B('귀가 얇아서', 'weakSpot')],
      { blurb: '귀가 얇아 늘 돈을 뜯긴다. 던지는 손만은 정확하다' }),
      U(`${k}_u4`, '초대 교주의 수양록', 2, '강화', [], [atkRun(0.15), draw(2)], [
        O('필사본', null, ['개전'], [atkRun(0.15), draw(2)]),
        O('원본 수양록', 3, [], [atkRun(0.2), draw(3), stk('지식', 3)]),
        O('주석 달기', null, [], [atkRun(0.15), draw(2), stk('지식', 2)]),
        O('절판 도서 컬렉션', null, [], [atkRun(0.15), make(T1, 3)]),
        O('밤새 읽기', 1, [], [atkRun(0.1), draw(2)]),
      ], [B('지식 컬렉션', 'atkUp'), B('표지 수선', null, [], ['개전']), B('귀한 판본', null, [stk('지식', 1)])],
      { blurb: '초대 교주가 남긴 수양록. 아무에게도 안 빌려준다' }),
    ],
  });
}

// 아이시아 — 만드는 쪽(딜러). 냉장고에서 미공개 신제품을 꺼낸다 — 증발 카드라 이번 턴에 안 쓰면 녹는다.
{
  const k = '아이시아', T1 = `${k}_t1`, T2 = `${k}_t2`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '프로스트 노바의 키 작은 회장. 미공개 신제품을 매 턴 꺼내 시연한다 — 이번 턴에 안 쓰면 녹아 버린다.',
    keyword: { name: '시장 점유율', desc: '멜루 코퍼레이션을 따라잡는 중인 점유율', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }] },
    passives: [
      P('주주총회', { on: 'turnStart' }, [make(T1, 1)]),
      P('기업전쟁', { on: 'kill', mine: true }, [stk('시장 점유율', 1), make(T2, 1)], null, turn1),
    ],
    ult: { name: 'CEO의 비밀 병기', cost: 250, fx: [st('기절', 1), all(1.6), make(T2, 2)] },
    cards: [
      TOK(T1, '냉동 신제품', 0, '공격', ['증발', '소멸'], [d(0.8), chain, draw(1)], '이번 턴에 안 쓰면 녹는다. 시연은 지금이다'),
      TOK(T2, '한정판 아이스박스', 0, '공격', ['증발', '소멸'], [d(1.2), st('취약', 1)], '132차 기업전쟁의 비밀 병기. 금방 녹는다'),
      U(`${k}_u1`, '신제품 시연', 2, '공격', [], [d(2.0), make(T1, 2)], [
        O('샘플 증정', 1, [], [d(1.2), make(T1, 1)]),
        O('대규모 출시', 3, [], [d(3.0), make(T1, 2), make(T2, 1)]),
        O('한정판', null, [], [d(2.0), make(T2, 1), make(T1, 1)]),
        O('언론 공개', null, [], [d(2.0), make(T1, 2), stk('시장 점유율', 1)]),
        O('빙결 신소재', null, [], [d(2.0), st('취약', 1), make(T1, 2)]),
      ], [B('특허 기술', 'power'), B('냉장 유통', 'frost'), B('주주 앞 존대', null, [stk('시장 점유율', 1)])],
      { signature: true, blurb: '아직 공개 안 한 신제품을 전투 한복판에서 시연한다. 녹기 전에' }),
      U(`${k}_u2`, '내가 누군지 몰라?', 1, '스킬', [], [draw(2), make(T1, 1)], [
        O('명함', 0, [], [draw(1), make(T1, 1)]),
        O('회장님 등장', 2, [], [draw(3), make(T1, 2), stk('시장 점유율', 1)]),
        O('비서 호출', null, [], [draw(2), make(T1, 1), blk(1.0)]),
        O('회장 직속', null, [], [draw(2), make(T2, 1)]),
        O('결재 서류', null, ['보존'], [draw(2), make(T1, 1)]),
      ], [B('회장 직인', 'draw'), B('법인 카드', null, [blk(1.0)]), B('호통', null, [], ['개전'])],
      { blurb: '키는 작아도 회장이다. 호통 한 번에 냉장고 문이 열린다' }),
      U(`${k}_u3`, '해고야!', 2, '공격', [], [d(2.4), brk, make(T2, 1), make(T1, 1)], [
        O('경고장', 1, [], [d(1.4), brk, make(T2, 1)]),
        O('대량 해고', 3, [], [all(2.0), brk, make(T2, 2), make(T1, 1)]),
        O('구조 조정', null, [], [d(2.4), st('취약', 1), brk, make(T2, 2)]),
        O('즉시 해고', null, [], [d(2.8), brk, make(T2, 1), make(T1, 1)]),
        O('남 좋은 일', null, [], [d(2.4), make(T1, 1), brk, make(T2, 1)]),
      ], [B('해고 통지서', 'power'), B('냉혹한 결정', 'weakSpot'), B('퇴직금', null, [draw(1)])],
      { blurb: '「해고야!」 — 번번이 남 좋은 일로 끝나지만 외칠 때만은 진심이다' }),
      U(`${k}_u4`, '정복, 멜루나!', 1, '강화', [], [atkRun(0.1), stk('시장 점유율', 2)], [
        O('133차 기업전쟁', null, ['개전'], [atkRun(0.1), stk('시장 점유율', 2)]),
        O('업계 1위', 2, [], [atkRun(0.15), stk('시장 점유율', 3), make(T2, 2)]),
        O('적대적 인수', null, [], [atkRun(0.1), stk('시장 점유율', 2), make(T2, 1)]),
        O('신제품 라인업', null, [], [atkRun(0.1), make(T1, 2)]),
        O('프로스트 노바', null, [], [atkRun(0.15), stk('시장 점유율', 2)]),
      ], [B('CEO의 자존심', 'atkUp'), B('사훈', null, [], ['개전']), B('주가 상승', null, [stk('시장 점유율', 1)])],
      { blurb: '멜루나를 꺾는 날까지. 오늘도 133차 기업전쟁이 시작된다' }),
    ],
  });
}

// ───────────────────────── 광기 ─────────────────────────
// 롤렛 — 만드는 쪽(딜러). 모자에서 카드를 꺼낸다 — 갈채가 클수록 좋은 카드(비둘기 → 사라지는 상자).
{
  const k = '롤렛', T1 = `${k}_t1`, T2 = `${k}_t2`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '세상을 서커스 무대로 보는 고위 마녀. 모자에서 카드를 꺼내고, 갈채가 클수록 더 놀라운 것이 나온다.',
    keyword: { name: '갈채', desc: '객석에서 쏟아지는 박수 — 클수록 다음 마술이 화려해진다', carrier: 'self', cap: 5 },
    passives: [
      P('관객석', { on: 'play', every: 2, minCost: 0 }, [stk('갈채', 1)]),
      P('봉봉한 결말', { on: 'kill', mine: true }, [make(T1, 1)], null, turn1),
    ],
    ult: { name: '관객을 사로잡는 트릭스터', cost: 250, fx: [all(1.6), st('기절', 1, 'randomEnemy'), stk('갈채', 3)] },
    cards: [
      TOK(T1, '비둘기', 0, '공격', ['소멸'], [rnd(0.4, 2), chain, stk('갈채', 1)], '모자에서 푸드덕. 관객석이 술렁인다'),
      TOK(T2, '사라지는 상자', 0, '공격', ['소멸'], [all(1.0), tough(1, 'allEnemies')], '상자를 닫았다 열면 — 적이 없다. 울랄라'),
      U(`${k}_u1`, '갈채를 먹는 엔터테이너', 3, '공격', [], [d(2.0), per('갈채'), d(0.5), spend('갈채', 'all')], [
        O('짧은 막간극', 2, [], [d(1.3), per('갈채'), d(0.5), spend('갈채', 'all')]),
        O('대서커스', 4, [], [d(3.4), per('갈채'), d(0.7), spend('갈채', 'all')]),
        O('커튼콜', null, [], [per('갈채'), d(0.6), spend('갈채', 'all'), make(T1, 2)]),
        O('객석 지목', null, [], [d(2.4), per('갈채'), d(0.6), spend('갈채', 'all')]),
        O('모두의 무대', null, [], [all(1.3), per('갈채'), d(0.5, 1, 'allEnemies'), spend('갈채', 'all')]),
      ], [B('스포트라이트', 'power'), B('모나미', 'cost'), B('실 부 플레', null, [stk('갈채', 1)])],
      { signature: true, blurb: '갈채를 먹고 사는 엔터테이너. 박수가 클수록 결말이 화려하다' }),
      U(`${k}_u2`, '비둘기 부활 마술', 1, '스킬', [], [make(T1, 2), has('갈채', 3), spend('갈채', 3), make(T2, 1)], [
        O('손수건', 0, [], [make(T1, 1), has('갈채', 3), spend('갈채', 3), make(T2, 1)]),
        O('비둘기 떼', 2, [], [make(T1, 4), has('갈채', 3), spend('갈채', 3), make(T2, 1)]),
        O('부활 또 부활', null, [], [make(T1, 3), has('갈채', 3), spend('갈채', 3), make(T2, 1)]),
        O('관객 참여', null, [], [make(T1, 2), has('갈채', 2), spend('갈채', 2), make(T2, 1)]),
        O('트릭 모자', null, ['보존'], [make(T1, 2), has('갈채', 3), spend('갈채', 3), make(T2, 1)]),
      ], [B('하얀 비둘기', 'draw'), B('비밀 주머니', null, [], ['보존']), B('울랄라', null, [stk('갈채', 1)])],
      { blurb: '죽은 비둘기를 살리는 마술. 박수가 터지면 상자까지 나온다' }),
      U(`${k}_u3`, '봉봉한 결말', 0, '공격', [], [{ k: 'dmg', ratio: 0.7, xHits: true, target: 'randomEnemy' }, chain, make(T1, 1)], [
        O('짧은 결말', null, [], [{ k: 'dmg', ratio: 0.8, xHits: true, target: 'randomEnemy' }, chain, make(T1, 1)]),
        O('반전 결말', null, [], [{ k: 'dmg', ratio: 0.7, xHits: true, target: 'randomEnemy' }, make(T1, 2)]),
        O('관객 선택', null, [], [{ k: 'dmg', ratio: 0.7, xHits: true, target: 'oneEnemy' }, chain, make(T1, 1)]),
        O('열린 결말', null, ['보존'], [{ k: 'dmg', ratio: 0.8, xHits: true, target: 'randomEnemy' }, chain, make(T1, 1)]),
        O('갈채의 결말', null, [], [{ k: 'dmg', ratio: 0.7, xHits: true, xStack: '갈채', target: 'randomEnemy' }, spend('갈채', 'all')]),
      ], [B('결말 비틀기', 'power'), B('앙코르', null, [stk('갈채', 1)]), B('막 내림', null, [draw(1)])],
      { x: true, blurb: '판을 슬쩍 굴려 가장 흥미로운 결말로 몰아간다. 그 다음엔 어떻게?' }),
      U(`${k}_u4`, '최고의 엔터테인먼트', 2, '강화', [], [atkRun(0.15), stk('갈채', 2)], [
        O('개막 공연', null, ['개전'], [atkRun(0.15), stk('갈채', 2)]),
        O('전석 매진', 3, [], [atkRun(0.2), stk('갈채', 3), make(T2, 1)]),
        O('무대 장치', null, [], [atkRun(0.15), stk('갈채', 2), make(T1, 1)]),
        O('평범함은 싫어', null, [], [atkRun(0.15), critRun(0.1)]),
        O('트릭스터의 눈', 1, [], [atkRun(0.1), stk('갈채', 1)]),
      ], [B('정장 차림', 'atkUp'), B('무대 조명', null, [], ['개전']), B('객석의 함성', null, [stk('갈채', 1)])],
      { blurb: '무반응이 제일 싫다. 오늘 밤 무대는 반드시 봉봉해야 한다' }),
    ],
  });
}

// 피라 — 쓰는 쪽(서포터). 금 같지만 금 아닌 위조범 — 위조 금화를 뿌리고, 모인 부유함을 한 번에 수금한다.
{
  const k = '피라', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '황철석 용족 위조범. 위조 금화를 손에 뿌리고, 이어 낼 때마다 이자가 붙는다 — 수금은 한 번에.',
    keyword: { name: '부유함', desc: '번쩍이는 (가짜) 재산', carrier: 'self', cap: 6 },
    passives: [
      P('뒷골목 장사', { on: 'fightStart' }, [make(T1, 2)]),
      P('이자', { on: 'turnEnd' }, [heal(0.6)], [{ c: 'stack', id: '부유함', n: 3 }]),
    ],
    ult: { name: '피버☆타임이다!', cost: 200, fx: [rnd(0.3, 9), stk('부유함', 3), make(T1, 2)] },
    cards: [
      TOK(T1, '위조 금화', 0, '스킬', ['소멸'], [stk('부유함', 1), chain, draw(1)], '진짜 같은 가짜. 한 번 쓰면 들통난다'),
      U(`${k}_u1`, '수금 시간이다!', 2, '공격', [], [per('부유함'), dd(0.45, 1, 'randomEnemy'), spend('부유함', 'all'), heal(1.0)], [
        O('소액 수금', 1, [], [per('부유함'), dd(0.35, 1, 'randomEnemy'), spend('부유함', 'all')]),
        O('채권 추심', 3, [], [per('부유함'), dd(0.7, 1, 'randomEnemy'), spend('부유함', 'all'), heal(1.5)]),
        O('복리 이자', null, [], [per('부유함'), dd(0.55, 1, 'randomEnemy'), spend('부유함', 'all'), heal(1.0)]),
        O('담보 잡기', null, [], [per('부유함'), dd(0.45, 1, 'randomEnemy'), spend('부유함', 'all'), sh(1.5)]),
        O('다조!', null, [], [per('부유함'), dd(0.45, 1, 'allEnemies'), spend('부유함', 'all')]),
      ], [B('금니', 'power'), B('장부 정리', 'heal'), B('야로!', null, [], ['보존'])],
      { signature: true, blurb: '돈 냄새가 나면 소심한 위조범도 험악해진다. 이자까지 쳐서 받는다' }),
      U(`${k}_u2`, '위조 금화 뿌리기', 1, '공격', [], [rnd(0.4, 3), make(T1, 1)], [
        O('동전 튕기기', 0, [], [rnd(0.4, 1), make(T1, 1)]),
        O('금화 소나기', 2, [], [rnd(0.4, 6), make(T1, 2)]),
        O('번쩍번쩍', null, [], [rnd(0.4, 3), st('약화', 1, 'randomEnemy'), make(T1, 1)]),
        O('위폐 공장', null, [], [rnd(0.4, 3), make(T1, 2)]),
        O('황철석 가루', null, [], [rnd(0.5, 3), make(T1, 1)]),
      ], [B('진짜 같은', 'power'), B('돈 냄새', null, [stk('부유함', 1)]), B('고래!', 'frost')],
      { blurb: '금 같지만 금 아니다. 맞으면 아프고 주우면 손해다' }),
      U(`${k}_u3`, '연금술 실험', 1, '스킬', [], [heal(1.8), chain, make(T1, 2)], [
        O('플라스크 하나', 0, [], [heal(1.0), chain, make(T1, 1)]),
        O('대실험', 2, [], [heal(3.6), chain, make(T1, 3)]),
        O('화금석 반응', null, [], [heal(1.8), stk('부유함', 1), chain, make(T1, 2)]),
        O('폭발 주의', null, [], [heal(1.8), st('약화', 1, 'allEnemies'), chain, make(T1, 2)]),
        O('진짜 금?', null, [], [heal(2.2), chain, make(T1, 2)]),
      ], [B('정제', 'heal'), B('실험 노트', null, [draw(1)]), B('보호 장갑', null, [sh(0.6)])],
      { blurb: '위조를 끊고 진짜 화금석을 찾는 중이다. 가끔 금화가 튀어나온다' }),
      U(`${k}_u4`, '화금석 연구', 2, '강화', [], [defRun(0.15), make(T1, 2)], [
        O('연구 노트', null, ['개전'], [defRun(0.15), make(T1, 2)]),
        O('골디를 넘어', 3, [], [defRun(0.2), make(T1, 3), stk('부유함', 3)]),
        O('과거 청산', null, [], [defRun(0.15), make(T1, 2), cleanse(1)]),
        O('황금 비율', null, [], [defRun(0.15), stk('부유함', 3)]),
        O('뒷골목 연금술', 1, [], [defRun(0.1), make(T1, 2)]),
      ], [B('화금석', 'defUp'), B('연구실', null, [], ['개전']), B('투자 유치', null, [stk('부유함', 1)])],
      { blurb: '골디와 비교당하지 않을 진짜 금. 언젠가는' }),
    ],
  });
}

// ───────────────────────── 활발 ─────────────────────────
// 우이 — 쓰는 쪽(서포터 · 엘다인). 바라는 것을 현실로 — 드로우를 뿌려 손을 바꾸고, 같은 결의 카드에 이어 내면 소원이 이뤄진다.
{
  const k = '우이', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '개구리 친구 에루와 다니는 이슬비 정령. 말하는 대로 손이 바뀌고, 싫은 건 안 보면 된다.',
    keyword: { name: '소원', desc: '「말하는 대로」 이뤄질 바람', carrier: 'self', cap: 3, consumeAll: true, per: [{ stat: 'dealt', v: 0.2 }] },
    passives: [
      P('말하는 대로', { on: 'play', every: 3, minCost: 0 }, [draw(1)]),
      P('덮어 주기', { on: 'lowHp', pct: 0.4 }, [heal(3.0), st('피해 감소', 2)]),
    ],
    ult: { name: '말하는대로!', cost: 250, fx: [heal(4.0), ap(2), draw(2)] },
    cards: [
      TOK(T1, '에루', 0, '공격', ['소멸'], [dd(0.6), chain, stk('소원', 1)], '개굴. 우이 친구는 우이가 지킨다'),
      U(`${k}_u1`, '개굴비', 2, '스킬', [], [heal(2.4), draw(2), chain, make(T1, 1)], [
        O('이슬 한 방울', 1, [], [heal(1.4), draw(1), chain, make(T1, 1)]),
        O('장맛비', 3, [], [heal(3.6), draw(3), chain, make(T1, 2)]),
        O('개구리 합창', null, [], [heal(2.4), draw(2), make(T1, 2)]),
        O('무지개', null, [], [heal(2.8), draw(2), chain, make(T1, 1)]),
        O('행복 이야기', null, [], [heal(2.4), draw(2), stk('소원', 2)]),
      ], [B('보슬보슬', 'heal'), B('우산', null, [sh(0.6)]), B('개굴개굴', null, [], ['보존'])],
      { signature: true, blurb: '이슬비가 내리면 다들 기분이 나아진다. 개구리도 신이 난다' }),
      U(`${k}_u2`, '에루 출동', 1, '공격', [], [dd(0.7), make(T1, 1), insp, make(T1, 1)], [
        O('폴짝', 0, [], [make(T1, 1), insp, make(T1, 1)]),
        O('에루 군단', 2, [], [dd(1.0), make(T1, 3), insp, make(T1, 1)]),
        O('혀 날름', null, [], [dd(0.7), st('약화', 1), insp, make(T1, 2)]),
        O('에루랑 같이', null, [], [dd(0.7), make(T1, 2)]),
        O('연못 점프', null, [], [dd(1.0), make(T1, 1), insp, make(T1, 1)]),
      ], [B('끈적한 혀', 'power'), B('연잎', null, [blk(1.0)]), B('친구야~', null, [], ['개전'])],
      { blurb: '우이의 단짝 개구리. 부르면 어디서든 폴짝 나온다' }),
      U(`${k}_u3`, '싫은 건 안 보면 돼', 1, '스킬', [], [draw(2), chain, ap(1)], [
        O('눈 감기', 0, [], [draw(1), chain, ap(1)]),
        O('다 덮어 줄게', 2, [], [draw(3), cleanse(2), chain, ap(2)]),
        O('잊어버리기', null, [], [draw(2), cleanse(1), chain, ap(1)]),
        O('행복한 쪽으로', null, [], [draw(2), stk('소원', 1), chain, ap(1)]),
        O('영원살이 막내', null, ['보존'], [draw(2), chain, ap(1)]),
      ], [B('해맑은 얼굴', 'draw'), B('비구름 걷기', null, [cleanse(1)]), B('노래 흥얼', null, [], ['개전'])],
      { blurb: '견디기 힘든 건 지워 버렸다. 그래서 남의 슬픔도 덮어 주려 한다' }),
      U(`${k}_u4`, '장화 신은 우이', 2, '강화', [], [defRun(0.15), draw(2), stk('소원', 1)], [
        O('새 장화', null, ['개전'], [defRun(0.15), draw(2), stk('소원', 1)]),
        O('물웅덩이 축제', 3, [], [defRun(0.2), draw(3), make(T1, 2)]),
        O('첨벙첨벙', null, [], [defRun(0.15), draw(2), make(T1, 1)]),
        O('비 오는 날 산책', null, [], [defRun(0.15), draw(3)]),
        O('작은 장화', 1, [], [defRun(0.1), draw(2)]),
      ], [B('빨간 장화', 'defUp'), B('비옷', null, [], ['개전']), B('물방울 무늬', null, [stk('소원', 1)])],
      { blurb: '장화만 신으면 어디든 갈 수 있다. 물웅덩이부터' }),
    ],
  });
}

// 쥬비 — 만드는 쪽(딜러). 수백 마리 군집 — 작은 「친구 쥬비」 가 여러 장 들어온다. 뽑힐 때(영감) 더 몰려온다.
{
  const k = '쥬비', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '수백 마리가 한 정신을 나눠 쓰는 꿀의 정령. 손에 친구 쥬비가 우르르 들어오고, 꿀 도둑은 절대 잊지 않는다비.',
    keyword: { name: '꿀 재고', desc: '군집이 모아 둔 꿀 — 넉넉하면 친구가 더 온다', carrier: 'self', cap: 5 },
    passives: [
      P('윙윙', { on: 'play', every: 3, minCost: 0 }, [rnd(0.6, 1)]),
      P('꿀 창고', { on: 'fightStart' }, [make(T1, 2)]),
    ],
    ult: { name: '행복했쮸비', cost: 200, fx: [atkRun(0.2), make(T1, 4)] },
    cards: [
      TOK(T1, '친구 쥬비', 0, '공격', ['소멸'], [d(0.5), chain, d(0.4)], '쥬비의 친구 쥬비. 작지만 쏘면 아프다비'),
      U(`${k}_u1`, '친구왔쮸비', 1, '공격', [], [d(0.9), make(T1, 2)], [
        O('한 마리', 0, [], [d(0.5), make(T1, 1)]),
        O('친구의 친구', 2, [], [d(1.6), make(T1, 4)]),
        O('줄 서서 쏘기', null, [], [d(0.9), make(T1, 3)]),
        O('꿀 냄새', null, [], [d(0.9), stk('꿀 재고', 1), make(T1, 2)]),
        O('여왕 쥬비', null, [], [d(1.2), make(T1, 2)]),
      ], [B('독침', 'power'), B('꿀 한 방울', null, [stk('꿀 재고', 1)]), B('정찰 쥬비', null, [], ['개전'])],
      { signature: true, blurb: '하나가 오면 둘이 오고, 둘이 오면 다 온다비' }),
      U(`${k}_u2`, '꿀 서리범 현상수배', 1, '스킬', [], [st('취약', 1), make(T1, 1), insp, make(T1, 2)], [
        O('수배 전단', 0, [], [st('취약', 1), insp, make(T1, 2)]),
        O('전국 수배', 2, [], [st('취약', 2, 'allEnemies'), make(T1, 2), insp, make(T1, 2)]),
        O('잊지 않는다비', null, [], [st('취약', 2), make(T1, 1), insp, make(T1, 2)]),
        O('현상금', null, [], [st('취약', 1), stk('꿀 재고', 2), insp, make(T1, 2)]),
        O('복수 예고', null, [], [st('취약', 1), make(T1, 2), insp, make(T1, 2)]),
      ], [B('몽타주', 'frost'), B('목격자 쥬비', null, [draw(1)]), B('빨간 압정', null, [], ['보존'])],
      { blurb: '꿀 도둑의 얼굴은 수백 마리가 다 기억한다' }),
      U(`${k}_u3`, '벌떼 습격', 2, '공격', [], [rnd(0.4, 6), insp, make(T1, 2)], [
        O('정찰대', 1, [], [rnd(0.4, 3), insp, make(T1, 2)]),
        O('대군집', 3, [], [rnd(0.45, 9), make(T1, 1), insp, make(T1, 2)]),
        O('한 놈만', null, [], [d(0.45, 6), insp, make(T1, 2)]),
        O('사방에서', null, [], [rnd(0.4, 6), make(T1, 1), insp, make(T1, 2)]),
        O('유혈 사태', null, [], [rnd(0.5, 6), insp, make(T1, 2)]),
      ], [B('날카로운 침', 'power'), B('날갯짓', null, [rushDown(1, 'randomEnemy')]), B('꿀벌의 춤', 'frost')],
      { blurb: '하늘이 노랗게 덮인다. 꿀 도둑은 숨을 곳이 없다' }),
      U(`${k}_u4`, '윙윙 집합', 1, '스킬', [], [make(T1, 3)], [
        O('집합 신호', 0, [], [make(T1, 2)]),
        O('군집 총동원', 2, [], [make(T1, 5), blk(1.0)]),
        O('밀랍 벽', null, [], [make(T1, 3), blk(1.0)]),
        O('꿀 배급', null, [], [make(T1, 3), stk('꿀 재고', 2)]),
        O('한 정신', null, [], [atkRun(0.1), make(T1, 3)], true),
      ], [B('여왕의 부름', 'cost'), B('벌집', null, [blk(1.0)]), B('비비비', null, [], ['개전'])],
      { blurb: '윙윙 소리가 들리면 이미 늦었다비' }),
    ],
  });
}

// ───────────────────────── 우울 ─────────────────────────
// 시온 더 다크불릿 — 쓰는 쪽(딜러 · 엘다인). 탄창을 채워 「진혼의 탄환」 을 장전한다 — 탄환은 회수로 한 번 더 돌아온다.
{
  const k = '시온더다크불릿', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '자칭 검은 마탄의 사수(실은 편의점 야간 알바). 탄창을 채워 진혼의 탄환을 장전하고, 쏜 탄은 다시 손에 돌아온다.',
    keyword: { name: '흑마탄', desc: '탄창에 재어 둔 검은 마탄', carrier: 'self', cap: 3 },
    passives: [
      P('장전', { on: 'play', type: '공격', every: 2 }, [stk('흑마탄', 1)]),
      P('화합의 엘다인', { on: 'lowHp', pct: 0.4 }, [st('피해 감소', 2), make(T1, 1)]),
    ],
    ult: { name: '종.말.의.폭.발★', cost: 150, fx: [all(1.2), stk('흑마탄', 3)] },
    cards: [
      TOK(T1, '진혼의 탄환', 0, '공격', ['회수'], [d(1.4), chain, d(0.6)], '검은 운명을 새긴 한 발. 쏘고 나면 다시 손에 돌아온다'),
      U(`${k}_u1`, '마.탄.의.사.수', 2, '공격', [], [d(2.2), has('흑마탄', 3), spend('흑마탄', 3), make(T1, 1)], [
        O('견제 사격', 1, [], [d(1.3), has('흑마탄', 3), spend('흑마탄', 3), make(T1, 1)]),
        O('종말의 탄도', 3, [], [d(3.6), has('흑마탄', 3), spend('흑마탄', 3), make(T1, 2)]),
        O('큭큭', null, [], [d(2.4), stk('흑마탄', 2)]),
        O('흑염룡의 눈', null, [], [d(2.6), has('흑마탄', 3), spend('흑마탄', 3), make(T1, 1)]),
        O('운명 사격', null, [], [d(2.2), st('취약', 2), make(T1, 1)]),
      ], [B('검은 마탄', 'power'), B('운명의 방아쇠', 'weakSpot'), B('어둠의 계약', null, [stk('흑마탄', 1)])],
      { signature: true, blurb: '마탄의 사수가 운명을 쏜다. 탄창이 차 있으면 진혼의 탄환이 장전된다' }),
      U(`${k}_u2`, '좌표 잡기', 1, '공격', ['회수'], [d(1.0), chain, stk('흑마탄', 1)], [
        O('눈대중', 0, ['회수'], [d(0.6), chain, stk('흑마탄', 1)]),
        O('위성 좌표', 2, ['회수'], [d(2.0), stk('흑마탄', 1), chain, stk('흑마탄', 2)]),
        O('조준경', null, ['회수'], [d(1.0), st('취약', 1), chain, stk('흑마탄', 1)]),
        O('관측수', null, ['회수'], [d(1.0), draw(1), chain, stk('흑마탄', 1)]),
        O('고정 좌표', null, ['회수 2'], [d(1.0), chain, stk('흑마탄', 1)]),
      ], [B('야간 투시', 'power'), B('삼각대', null, [blk(1.0)]), B('메모', null, [], ['보존'])],
      { blurb: '좌표를 찍고 탄을 잰다. 쏘고 나면 다시 손에 돌아온다' }),
      U(`${k}_u3`, '디 엑시트로', 2, '공격', [], [all(1.2), chain, make(T1, 1)], [
        O('출구 표시', 1, [], [all(0.8), chain, make(T1, 1)]),
        O('비상구 폭파', 3, [], [all(2.0), make(T1, 2)]),
        O('어둠의 출구', null, [], [all(1.2), st('약화', 1, 'allEnemies'), chain, make(T1, 1)]),
        O('출구는 없다', null, [], [all(1.5), chain, make(T1, 1)]),
        O('퇴근 시간', null, [], [all(1.2), make(T1, 1), draw(1)]),
      ], [B('종말의 서', 'power'), B('검은 날개', null, [st('약화', 1, 'allEnemies')]), B('야간 알바', 'cost')],
      { blurb: '「디 엑시트로!」 — 뜻은 본인도 모른다. 터지는 건 확실하다' }),
      U(`${k}_u4`, '흑염룡 각성', 1, '강화', [], [atkRun(0.1), stk('흑마탄', 3)], [
        O('봉인 해제', null, ['개전'], [atkRun(0.1), stk('흑마탄', 3)]),
        O('완전 각성', 2, [], [atkRun(0.15), stk('흑마탄', 3), make(T1, 1)]),
        O('붕대 풀기', null, [], [atkRun(0.1), make(T1, 1)]),
        O('검은 오른팔', null, [], [atkRun(0.15), stk('흑마탄', 2)]),
        O('가족의 이름으로', null, [], [atkRun(0.1), critRun(0.1), stk('흑마탄', 2)]),
      ], [B('흑염룡', 'atkUp'), B('중2의 맹세', null, [], ['개전']), B('편의점 커피', null, [draw(1)])],
      { blurb: '오른팔에 봉인된 흑염룡이 깨어난다(는 설정이다)' }),
    ],
  });
}

// 에슈르 — 만드는 쪽(딜러). 마법학교(라고 우기는 빵집) — 「갓 구운 센디오」 를 구워 손에 돌린다. 빵은 회수로 한 번 더.
{
  const k = '에슈르', T1 = `${k}_t1`;
  out[k] = hero(k, {
    style: STYLE,
    blurb: '마법학교 교장(이라고 우기는 빵집 주인). 갓 구운 빵을 손에 돌리고, 빵은 한 번 더 구워져 돌아온다.',
    keyword: { name: '반죽', desc: '부풀고 있는 반죽 — 오븐에 넣으면 빵이 된다', carrier: 'self', cap: 3 },
    passives: [
      P('새벽 반죽', { on: 'turnStart' }, [stk('반죽', 1)]),
      P('빵 아니라고!', { on: 'play', type: '공격', minCost: 2 }, [has('반죽', 2), spend('반죽', 2), make(T1, 1)]),
    ],
    ult: { name: '빵테오', cost: 250, fx: [d(3.0), all(1.0), st('기절', 1)] },
    cards: [
      TOK(T1, '갓 구운 센디오', 0, '공격', ['회수'], [d(0.6), chain, heal(0.5)], '빵이 아니라 마법 촉매다. 따끈하다'),
      U(`${k}_u1`, '빵템피드', 2, '공격', [], [rnd(0.45, 4), make(T1, 2)], [
        O('한 쟁반', 1, [], [rnd(0.45, 2), make(T1, 1)]),
        O('빵 대폭주', 3, [], [rnd(0.5, 6), make(T1, 3)]),
        O('바게트 돌격', null, [], [rnd(0.55, 4), make(T1, 2)]),
        O('에심당 신메뉴', null, [], [rnd(0.45, 4), make(T1, 2), stk('반죽', 1)]),
        O('빵 비', null, [], [d(0.45, 4, 'allEnemies'), make(T1, 1)]),
      ], [B('버터 듬뿍', 'power'), B('오븐 장갑', null, [blk(1.0)]), B('명물', null, [], ['개전'])],
      { signature: true, blurb: '마법학교에서 빵이 쏟아져 나온다. 아니, 마법 촉매가' }),
      U(`${k}_u2`, '입자 이론', 1, '스킬', [], [draw(2), insp, make(T1, 1)], [
        O('기초 이론', 0, [], [draw(1), insp, make(T1, 1)]),
        O('장광설', 2, [], [draw(3), stk('반죽', 2), insp, make(T1, 2)]),
        O('발효 이론', null, [], [draw(2), stk('반죽', 1), insp, make(T1, 1)]),
        O('숨도 안 쉬고', null, [], [draw(2), insp, make(T1, 2)]),
        O('코골이', null, ['보존'], [draw(2), insp, make(T1, 1)]),
      ], [B('칠판', 'draw'), B('분필 가루', null, [stk('반죽', 1)]), B('수업 시작', null, [], ['개전'])],
      { blurb: '마법 이론만큼은 엘리아스 최정상급. 학생은 안 듣고 빵만 산다' }),
      U(`${k}_u3`, '화염 주문', 3, '공격', [], [all(2.0), chain, tough(2, 'allEnemies')], [
        O('불씨 주문', 2, [], [all(1.3), chain, tough(1, 'allEnemies')]),
        O('대화염', 4, [], [all(3.4), tough(2, 'allEnemies')]),
        O('오븐 예열', null, [], [all(2.0), stk('반죽', 3), chain, tough(2, 'allEnemies')]),
        O('겉바속촉', null, [], [all(2.0), make(T1, 1), chain, tough(2, 'allEnemies')]),
        O('불타는 빵', null, [], [all(2.4), chain, tough(2, 'allEnemies')]),
      ], [B('화력 조절', 'power'), B('굽는 냄새', null, [make(T1, 1)]), B('교장 권한', 'cost')],
      { blurb: '진짜 마법이다. 빵 굽는 데 쓰면 안 된다고 해도 쓴다' }),
      U(`${k}_u4`, '궁극의 그리모어', 1, '강화', [], [atkRun(0.1), stk('반죽', 3)], [
        O('그리모어 펼치기', null, ['개전'], [atkRun(0.1), stk('반죽', 3)]),
        O('금지된 장', 2, [], [atkRun(0.15), stk('반죽', 3), make(T1, 2)]),
        O('레시피 페이지', null, [], [atkRun(0.1), make(T1, 2)]),
        O('마법학교 교장', null, [], [atkRun(0.15), stk('반죽', 2)]),
        O('빵의 진리', null, [], [atkRun(0.1), stk('반죽', 3), draw(1)]),
      ], [B('교장의 위엄', 'atkUp'), B('책갈피 빵', null, [], ['개전']), B('밀가루 묻은 표지', null, [stk('반죽', 1)])],
      { blurb: '궁극의 마법서. 절반은 빵 레시피다' }),
    ],
  });
}

for (const [k, v] of Object.entries(out)) write(DIR, k, v);
console.log('썼다', Object.keys(out).length);
