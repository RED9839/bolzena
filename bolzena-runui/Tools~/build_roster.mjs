// 웹판(C:\projects\볼제나, 읽기 전용)의 사도 135명 표를 화면용 JSON 으로 뽑는다 — 도감 · 편성 그리드에 쓰는 겉모습만(이름 · 성격 · 종족 · 줄 · 역할 · 별 · 그림 이름).
// 규칙(카드 · 수치)은 코어 데이터에서 온다. 결과는 시험 프로젝트 Resources(원작 설정이라 git 에 넣지 않는다).
//   node Tools~/build_roster.mjs [출력 경로]
import fs from "node:fs";
const WEB = "file:///C:/projects/볼제나/js/";
const out = process.argv[2] || "C:/projects/bolzena-runui-test/Assets/Resources/RunUI/roster.json";
const { HERO_DATA } = await import(WEB + "cardbook.js");
const ART = (await import(WEB + "data/artmap.js")).default.art;
const heroes = Object.entries(HERO_DATA).map(([key, h]) => ({
  key, ko: h.ko || key, nature: h.nature, race: h.race, row: h.row, role: h.role, star: h.star || 3,
  hp: h.hp, atk: h.atk, def: h.def, crit: h.crit,
  blurb: h.blurb || "", keyword: (h.keyword && h.keyword.ko) || "", ult: (h.ult && h.ult.ko) || "",
  art: ART[key] || null,
}));
fs.mkdirSync(out.replace(/[\/][^\/]+$/, ""), { recursive: true });
fs.writeFileSync(out, JSON.stringify({ heroes }, null, 0));
console.log("roster", heroes.length, "→", out, "그림 없음:", heroes.filter((h) => !h.art).map((h) => h.key).join(","));
