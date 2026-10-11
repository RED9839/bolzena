// 틀 다양성(2026-10-10 사용자 결정) — 「스택을 전부 써서 1개당 n% 피해 · 회복 · 실드」 틀은 리뉴얼 사도의 2할 상한.
// 남긴 21명(원작상 「모았다가 한 방에 쏟는」 사도) 밖의 사도는 그 카드(본판 + 같은 꼴 신탁)를 원작에 맞는 다른 방식으로 바꾼다.
// lib.run 이 기본 4장 뒤, 배율 앞에 부른다(applyDiverse). 방식 표는 00_기준 §11 「틀 다양성」.
const SPEND_KEEP = ['버터', '에르핀', '시온더다크불릿', '리츠', '마리', '모모', '에슈르_마도', '다야_퓨어샤인', '셀리네', '앨리스', '아야', '란', '티그_영웅', '코미', '실라', '하이디', '레비_졸업', '스피키_메이드', '라이카', '뮤트', '로니'];
// 마리는 「기폭 스위치」 만 남기고 「숨겨 둔 한 방」 은 바꾼다
const ONLY = { 마리: ['숨겨 둔 한 방'] };
export const MECH = {
  문턱: ['벨벳', '스노키', '리온'],                        // 쌓인 만큼이 아니라 N 이상이면 크게 — 쌓기는 남는다
  지연: ['포셔', '디아나', '힐데', '아라그니아', '엘레나'],  // 다음 턴에 터진다(약효 · 치유가 늦게 돈다)
  함정: ['프리클', '그윈', '바롱', '잉클'],                  // 적이 행동할 때 터진다
  박자: ['루포', '림', '아르코', '샤샤'],                    // 이번 턴 셋째 카드로 내면
  연속: ['디아나_왕년', '베니_베니', '키디언'],              // 같은 사도의 카드를 잇달아 내면
  대가: ['셰럼', '이드_재활', '아일라', '비비'],             // HP 를 치르고 크게
  도박: ['롤렛', '리스티', '스키아'],                        // 둘 중 하나 — 크게 터지거나 빗나가거나
  한번더: ['에피카', '비비_신성', '코미_수영복', '멜루나'],  // N 이상이면 이 카드를 한 번 더(절반)
  선물: ['벨라', '오팔', '실비아', '로네_시장'],              // 동료가 한 장 뽑는다
  의도: ['피라', '우로스', '아멜리아'],                      // 적의 다음 공격이 다른 적에게(혼란)
  장수: ['헤일리_멀쩡', '아이시아', '에르핀_왕도', '마리'],  // 이번 턴 낸 장수만큼
  소멸: ['우이_기억', '림_혼돈', '제이드'],                  // 이번 전투 소멸한 장수만큼
  반격: ['이드', '오로라', '슈로'],                          // 받은 피해를 되돌린다
};
const HERO_MECH = Object.fromEntries(Object.entries(MECH).flatMap(([m, hs]) => hs.map(h => [h, m])));
const OUT = new Set(['dmg', 'heal', 'shield']);
const R2 = x => Math.round(x * 100) / 100;
/** fx 안의 「per(K) → X(r) … spend all K」 를 방식에 맞게 바꾼다. 꼴이 없으면 그대로. */
function conv(fx, m, needSpend = true, K0 = null) {
  const i = fx.findIndex(f => f.k === 'perStack');
  if (i < 0) return null;
  const K = fx[i].id, s = fx.findIndex(f => f.k === 'spend' && f.all && f.id === K), x = fx.findIndex((f, n) => n > i && OUT.has(f.k));
  if (x < 0 || (needSpend && s < 0) || (K0 && K !== K0)) return null;
  const X = fx[x], rest = fx.filter((f, n) => n !== i && n !== x && n !== s);
  const Xr = r => ({ ...X, ratio: R2(X.ratio * r) });
  const at = Math.min(i, rest.length);
  const isCond = f => /^(if|per)/.test(f.k);
  const put = (...items) => {
    const r2 = [...rest];
    // 카드당 효과 셋까지 — 넘치면 덧붙은 효과(조건 아닌 것)를 뒤에서부터 덜어 낸다
    while (r2.filter(f => !isCond(f)).length + items.filter(f => !isCond(f)).length > 3) { const k = r2.map(isCond).lastIndexOf(false); if (k < 0) break; r2.splice(k, 1); }
    const a2 = Math.min(at, r2.length);
    return [...r2.slice(0, a2), ...items, ...r2.slice(a2)];
  };
  switch (m) {
    case '문턱': return put({ k: 'ifStack', id: K, n: 3 }, Xr(3));
    case '지연': return put({ k: 'later', n: 1, then: [Xr(3.5)] });
    case '함정': return put({ k: 'trap', target: X.k === 'dmg' && X.target && X.target !== 'allEnemies' ? X.target : 'oneEnemy', then: [Xr(3.5)] });
    case '박자': return put({ k: 'ifNth', n: 3 }, Xr(3.5));
    case '연속': return put({ k: 'ifPrevSame' }, Xr(3.5));
    case '대가': return put({ k: 'payHpPct', v: 0.03 }, Xr(4));
    case '도박': return put({ k: 'roll', n: 2 }, { k: 'ifRoll', n: 1 }, Xr(6));
    case '한번더': return put(Xr(2), { k: 'ifStack', id: K, n: 3 }, { k: 'recast', ratio: 0.5 });
    case '선물': return put(Xr(2.5), { k: 'draw', v: 1, who: 'other' });
    case '의도': return put(Xr(2.5), { k: 'confuse', target: 'oneEnemy' });
    case '장수': return put({ k: 'perPlayed' }, Xr(0.9));
    case '소멸': return put({ k: 'perGone', who: 'self' }, Xr(0.6));
    case '반격': return put(Xr(2), { k: 'reflect', ratio: 0.5 });
  }
  return null;
}
export function applyDiverse(j) {
  const h = j.heroes[0]; const m = HERO_MECH[h.id];
  if (!m) return;
  if (SPEND_KEEP.includes(h.id) && !ONLY[h.id]) return;
  for (const c of j.cards.filter(c => c.hero === h.id && c.unique)) {
    if (ONLY[h.id] && !ONLY[h.id].includes(c.name)) continue;
    const KC = (c.fx.find(f => f.k === 'perStack') || {}).id;
    const nb = conv(c.fx, m); if (!nb) continue;
    c.fx = nb;
    for (const o of c.oracles || []) { const no = conv(o.fx, m, false, KC); if (no) o.fx = no; }   // 같은 고유 효과를 1개당으로 세는 신탁은 소모가 없어도 같은 방식으로
  }
  // 손질 — 바꾼 뒤 값어치가 모자란 신탁
  const fix = { '롤렛_u3': c => { delete c.oracles[4].cost; }, '바롱_u3': c => { const o = c.oracles[4]; o.fx = o.fx.filter(f => f.target !== 'otherEnemy'); o.fx.push({ k: 'status', id: '약화', v: 1, target: 'oneEnemy' }); }, '오로라_u4': c => { c.oracles[3].fx = [{ k: 'heal', ratio: 0.7 }, { k: 'reflect', ratio: 0.9 }]; delete c.oracles[0].numOnly; c.oracles[0].fx = [{ k: 'heal', ratio: 0.8 }, { k: 'cleanse', v: 2 }, { k: 'reflect', ratio: 0.9 }]; } };
  for (const c of j.cards) if (fix[c.id]) fix[c.id](c);
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) { const nr = conv(r.fx, m); if (nr) r.fx = nr; }
  for (const p of h.passives || []) { const np = conv(p.fx, m); if (np) p.fx = np; }
}
export const KEEP = SPEND_KEEP;
