if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 원본을 그대로 두는 사도(boost 손실로 다시 생성하지 않음 — 에르핀 · 캬롯 · 셰이디 · 시저)에 구조 손질 · 기본 4장을 원본 파일에 바로
// node _gen/deep/apply_struct.mjs <데이터 폴더> 정령/시저 …   (몇 번 돌려도 같은 결과 — 기본 4장은 모자랄 때만 더한다. 구조 손질은 값을 정해 넣는다)
import fs from 'fs'; import { STRUCT } from './struct.mjs'; import { addBasics } from './basics4.mjs';
import { applyDiverse } from './diverse.mjs';
import { applyFeedback, dedupe } from './fb.mjs';
import { mergePassives } from './merge_pas.mjs';
import { extraDraw, tune, TUNE } from './lib.mjs';
import { dealerRole } from './dealer.mjs';
import { roleCoef } from './rolecoef.mjs';
const [dir, ...keys] = process.argv.slice(2); if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
// 원본 그대로 두는 사도의 손 보정(다시 생성하지 않아 boost 가 안 닿는다) — 값을 정해 넣어 몇 번 돌려도 같다
const FIX = { '요정/에르핀': j => { const c = j.cards.find(c => c.id === '에르핀_u3'); const d = c && c.oracles[2].fx.find(f => f.k === 'dmg'); if (d) d.ratio = 1.15; } };
for (const k of keys) {
  // 입력은 늘 orig4(2026-10-10 원본 스냅숏) — 역할 계수 · 배율이 거듭 곱해지지 않게(몇 번 돌려도 같다)
  const p = `${dir}/heroes/${k}.json`; const j = JSON.parse(fs.readFileSync(new URL(`./orig4/${k}.json`, import.meta.url), 'utf8'));
  if (STRUCT[k]) STRUCT[k](j); addBasics(j); applyDiverse(j); applyFeedback(j, k); mergePassives(j); dealerRole(j); if (FIX[k]) FIX[k](j); if (TUNE[k]) tune(j, TUNE[k]); roleCoef(j); dedupe(j); extraDraw(j);
  fs.writeFileSync(p + '.tmp', JSON.stringify(j, null, 2) + '\n'); fs.renameSync(p + '.tmp', p); console.log('손질', k);
}
