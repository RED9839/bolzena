if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 글 45자 넘침 손질(2026-10-10 — 역할 계수 · 완성형으로 수치가 세 자리가 된 카드) — 덜 중요한 효과 하나를 뺀다(값어치는 뒤의 valfix 가 수치로 채운다)
// node _gen/deep/textfix.mjs <데이터 폴더>   (몇 번 돌려도 같다 — 없으면 건너뜀)
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
// [사도 파일, 카드, 신탁 번호(0 = 카드 자체), 뺄 효과(k[:id])]
const CUT = [['수인/그윈', '그윈_u4', 4, 'shield'], ['엘프/엘레나', '엘레나_u3', 4, 'draw'], ['요정/캬롯', '캬롯_u3', 4, 'shield'],
  ['용족/다야_퓨어샤인', '다야_퓨어샤인_u3', 0, 'status:피해 감소'], ['용족/비비', '비비_u4', 0, 'status:결의'], ['정령/뮤트', '뮤트_u2', 0, 'spend:홀로그램'], ['정령/시저', '시저_u4', 2, 'dealtMod']];
for (const [f, id, n, what] of CUT) {
  const p = `${dir}/heroes/${f}.json`; if (!fs.existsSync(p)) continue;
  const j = JSON.parse(fs.readFileSync(p, 'utf8')); const c = j.cards.find(c => c.id === id); if (!c) continue;
  const o = n ? c.oracles && c.oracles[n - 1] : c; if (!o) continue;
  const [k, sid] = what.split(':'); const before = o.fx.length;
  o.fx = o.fx.filter(q => !(q.k === k && (!sid || q.id === sid)));
  if (o.fx.length !== before) { fs.writeFileSync(p + '.tmp', JSON.stringify(j, null, 2) + '\n'); fs.renameSync(p + '.tmp', p); console.log('글 손질', id, n || '카드', what); }
}
