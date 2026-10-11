if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 변신이 기본 카드를 바꾸던 것을 「변신 카드 생성」으로(2026-10-11 사용자 — 기본 카드는 어차피 뺀다).
// 변신 카드(_fN 토큰)는 증발 · 소멸 — 변신에 드는 그 자리(form 효과 바로 뒤)와 변신 동안 매 턴 시작에 1장씩 손으로. 변신이 끝나면 더 생기지 않는다(변신 패시브).
// 고유 카드를 바꾸는 것(디아나(왕년) 고유 1)은 그대로. node _gen/deep/formmake.mjs <데이터 폴더>   (몇 번 돌려도 같다)
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
const NAME = '변신 카드';
const walk = (fx, cb) => { if (!fx) return; for (let i = 0; i < fx.length; i++) { cb(fx, i); walk(fx[i].then, cb); for (const r of fx[i].rules || []) walk(r.fx, cb); } };
const out = [];
for (const r of fs.readdirSync(`${dir}/heroes`)) for (const f of fs.readdirSync(`${dir}/heroes/${r}`)) {
  if (!f.endsWith('.json')) continue;
  const p = `${dir}/heroes/${r}/${f}`; const s0 = fs.readFileSync(p, 'utf8'); const j = JSON.parse(s0); const h = j.heroes[0];
  for (const fm of h.forms || []) {
    const pairs = Object.entries(fm.cards || {}).filter(([k]) => /_s\d+$/.test(k)); if (!pairs.length) continue;
    for (const [k, tok] of pairs) {
      delete fm.cards[k];
      const t = j.cards.find(c => c.id === tok); if (t) { t.token = true; t.tags = [...new Set([...(t.tags || []).filter(x => x !== '보존'), '증발', '소멸'])]; }
      fm.passives = (fm.passives || []).filter(x => !(x.name === NAME && x.fx.some(q => q.id === tok)));
      fm.passives.push({ name: NAME, when: { on: 'turnStart' }, fx: [{ k: 'make', id: tok, v: 1 }] });
      // 드는 그 자리 — 이 변신을 여는 효과 바로 뒤에 1장
      const add = fx => walk(fx, (arr, i) => { if (arr[i].k === 'form' && arr[i].id === fm.id && !(arr[i + 1] && arr[i + 1].k === 'make' && arr[i + 1].id === tok)) arr.splice(i + 1, 0, { k: 'make', id: tok, v: 1 }); });
      for (const c of j.cards) { add(c.fx); for (const o of c.oracles || []) add(o.fx); }
      if (h.ult) add(h.ult.fx);
      for (const rr of [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])]) add(rr.fx);
      out.push(`${h.id} 「${fm.name}」: ${k} → ${tok} 바꾸기 ⇒ 드는 자리 + 매 턴 「${t ? t.name : tok}」 1장(증발 · 소멸)`);
    }
    if (fm.cards && !Object.keys(fm.cards).length) delete fm.cards;
  }
  const s1 = JSON.stringify(j, null, 2) + '\n'; if (s1 !== s0) { fs.writeFileSync(p + '.tmp', s1); fs.renameSync(p + '.tmp', p); }
}
console.log(out.join('\n') || '변신 카드 생성 — 바꿀 것 없음');
