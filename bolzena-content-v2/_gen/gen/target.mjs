// 표적형 11명 — 적 하나를 찍고(찍기 키워드 — 한 번에 한 적, 옮기면 처음부터) 그 적을 끝까지 쫓는다.
// 공용 계기: 「찍은 적이면:」(ifHunted — 아군 누구의 찍기든) · 「(아군이) 찍은 적이 쓰러지면」(huntDown who:any).
// 같은 속성 짝: 찍는 쪽(표식을 크게 · 싸게) 과 끝내는 쪽(찍은 적이면 · 쓰러지면) 으로 무게를 나눈다.
import { d, dd, all, rnd, blk, sh, heal, st, stk, spend, draw, ap, make, cheap, tough, gauge, rushDown, cleanse, strip,
  chain, brk, tune, insp, has, hasNot, per, atkRun, defRun, critRun, taken, O, B, P, turn1, turnN, fight1, hero, U, TOK, write } from './lib.mjs';

const DIR = 'C:/projects/bolzena-content/heroes/mark';
const STYLE = '표적형';
const hunted = { k: 'ifHunted' };
const top = (id, v) => stk(id, v, 'topEnemy');
const low = (id, v) => stk(id, v, 'lowEnemy');
const crit = (v) => ({ k: 'critMod', v, target: 'self' });
const KW = (name, desc, cap, per) => ({ name, desc, carrier: 'enemy', hunt: true, cap, ...(per ? { per } : {}) });
const out = {};

// ───────────────────────── 순수 ─────────────────────────
// 캐시 — 찍는 쪽(딜러). 유령 늪의 정찰 요원 — 정찰로 찍은 적에게 테이저를 몰아 쏜다.
{
  const k = '캐시', M = '정찰 표적';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '유령 늪에 박힌 정찰 요원. 정찰로 한 놈을 찍어 두고, 테이저를 그 녀석에게만 몰아 쏜다.',
    keyword: KW(M, '정찰로 찍어 둔 적 — 테이저가 그쪽으로만 간다', 4, [{ stat: 'taken', v: 0.1 }]),
    passives: [
      P('늪지 잠복', { on: 'fightStart' }, [top(M, 2)]),
      P('다음 정찰', { on: 'huntDown', who: 'any' }, [top(M, 2), draw(1)], null, turn1),
    ],
    ult: { name: '점프 스케어', cost: 150, fx: [stk(M, 3), d(1.5), st('약화', 1)] },
    cards: [
      U(`${k}_u1`, '정찰 개시', 1, '스킬', [], [stk(M, 3), draw(1)], [
        O('망원경', 0, [], [stk(M, 2), draw(1)]),
        O('정밀 정찰', 2, [], [stk(M, 4), st('취약', 2), draw(2)]),
        O('위장 텐트', null, [], [stk(M, 3), blk(1.5), draw(1)]),
        O('늪 지도', null, [], [stk(M, 3), draw(2)]),
        O('유령 탐지기', null, ['보존'], [stk(M, 4), draw(1)]),
      ], [B('쌍안경', 'draw'), B('표식 깃발', null, [st('취약', 1)]), B('잠복', null, [], ['개전'])],
      { signature: true, blurb: '늪 한가운데서 망원경을 든다. 찍힌 녀석은 오늘 테이저 맛을 본다' }),
      U(`${k}_u2`, '테이저 연사', 1, '공격', [], [d(0.5, 2), hunted, d(0.4, 2)], [
        O('한 발', 0, [], [d(0.5), hunted, d(0.4, 2)]),
        O('과충전 연사', 2, [], [d(0.6, 3), hunted, d(0.5, 3)]),
        O('전기 그물', null, [], [d(0.5, 2), st('약화', 1), hunted, d(0.4, 2)]),
        O('집중 사격', null, [], [d(0.5, 2), hunted, d(0.5, 3)]),
        O('추적 탄', null, [], [d(0.5, 2), stk(M, 1), hunted, d(0.4, 2)]),
      ], [B('고압 배터리', 'power'), B('찌릿', 'frost'), B('예비 탄창', null, [draw(1)])],
      { blurb: '찍힌 적이면 방아쇠를 놓지 않는다' }),
      U(`${k}_u3`, '풀파워 테이저', 2, '공격', [], [per(M), d(0.6), d(1.2)], [
        O('저출력', 1, [], [per(M), d(0.5), d(0.6)]),
        O('최대 출력', 3, [], [per(M), d(0.9), d(2.0)]),
        O('감전 쇼크', null, [], [per(M), d(0.6), d(1.2), st('약화', 2)]),
        O('기절 출력', null, [], [per(M), d(0.6), d(1.2), tough(2)]),
        O('방전', null, [], [per(M), d(0.75), d(1.2)]),
      ], [B('전극', 'power'), B('절연 장갑', null, [blk(1.0)]), B('조준 레이저', 'weakSpot')],
      { blurb: '정찰한 만큼 출력이 오른다. 찍힌 적에게 남김없이' }),
      U(`${k}_u4`, '정찰 요원의 각오', 1, '강화', [], [atkRun(0.1), stk(M, 2)], [
        O('첫 임무', null, ['개전'], [atkRun(0.1), stk(M, 3)]),
        O('베테랑 요원', 2, [], [atkRun(0.15), stk(M, 4), draw(2)]),
        O('무전기', null, [], [atkRun(0.1), stk(M, 2), draw(1)]),
        O('귀환 약속', null, [], [atkRun(0.1), stk(M, 2), st('피해 감소', 2)]),
        O('겁쟁이 아님', null, [], [atkRun(0.15), stk(M, 2)]),
      ], [B('요원 배지', 'atkUp'), B('정찰 수첩', null, [], ['개전']), B('늪 장화', null, [blk(1.0)])],
      { blurb: '유령은 무섭다. 그래도 임무는 끝까지 한다' }),
    ],
  });
}

// 다야 — 끝내는 쪽(딜러). 소원은 완벽해지기 — 한 놈씩 완벽하게. 찍은 적을 끝내면 다이아가 다음 적(가장 약한 적)으로 튄다.
{
  const k = '다야', M = '다이아 쓰라림';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '다이아몬드 용족. 한 놈씩 완벽하게 끝내고, 박힌 다이아는 다음 적으로 튄다.',
    keyword: KW(M, '박혀 반짝이는 다이아 조각', 5, [{ stat: 'taken', v: 0.08 }]),
    passives: [
      P('튀는 다이아', { on: 'huntDown' }, [per(M), d(0.4, 1, 'lowEnemy'), low(M, 2)]),
      P('완벽주의', { on: 'huntDown', who: 'any' }, [ap(1)], null, turn1),
    ],
    ult: { name: '다이아 브레…츄!', cost: 250, fx: [all(2.2), st('기절', 1), stk(M, 3)] },
    cards: [
      U(`${k}_u1`, '완벽한 일격', 2, '공격', [], [d(1.6), per(M), d(0.4), brk, draw(1)], [
        O('깔끔한 일격', 1, [], [d(1.0), per(M), d(0.3), brk, draw(1)]),
        O('완벽 그 이상', 3, [], [d(2.6), per(M), d(0.6), brk, draw(2)]),
        O('보석 세공', null, [], [d(1.6), per(M), d(0.4), brk, ap(1)]),
        O('흠집 하나 없이', null, [], [d(2.0), per(M), d(0.4), brk, draw(1)]),
        O('찍은 대로', null, [], [d(1.6), per(M), d(0.5), hunted, tough(2)]),
      ], [B('브릴리언트 컷', 'power'), B('광택', 'weakSpot'), B('완벽한 자세', null, [], ['보존'])],
      { signature: true, blurb: '완벽해지고 싶다는 소원. 한 번에 한 놈, 흠 없이' }),
      U(`${k}_u2`, '다이아 박기', 1, '공격', [], [d(0.9), stk(M, 2)], [
        O('파편', 0, [], [d(0.5), stk(M, 1)]),
        O('원석 통째로', 2, [], [d(1.8), stk(M, 4)]),
        O('쐐기 다이아', null, [], [d(0.9), stk(M, 2), tough(1)]),
        O('반짝이는 낙인', null, [], [d(0.9), stk(M, 3)]),
        O('다이아 비', null, [], [d(0.4, 3), stk(M, 2)]),
      ], [B('단단한 결', 'power'), B('반사광', null, [st('약화', 1)]), B('보석함', null, [], ['개전'])],
      { blurb: '세상에서 제일 단단한 조각이 박힌다. 뺄 수 없다' }),
      U(`${k}_u3`, '다이아 비늘', 1, '스킬', [], [blk(2.0), hunted, stk(M, 2)], [
        O('비늘 한 장', 0, [], [blk(1.2), hunted, stk(M, 1)]),
        O('다이아 갑주', 2, [], [blk(4.0), hunted, stk(M, 3)]),
        O('반사 비늘', null, [], [blk(2.0), st('반격', 1), hunted, stk(M, 2)]),
        O('빛 굴절', null, [], [blk(2.0), hunted, stk(M, 2), draw(1)]),
        O('용족의 긍지', null, ['보존'], [blk(2.4), hunted, stk(M, 2)]),
      ], [B('단단함', 'guard'), B('광채', null, [st('약화', 1)]), B('긍지', null, [], ['개전'])],
      { blurb: '다이아 비늘은 막는 순간에도 조각을 흩뿌린다' }),
      U(`${k}_u4`, '완벽해지는 소원', 2, '강화', [], [atkRun(0.15), stk(M, 3)], [
        O('소원 빌기', null, ['개전'], [atkRun(0.15), stk(M, 3)]),
        O('완벽한 용', 3, [], [atkRun(0.2), stk(M, 5), ap(1)]),
        O('흠 없는 하루', null, [], [atkRun(0.15), stk(M, 3), draw(1)]),
        O('거울 앞에서', null, [], [atkRun(0.15), critRun(0.15)]),
        O('작은 소원', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('다이아 왕관', 'atkUp'), B('별똥별', null, [], ['개전']), B('반짝임', null, [stk(M, 1)])],
      { blurb: '완벽해지고 싶다. 그러려면 한 놈씩 확실히' }),
    ],
  });
}

// 란 — 끝내는 쪽(딜러 · 엘다인). 최초의 사냥꾼 늑대 — 사냥감 하나를 물고 끝까지, 쓰러지면 다음 사냥감(가장 센 적).
{
  const k = '란', M = '사냥감';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '최초의 사냥꾼 늑대. 사냥감 하나를 물면 끝까지 놓지 않고, 쓰러지면 곧장 다음 사냥감을 문다.',
    keyword: KW(M, '물어 둔 사냥감 — 냄새를 놓치지 않는다', 4, [{ stat: 'taken', v: 0.12 }]),
    passives: [
      P('다음 사냥감', { on: 'huntDown', who: 'any' }, [top(M, 2), draw(1)], null, turn1),
      P('몰이', { on: 'play', type: '공격', every: 3 }, [stk(M, 1)]),
    ],
    ult: { name: '몰이 사냥', cost: 250, fx: [d(0.9, 4), stk(M, 2), brk, ap(2)] },
    cards: [
      U(`${k}_u1`, '일섬 발도', 1, '공격', [], [d(1.3), hunted, tough(1), brk, ap(1)], [
        O('반 발도', 0, [], [d(0.8), hunted, tough(1), brk, ap(1)]),
        O('이중 일섬', 2, [], [d(1.5, 2), hunted, tough(2), brk, ap(1)]),
        O('목덜미', null, [], [d(1.6), hunted, tough(1), brk, ap(1)]),
        O('늑대의 이빨', null, [], [d(1.3), stk(M, 2), brk, ap(1)]),
        O('끝까지', null, [], [d(1.3), hunted, d(0.6), brk, ap(1)]),
      ], [B('예리한 칼날', 'power'), B('달밤', 'weakSpot'), B('칼집', null, [], ['보존'])],
      { signature: true, blurb: '물어 둔 사냥감이면 칼끝이 먼저 간다. 쓰러지면 다음 칼을 뽑는다' }),
      U(`${k}_u2`, '냄새 맡기', 0, '스킬', [], [stk(M, 2), draw(1)], [
        O('바람 냄새', null, [], [stk(M, 2), draw(1), blk(0.6)]),
        O('추적', 1, [], [stk(M, 3), draw(2)]),
        O('발자국', null, [], [stk(M, 3), draw(1)]),
        O('피 냄새', null, [], [stk(M, 2), st('취약', 1), draw(1)]),
        O('사냥꾼의 코', null, ['보존'], [stk(M, 2), draw(1), cheap(1)]),
      ], [B('예민한 코', 'draw'), B('귀 쫑긋', null, [blk(0.6)]), B('무리의 신호', null, [], ['개전'])],
      { blurb: '한 번 맡은 냄새는 놓치지 않는다' }),
      U(`${k}_u3`, '몰이 사냥', 2, '공격', [], [d(0.45, 4), per(M), d(0.3)], [
        O('두 마리 몰이', 1, [], [d(0.45, 2), per(M), d(0.25)]),
        O('무리 사냥', 3, [], [d(0.5, 6), per(M), d(0.4)]),
        O('퇴로 차단', null, [], [d(0.45, 4), st('약화', 1), per(M), d(0.3)]),
        O('물고 흔들기', null, [], [d(0.5, 4), per(M), d(0.35)]),
        O('사냥 신호', null, [], [d(0.45, 4), stk(M, 1), per(M), d(0.3)]),
      ], [B('날카로운 송곳니', 'power'), B('포위', 'frost'), B('늑대 울음', null, [draw(1)])],
      { blurb: '사냥감이 지칠 때까지 몰아붙인다' }),
      U(`${k}_u4`, '최초의 사냥꾼', 2, '강화', [], [atkRun(0.15), stk(M, 2)], [
        O('사냥 시작', null, ['개전'], [atkRun(0.15), stk(M, 3)]),
        O('사냥꾼의 왕', 3, [], [atkRun(0.2), stk(M, 4), ap(1)]),
        O('엘다인의 피', null, [], [atkRun(0.15), stk(M, 2), st('피해 감소', 2)]),
        O('고요한 숲', null, [], [atkRun(0.15), stk(M, 2), draw(1)]),
        O('홀로 걷는 늑대', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('은빛 털', 'atkUp'), B('사냥 의식', null, [], ['개전']), B('발톱', null, [stk(M, 1)])],
      { blurb: '최초의 사냥꾼. 늑대는 사냥감을 고를 뿐 망설이지 않는다' }),
    ],
  });
}

// 뮤트 — 찍는 쪽(서포터). 표적 하나에 홀로그램을 겹겹이 — 한 적에게만 쌓이고, 옮기면 처음부터.
{
  const k = '뮤트', M = '홀로그램';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '나타의 해커 정령. 표적 하나에 홀로그램을 겹겹이 덮어 씌워, 아군의 공격이 그 적에게 쏟아지게 한다.',
    keyword: KW(M, '겹겹이 덧씌운 표적 홀로그램', 5, [{ stat: 'taken', v: 0.1 }]),
    passives: [
      P('표적 동기화', { on: 'fightStart' }, [top(M, 2)]),
      P('재부팅', { on: 'huntDown', who: 'any' }, [top(M, 2), heal(1.0)], null, turn1),
    ],
    ult: { name: '기초적인 해킹', cost: 250, fx: [stk(M, 5), st('취약', 2), ap(2)] },
    cards: [
      U(`${k}_u1`, '타깃 해킹', 1, '스킬', [], [stk(M, 2), st('취약', 1), hunted, draw(1)], [
        O('핑', 0, [], [stk(M, 2), hunted, draw(1)]),
        O('루트 권한', 2, [], [stk(M, 4), st('취약', 2), hunted, draw(2)]),
        O('백도어', null, [], [stk(M, 3), st('취약', 1), hunted, draw(1)]),
        O('바이러스', null, [], [stk(M, 2), st('취약', 1), st('약화', 1)]),
        O('자동 실행', null, ['개전'], [stk(M, 2), st('취약', 2), hunted, draw(1)]),
      ], [B('관리자 모드', 'draw'), B('취약점 스캔', null, [st('취약', 1)]), B('대기 중', null, [], ['보존'])],
      { signature: true, blurb: '홀로그램 한 겹이 더 씌워질 때마다 그 적이 또렷해진다' }),
      U(`${k}_u2`, '데이터 스트림', 1, '공격', [], [dd(0.6), per(M), dd(0.15)], [
        O('패킷', 0, [], [dd(0.3), per(M), dd(0.12)]),
        O('대용량 전송', 2, [], [dd(1.1), per(M), dd(0.3)]),
        O('스트림 분기', null, [], [dd(0.6), per(M), dd(0.15), stk(M, 1)]),
        O('과부하', null, [], [dd(0.6), per(M), dd(0.15), tough(1)]),
        O('압축 해제', null, [], [dd(0.7), per(M), dd(0.18)]),
      ], [B('광대역', 'power'), B('암호화', null, [sh(0.5)]), B('로그 기록', null, [draw(1)])],
      { blurb: '데이터가 홀로그램을 타고 흐른다. 겹이 많을수록 굵게' }),
      U(`${k}_u3`, '방화벽', 1, '스킬', [], [sh(1.5), hunted, draw(1)], [
        O('임시 방화벽', 0, [], [sh(0.8), hunted, draw(1)]),
        O('이중 방화벽', 2, [], [sh(3.0), hunted, draw(2)]),
        O('침입 차단', null, [], [sh(1.5), st('약화', 1), hunted, draw(1)]),
        O('백신', null, [], [sh(1.5), cleanse(1), hunted, draw(1)]),
        O('나타의 복구 코드', null, [], [sh(1.5), heal(0.8), hunted, draw(1)]),
      ], [B('보안 패치', 'guard'), B('로그인 기록', null, [stk(M, 1)]), B('상시 가동', null, [], ['보존'])],
      { blurb: '동료는 지키고 표적은 열어 둔다' }),
      U(`${k}_u4`, '홀로그램 증폭기', 1, '강화', [], [defRun(0.15), stk(M, 3)], [
        O('부팅', null, ['개전'], [defRun(0.15), stk(M, 4)]),
        O('양자 증폭', 2, [], [defRun(0.2), stk(M, 5), draw(2)]),
        O('나타 백업', null, [], [defRun(0.15), stk(M, 3), sh(1.0)]),
        O('표적 고정', null, [], [defRun(0.15), stk(M, 3), st('취약', 1)]),
        O('침묵의 해커', null, [], [defRun(0.2), stk(M, 3)]),
      ], [B('냉각 팬', 'defUp'), B('자동 시작', null, [], ['개전']), B('오버클럭', null, [stk(M, 1)])],
      { blurb: '말 대신 홀로그램으로 말한다' }),
    ],
  });
}

// 에르핀(왕도) — 끝내는 쪽(딜러 · 엘다인). 망설임 없는 왕도 — 앞을 막는 적 하나를 끝낼 때마다 왕마력이 차오른다(전투 내내 공격력).
{
  const k = '에르핀_왕도', M = '왕도의 걸림돌';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '왕도를 걷는 요정 여왕. 앞을 막는 적 하나를 지목해 끝내고, 끝낼 때마다 왕마력이 차오른다.',
    keyword: KW(M, '여왕이 지목한, 길을 막는 자', 3, [{ stat: 'taken', v: 0.15 }]),
    passives: [
      P('왕마력', { on: 'huntDown', who: 'any' }, [atkRun(0.15), top(M, 1)], null, { per: 'fight', n: 4 }),
      P('여왕의 지목', { on: 'fightStart' }, [top(M, 1)]),
    ],
    ult: { name: '돌겨어어어!!! 억⋯?', cost: 250, fx: [all(2.0), st('피해 감소', 3), st('약화', 2, 'allEnemies')] },
    cards: [
      U(`${k}_u1`, '대왕마력탄', 2, '공격', [], [d(2.2), hunted, d(0.8), brk, ap(1)], [
        O('왕마력탄', 1, [], [d(1.3), hunted, d(0.5), brk, ap(1)]),
        O('초대왕마력탄', 3, [], [d(3.4), hunted, d(1.2), brk, ap(2)]),
        O('왕도 직진', null, ['분쇄'], [d(2.4), hunted, d(0.8), brk, ap(1)]),
        O('여왕의 분노', null, [], [d(2.2), hunted, d(1.0), brk, draw(2)]),
        O('망설임 없이', null, [], [d(2.6), hunted, d(0.8)]),
      ], [B('왕관의 빛', 'power'), B('여왕 직권', 'cost'), B('케이크 한 입', null, [draw(1)])],
      { signature: true, blurb: '길을 막는 자에게 여왕은 망설이지 않는다' }),
      U(`${k}_u2`, '왕의 지목', 1, '스킬', [], [stk(M, 2), draw(1), hunted, ap(1)], [
        O('손가락질', 0, [], [stk(M, 1), draw(1), hunted, ap(1)]),
        O('칙령', 2, [], [stk(M, 3), st('취약', 2), hunted, ap(1)]),
        O('너다!', null, [], [stk(M, 3), draw(1), hunted, ap(1)]),
        O('여왕의 시선', null, [], [stk(M, 2), st('약화', 2), hunted, ap(1)]),
        O('왕실 문장', null, ['보존'], [stk(M, 2), draw(1), hunted, ap(1)]),
      ], [B('왕홀', 'draw'), B('위엄', null, [st('약화', 1)]), B('대관식', null, [], ['개전'])],
      { blurb: '「너, 비켜라.」 이미 찍힌 적이면 여왕의 발걸음이 빨라진다' }),
      U(`${k}_u3`, '여왕의 예복', 1, '스킬', [], [blk(2.0), hunted, st('피해 감소', 1)], [
        O('망토 자락', 0, [], [blk(1.2), hunted, st('피해 감소', 1)]),
        O('대관 예복', 2, [], [blk(4.0), hunted, st('피해 감소', 2)]),
        O('왕관 장식', null, [], [blk(2.0), stk(M, 1), hunted, st('피해 감소', 1)]),
        O('요정 날개', null, [], [blk(2.4), hunted, st('피해 감소', 1)]),
        O('위풍당당', null, [], [blk(2.0), hunted, st('피해 감소', 1), draw(1)]),
      ], [B('금실', 'guard'), B('여왕 직속 호위', null, [sh(0.5)]), B('예복 손질', null, [], ['보존'])],
      { blurb: '왕도는 다치지 않고 걷는 길이다' }),
      U(`${k}_u4`, '망설임 없는 왕도', 2, '강화', [], [atkRun(0.15), stk(M, 2)], [
        O('왕도의 첫걸음', null, ['개전'], [atkRun(0.15), stk(M, 3)]),
        O('왕도 완성', 3, [], [atkRun(0.2), stk(M, 3), ap(1)]),
        O('엘다인 각성', null, [], [atkRun(0.15), stk(M, 2), st('피해 감소', 2)]),
        O('왕의 길', null, [], [atkRun(0.15), stk(M, 2), draw(1)]),
        O('여왕 즉위', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('왕마력', 'atkUp'), B('개선 행진', null, [], ['개전']), B('왕실 연회', null, [heal(0.6)])],
      { blurb: '돌아가지 않는다. 막는 자가 있으면 넘는다' }),
    ],
  });
}

// ───────────────────────── 광기 ─────────────────────────
// 로니 — 찍는 쪽(딜러). 보안관 — 한 명만 현상범. 그 적을 잡으면 사기.
{
  const k = '로니', M = '현상수배';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '요정 보안관. 한 명만 현상범으로 지정해 쫓고, 잡으면 동료들의 사기가 오른다.',
    keyword: KW(M, '보안관이 붙인 수배 전단', 5, [{ stat: 'taken', v: 0.08 }]),
    passives: [
      P('검거', { on: 'huntDown', who: 'any' }, [st('사기', 1)], null, { per: 'fight', n: 2 }),
      P('다음 수배', { on: 'huntDown' }, [top(M, 2)]),
    ],
    ult: { name: '수사 종결', cost: 300, fx: [d(0.6, 6), st('피해 감소', 3), stk(M, 3)] },
    cards: [
      U(`${k}_u1`, '현상범 지정', 1, '스킬', [], [stk(M, 3), st('약화', 1)], [
        O('수배 전단', 0, [], [stk(M, 2), st('약화', 1)]),
        O('1급 수배', 2, [], [stk(M, 5), st('약화', 2), st('취약', 2)]),
        O('증거 확보', null, [], [stk(M, 3), st('약화', 1), draw(1)]),
        O('현상금 인상', null, [], [stk(M, 4), st('약화', 1)]),
        O('보안관 직감', null, ['개전'], [stk(M, 3), st('약화', 2)]),
      ], [B('보안관 배지', 'draw'), B('수갑', null, [st('약화', 1)]), B('잠복 수사', null, [], ['보존'])],
      { signature: true, blurb: '현상범은 한 명뿐이다. 보안관은 그 녀석만 본다' }),
      U(`${k}_u2`, '톤파 연타', 1, '공격', [], [d(0.55, 2), hunted, stk(M, 1)], [
        O('톤파 한 방', 0, [], [d(0.6), hunted, stk(M, 1)]),
        O('톤파 난타', 2, [], [d(0.6, 4), hunted, stk(M, 2)]),
        O('제압', null, [], [d(0.55, 2), tough(1), hunted, stk(M, 1)]),
        O('추궁', null, [], [d(0.55, 2), st('약화', 1), hunted, stk(M, 1)]),
        O('집요한 보안관', null, [], [d(0.65, 2), hunted, stk(M, 1)]),
      ], [B('강철 톤파', 'power'), B('기세', 'frost'), B('경찰봉 돌리기', null, [blk(0.8)])],
      { blurb: '현상범이면 한 대 더. 수배 전단도 한 장 더' }),
      U(`${k}_u3`, '수사 종결', 2, '공격', [], [d(1.0), per(M), d(0.35), brk, ap(1)], [
        O('구두 경고', 1, [], [d(0.6), per(M), d(0.25), brk, ap(1)]),
        O('최종 판결', 3, [], [d(1.6), per(M), d(0.55), brk, ap(2)]),
        O('현장 검거', null, [], [d(1.0), per(M), d(0.35), brk, draw(2)]),
        O('미란다 원칙', null, [], [d(1.0), per(M), d(0.35), st('약화', 2)]),
        O('사건 파일', null, [], [d(1.2), per(M), d(0.4), brk, ap(1)]),
      ], [B('결정적 증거', 'power'), B('수사 일지', null, [draw(1)]), B('보안관 모자', null, [], ['보존'])],
      { blurb: '수배 전단이 많을수록 판결은 무겁다' }),
      U(`${k}_u4`, '보안관 선서', 1, '강화', [], [atkRun(0.1), stk(M, 2)], [
        O('취임식', null, ['개전'], [atkRun(0.1), stk(M, 3)]),
        O('보안관의 이름으로', 2, [], [atkRun(0.15), stk(M, 4), st('사기', 1)]),
        O('순찰', null, [], [atkRun(0.1), stk(M, 2), draw(1)]),
        O('엄폐 사격', null, [], [atkRun(0.1), stk(M, 2), st('피해 감소', 2)]),
        O('정의 구현', null, [], [atkRun(0.15), stk(M, 2)]),
      ], [B('은 배지', 'atkUp'), B('선서문', null, [], ['개전']), B('카우보이 모자', null, [blk(1.0)])],
      { blurb: '요정 마을의 보안관. 법은 내가 지킨다' }),
    ],
  });
}

// 하이디 — 끝내는 쪽(딜러). 셔터 찬스를 노리는 기자 — 한 적을 따라 찍고, 그 적이 쓰러지는 순간(파괴)이 특종.
{
  const k = '하이디', M = '특종 거리';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '셔터 찬스를 노리는 엘프 기자. 한 적만 따라 찍다가, 쓰러지는 순간을 특종으로 터뜨린다.',
    keyword: KW(M, '렌즈에 잡아 둔 취재 대상', 5, [{ stat: 'taken', v: 0.06 }]),
    passives: [
      P('특종!', { on: 'huntDown', who: 'any' }, [gauge(20), draw(2)], null, turn1),
      P('다음 취재', { on: 'huntDown' }, [low(M, 2)]),
    ],
    ult: { name: '현장 중계 중!', cost: 200, fx: [st('피해 감소', 3), st('표식', 2), d(0.4, 5)] },
    cards: [
      U(`${k}_u1`, '결정적 순간', 2, '공격', [], [d(2.2), brk, draw(2), ap(1)], [
        O('스냅샷', 1, [], [d(1.3), brk, draw(1), ap(1)]),
        O('퓰리처 각', 3, [], [d(3.6), brk, draw(2), ap(2)]),
        O('찍은 대로', null, [], [d(2.2), hunted, d(0.6), brk, ap(1)]),
        O('타이밍', null, [], [d(2.6), brk, draw(2), ap(1)]),
        O('단독 보도', null, [], [d(2.2), st('취약', 1), brk, ap(1)]),
      ], [B('셔터 스피드', 'power'), B('플래시', 'frost'), B('메모리 카드', null, [], ['보존'])],
      { signature: true, blurb: '쓰러지는 그 순간을 놓치지 않는다. 그게 특종이다' }),
      U(`${k}_u2`, '줌 인', 0, '스킬', [], [stk(M, 2), draw(1)], [
        O('초점 맞추기', null, [], [stk(M, 2), draw(1), blk(0.6)]),
        O('망원 렌즈', 1, [], [stk(M, 4), draw(1), st('취약', 1)]),
        O('밀착 취재', null, [], [stk(M, 3), draw(1)]),
        O('잠복 취재', null, ['보존'], [stk(M, 2), draw(1), cheap(1)]),
        O('셀카 모드', null, [], [stk(M, 2), draw(1), st('피해 감소', 1)]),
      ], [B('자동 초점', 'draw'), B('기자 수첩', null, [blk(0.6)]), B('취재 허가증', null, [], ['개전'])],
      { blurb: '한 놈을 화면 가득 잡는다' }),
      U(`${k}_u3`, '연사 셔터', 1, '공격', [], [d(0.4, 3), hunted, d(0.4, 1)], [
        O('한 컷', 0, [], [d(0.45), hunted, d(0.45)]),
        O('고속 연사', 2, [], [d(0.4, 6), hunted, d(0.4, 2)]),
        O('플래시 세례', null, [], [d(0.4, 3), st('약화', 1), hunted, d(0.4, 1)]),
        O('따라 찍기', null, [], [d(0.4, 3), stk(M, 1), hunted, d(0.4, 1)]),
        O('베스트 컷', null, [], [d(0.45, 3), hunted, d(0.45, 1)]),
      ], [B('셔터 연타', 'power'), B('반사판', 'weakSpot'), B('예비 배터리', null, [draw(1)])],
      { blurb: '찰칵찰칵찰칵. 찍힌 놈이면 한 컷 더' }),
      U(`${k}_u4`, '현장 중계', 2, '강화', [], [atkRun(0.15), stk(M, 2), draw(1)], [
        O('생방송 시작', null, ['개전'], [atkRun(0.15), stk(M, 2), draw(1)]),
        O('특별 생방송', 3, [], [atkRun(0.2), stk(M, 4), draw(2)]),
        O('시청률 폭등', null, [], [atkRun(0.15), stk(M, 2), gauge(20)]),
        O('기자 정신', null, [], [atkRun(0.15), critRun(0.1), draw(1)]),
        O('속보', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('특종상', 'atkUp'), B('보도 완장', null, [], ['개전']), B('삼각대', null, [blk(1.0)])],
      { blurb: '현장에서 직접 전해 드립니다' }),
    ],
  });
}

// 마요 — 끝내는 쪽(딜러). 한 번 들어온 물건은 안 돌려주는 수집가 — 마취한 적을 쓰러뜨리면 수집품(전투 내내 공격력).
{
  const k = '마요', M = '마취';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '요정 수집가. 독침으로 한 놈을 마취해 두고, 쓰러뜨리면 그 녀석은 수집품이 된다 — 안 돌려준다.',
    keyword: KW(M, '독침에 퍼진 마취 — 손발이 굼떠진다', 4, [{ stat: 'dealt', v: -0.08 }]),
    passives: [
      P('수집품', { on: 'huntDown', who: 'any' }, [atkRun(0.15), draw(1)], null, { per: 'fight', n: 4 }),
      P('다음 표본', { on: 'huntDown' }, [low(M, 2)]),
    ],
    ult: { name: '그 수집품 내꺼임', cost: 250, fx: [rnd(0.5, 8), stk(M, 4)] },
    cards: [
      U(`${k}_u1`, '마취 독침', 1, '공격', [], [d(0.9), stk(M, 2)], [
        O('바늘', 0, [], [d(0.5), stk(M, 1)]),
        O('맹독 대침', 2, [], [d(2.0), stk(M, 4)]),
        O('이중 독침', null, [], [d(0.5, 2), stk(M, 2)]),
        O('진한 마취', null, [], [d(0.9), stk(M, 3)]),
        O('신경 독', null, [], [d(0.9), stk(M, 2), st('약화', 1)]),
      ], [B('독샘', 'power'), B('마비', 'frost'), B('채집통', null, [], ['개전'])],
      { signature: true, blurb: '찔린 녀석은 굼떠진다. 마요는 이미 진열장 자리를 보고 있다' }),
      U(`${k}_u2`, '수집 개시', 2, '공격', [], [d(1.2), per(M), d(0.4), brk, draw(1)], [
        O('채집망', 1, [], [d(0.8), per(M), d(0.3), brk, draw(1)]),
        O('대수집', 3, [], [d(2.0), per(M), d(0.6), brk, draw(2)]),
        O('표본 고정', null, [], [d(1.2), per(M), d(0.4), tough(1)]),
        O('희귀 표본', null, [], [d(1.4), per(M), d(0.45), brk, draw(1)]),
        O('진열 준비', null, [], [d(1.2), per(M), d(0.4), brk, ap(1)]),
      ], [B('수집가의 눈', 'power'), B('라벨', null, [draw(1)]), B('핀셋', 'weakSpot')],
      { blurb: '마취가 깊을수록 수집은 손쉽다' }),
      U(`${k}_u3`, '진열장 뒤로', 1, '스킬', [], [blk(2.0), hunted, stk(M, 1)], [
        O('유리문', 0, [], [blk(1.2), hunted, stk(M, 1)]),
        O('수장고', 2, [], [blk(4.0), hunted, stk(M, 2)]),
        O('먼지떨이', null, [], [blk(2.0), st('약화', 1), hunted, stk(M, 1)]),
        O('잠금장치', null, [], [blk(2.4), hunted, stk(M, 1)]),
        O('컬렉션 정리', null, [], [blk(2.0), draw(1), hunted, stk(M, 1)]),
      ], [B('강화 유리', 'guard'), B('독 바른 유리', null, [st('약화', 1)]), B('진열 순서', null, [], ['보존'])],
      { blurb: '수집품 뒤에 숨는다. 수집품은 안 다치게' }),
      U(`${k}_u4`, '안 돌려줌', 2, '강화', [], [atkRun(0.15), stk(M, 2)], [
        O('첫 수집품', null, ['개전'], [atkRun(0.15), stk(M, 3)]),
        O('박물관 개관', 3, [], [atkRun(0.2), stk(M, 4), draw(2)]),
        O('소유권 주장', null, [], [atkRun(0.15), stk(M, 2), draw(1)]),
        O('수집가의 집착', null, [], [atkRun(0.15), critRun(0.1)]),
        O('작은 진열장', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('수집 목록', 'atkUp'), B('명패', null, [], ['개전']), B('보존 처리', null, [stk(M, 1)])],
      { blurb: '한 번 들어온 물건은 절대 안 돌려준다' }),
    ],
  });
}

// ───────────────────────── 우울 ─────────────────────────
// 베루 — 찍는 쪽(딜러). 흠 하나를 잡아 물고 늘어진다 — 찍은 적의 흠을 끝까지 판다.
{
  const k = '베루', M = '결점';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '교주의 천벌을 자처하는 유령. 한 놈의 흠을 잡으면 끝까지 파고든다 — 흠이 깊을수록 받는 피해가 커진다.',
    keyword: KW(M, '잡아낸 흠 — 파고들수록 벌어진다', 6, [{ stat: 'taken', v: 0.08 }]),
    passives: [
      P('트집', { on: 'play', type: '공격', every: 2 }, [stk(M, 1)]),
      P('다음 흠', { on: 'huntDown' }, [top(M, 3)]),
    ],
    ult: { name: '교주의 천벌 - 베루', cost: 250, fx: [stk(M, 4), d(4.0)] },
    cards: [
      U(`${k}_u1`, '흠 잡기', 1, '스킬', [], [stk(M, 3), st('취약', 1)], [
        O('눈 흘기기', 0, [], [stk(M, 2), st('취약', 1)]),
        O('흠 백과사전', 2, [], [stk(M, 5), st('취약', 2), draw(1)]),
        O('꼬투리', null, [], [stk(M, 4), st('취약', 1)]),
        O('뒤끝', null, [], [stk(M, 3), st('취약', 1), draw(1)]),
        O('천벌 예고', null, ['개전'], [stk(M, 3), st('취약', 2)]),
      ], [B('매서운 눈', 'draw'), B('트집 수첩', null, [st('약화', 1)]), B('뒤끝 작렬', null, [], ['보존'])],
      { signature: true, blurb: '흠은 하나면 충분하다. 거기서부터 판다' }),
      U(`${k}_u2`, '파고들기', 1, '공격', [], [d(0.8), per(M), d(0.12)], [
        O('찔러 보기', 0, [], [d(0.4), per(M), d(0.1)]),
        O('끝까지 파기', 2, [], [d(1.4), per(M), d(0.25)]),
        O('후벼 파기', null, [], [d(0.8), per(M), d(0.12), stk(M, 1)]),
        O('갈라진 틈', null, [], [d(0.8), per(M), d(0.12), tough(1)]),
        O('손도끼', null, [], [d(1.0), per(M), d(0.14)]),
      ], [B('예리한 손도끼', 'power'), B('집요함', 'weakSpot'), B('흠집 기록', null, [stk(M, 1)])],
      { blurb: '흠이 깊을수록 손도끼가 깊이 들어간다' }),
      U(`${k}_u3`, '숨기', 1, '스킬', [], [blk(2.0), hunted, stk(M, 2)], [
        O('그림자', 0, [], [blk(1.2), hunted, stk(M, 1)]),
        O('완전 은신', 2, [], [blk(4.0), hunted, stk(M, 3)]),
        O('엿보기', null, [], [blk(2.0), draw(1), hunted, stk(M, 2)]),
        O('벽 너머', null, [], [blk(2.4), hunted, stk(M, 2)]),
        O('유령의 몸', null, [], [blk(2.0), st('피해 감소', 1), hunted, stk(M, 2)]),
      ], [B('투명', 'guard'), B('으스스', null, [st('약화', 1)]), B('구석 자리', null, [], ['보존'])],
      { blurb: '숨어서 본다. 흠은 숨어서 볼 때 더 잘 보인다' }),
      U(`${k}_u4`, '천벌의 대리인', 2, '강화', [], [atkRun(0.15), stk(M, 3)], [
        O('대리인 임명', null, ['개전'], [atkRun(0.15), stk(M, 3)]),
        O('천벌 집행', 3, [], [atkRun(0.2), stk(M, 5), st('취약', 2)]),
        O('교주님 대신', null, [], [atkRun(0.15), stk(M, 3), draw(1)]),
        O('우울한 확신', null, [], [atkRun(0.15), critRun(0.15)]),
        O('작은 천벌', 1, [], [atkRun(0.1), stk(M, 2)]),
      ], [B('천벌 인장', 'atkUp'), B('검은 리본', null, [], ['개전']), B('저주 노트', null, [stk(M, 1)])],
      { blurb: '교주님 대신 천벌을 내린다(고 믿는다)' }),
    ],
  });
}

// 키디언 — 끝내는 쪽(딜러 · 전열). 흑요석 칼끝 — 찍은 적의 급소를 찾고, 그 적에게만 치명이 꽂힌다.
{
  const k = '키디언', M = '급소';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '흑요석 용족 닌자. 한 놈의 급소를 찾아 두고, 그 적을 칠 때만 칼끝이 치명으로 꽂힌다.',
    keyword: KW(M, '찾아 둔 급소 — 흑요석 칼끝이 노린다', 4, [{ stat: 'taken', v: 0.12 }]),
    passives: [
      P('그림자 추적', { on: 'huntDown', who: 'any' }, [top(M, 2), blk(1.5)], null, turn1),
      P('흑요석', { on: 'fightStart' }, [top(M, 1)]),
    ],
    ult: { name: '쉐도우 다이브', cost: 150, fx: [st('잔불', 1), d(2.5), stk(M, 2)] },
    cards: [
      U(`${k}_u1`, '급소 찌르기', 1, '공격', [], [hunted, crit(0.5), d(1.3)], [
        O('바늘 찌르기', 0, [], [hunted, crit(0.5), d(0.8)]),
        O('흑요석 관통', 2, [], [hunted, crit(0.5), d(2.8)]),
        O('이중 찌르기', null, [], [hunted, crit(0.5), d(0.75, 2)]),
        O('숨통', null, [], [stk(M, 1), hunted, crit(0.5), d(1.4)]),
        O('그림자 일격', null, [], [hunted, crit(0.6), d(1.5)]),
      ], [B('흑요석 날', 'power'), B('독 바른 칼끝', 'weakSpot'), B('칼집', null, [], ['보존'])],
      { signature: true, blurb: '찍힌 적이면 칼끝이 급소로 간다' }),
      U(`${k}_u2`, '급소 찾기', 0, '스킬', [], [stk(M, 2), draw(1)], [
        O('눈 감고 듣기', null, [], [stk(M, 2), draw(1), blk(0.6)]),
        O('완벽한 해부', 1, [], [stk(M, 4), draw(2)]),
        O('약점 노출', null, [], [stk(M, 2), st('취약', 1), draw(1)]),
        O('그림자 관찰', null, [], [stk(M, 3), draw(1)]),
        O('인내', null, ['보존'], [stk(M, 2), draw(1), cheap(1)]),
      ], [B('예리한 눈', 'draw'), B('숨 죽이기', null, [blk(0.6)]), B('잠입', null, [], ['개전'])],
      { blurb: '한 번에 하나. 급소는 하나면 된다' }),
      U(`${k}_u3`, '거대 쿠나이', 2, '공격', [], [per(M), d(0.5), d(1.2), brk, ap(1)], [
        O('쿠나이 투척', 1, [], [per(M), d(0.4), d(0.6), brk, ap(1)]),
        O('흑요석 폭풍', 3, [], [per(M), d(0.8), d(2.0), brk, ap(2)]),
        O('관통 쿠나이', null, ['분쇄'], [per(M), d(0.55), d(1.2), brk, ap(1)]),
        O('회수 쿠나이', null, [], [per(M), d(0.5), d(1.2), brk, draw(2)]),
        O('급소 직격', null, [], [per(M), d(0.6), d(1.3), brk, ap(1)]),
      ], [B('무게 중심', 'power'), B('그림자 분신', null, [draw(1)]), B('쿠나이 줄', 'cost')],
      { blurb: '급소가 드러난 만큼 깊이 박힌다' }),
      U(`${k}_u4`, '그림자 수련', 1, '강화', [], [critRun(0.1), stk(M, 2)], [
        O('새벽 수련', null, ['개전'], [critRun(0.1), stk(M, 3)]),
        O('비전 오의', 2, [], [critRun(0.15), atkRun(0.1), stk(M, 3)]),
        O('흑요석 연마', null, [], [critRun(0.1), stk(M, 2), draw(1)]),
        O('그림자 숨기', null, [], [critRun(0.1), stk(M, 2), blk(1.5)]),
        O('닌자의 길', null, [], [critRun(0.15), stk(M, 2)]),
      ], [B('검은 두건', 'atkUp'), B('수련 일지', null, [], ['개전']), B('표창', null, [stk(M, 1)])],
      { blurb: '그림자 속에서 칼끝을 간다' }),
    ],
  });
}

// 리온 — 찍는 쪽(탱커). 빌런을 판결하는 히어로 — 판결받은 빌런 하나의 공격을 대신 받아 되친다.
{
  const k = '리온', M = '판결';
  out[k] = hero(k, {
    style: STYLE,
    blurb: '빌런을 판결하는 사자 히어로. 빌런 하나에게 판결을 내리고, 그 녀석의 주먹을 대신 받아 되친다.',
    keyword: KW(M, '히어로가 내린 판결 — 빌런의 주먹이 무뎌진다', 3, [{ stat: 'dealt', v: -0.1 }]),
    passives: [
      P('정의의 반격', { on: 'hurt' }, [st('반격', 1)], null, turn1),
      P('다음 빌런', { on: 'huntDown', who: 'any' }, [top(M, 2), sh(1.5)], null, turn1),
    ],
    ult: { name: '즉결심판', cost: 200, fx: [strip('allEnemies'), dd(0.4, 3, 'allEnemies'), stk(M, 3)] },
    cards: [
      U(`${k}_u1`, '판결 선고', 1, '스킬', [], [stk(M, 2), blk(2.0)], [
        O('경고', 0, [], [stk(M, 1), blk(1.2)]),
        O('최종 판결', 2, [], [stk(M, 3), blk(4.0), st('반격', 1)]),
        O('유죄', null, [], [stk(M, 3), blk(2.0)]),
        O('히어로 선언', null, [], [stk(M, 2), blk(2.0), st('반격', 1)]),
        O('법정 출두', null, ['개전'], [stk(M, 2), blk(2.4)]),
      ], [B('판결봉', 'guard'), B('정의의 눈', null, [st('약화', 1)]), B('법전', null, [], ['보존'])],
      { signature: true, blurb: '「너는 유죄다.」 판결받은 빌런의 주먹은 무뎌진다' }),
      U(`${k}_u2`, '대신 받기', 1, '스킬', [], [blk(2.5), hunted, st('반격', 1)], [
        O('막아서기', 0, [], [blk(1.4), hunted, st('반격', 1)]),
        O('사자의 갈기', 2, [], [blk(5.0), hunted, st('반격', 2)]),
        O('히어로 가드', null, [], [blk(3.0), hunted, st('반격', 1)]),
        O('물러서지 않는다', null, [], [blk(2.5), st('피해 감소', 1), hunted, st('반격', 1)]),
        O('동료를 위해', null, [], [blk(2.5), sh(0.8), hunted, st('반격', 1)]),
      ], [B('강철 방패', 'guard'), B('포효', null, [st('약화', 1)]), B('히어로 망토', null, [], ['개전'])],
      { blurb: '빌런의 주먹은 히어로가 받는다. 그리고 돌려준다' }),
      U(`${k}_u3`, '정의의 철퇴', 2, '공격', [], [dd(1.4), hunted, dd(0.6), tough(1)], [
        O('판결봉 휘두르기', 1, [], [dd(0.9), hunted, dd(0.4), tough(1)]),
        O('사자후', 3, [], [dd(2.3), hunted, dd(1.0), tough(2)]),
        O('빌런 퇴치', null, ['분쇄'], [dd(1.5), hunted, dd(0.6), tough(1)]),
        O('정의 집행', null, [], [dd(1.4), stk(M, 1), hunted, dd(0.7)]),
        O('히어로 펀치', null, [], [dd(1.7), hunted, dd(0.6)]),
      ], [B('정의의 무게', 'power'), B('갈기 휘날리며', 'frost'), B('승리 포즈', null, [blk(1.0)])],
      { blurb: '판결받은 빌런에게 히어로의 철퇴가 떨어진다' }),
      U(`${k}_u4`, '히어로 선서', 1, '강화', [], [defRun(0.15), stk(M, 2)], [
        O('첫 출동', null, ['개전'], [defRun(0.15), stk(M, 3)]),
        O('최강 히어로', 2, [], [defRun(0.2), stk(M, 3), st('반격', 2)]),
        O('수호 맹세', null, [], [defRun(0.15), stk(M, 2), sh(1.0)]),
        O('사자의 긍지', null, [], [atkRun(0.2), stk(M, 2)]),
        O('우울한 히어로', null, [], [defRun(0.2), stk(M, 2)]),
      ], [B('히어로 슈트', 'defUp'), B('변신', null, [], ['개전']), B('사인 공세', null, [sh(0.5)])],
      { blurb: '빌런이 있는 곳에 히어로가 있다' }),
    ],
  });
}

for (const [k, v] of Object.entries(out)) write(DIR, k, v);
console.log('썼다', Object.keys(out).length);
