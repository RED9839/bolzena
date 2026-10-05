// 이벤트 손질 — 옛 복사본에서 새 키워드 · 정신 붕괴 · 카드 얻기/제거/복제(카제나식 구조 그대로, 셋 중 고르기 없음)로
const fs = require("fs"), path = require("path");
const SRC = path.join(__dirname, "orig_events"), DST = "C:/projects/bolzena-content-v2/world/events";
const ev = {};
const files = fs.readdirSync(SRC).filter(f => f.endsWith(".json"));
const data = {};
for (const f of files) { data[f] = JSON.parse(fs.readFileSync(path.join(SRC, f), "utf8")); for (const e of data[f].events) ev[e.id] = e; }
const opt = (id, i) => ev[id].options[i];
const mb = (n = 1) => ({ k: "mindBreak", n });
const nextOf = (o) => { let x = o.find(z => z.k === "next"); if (!x) { x = { k: "next", next: {} }; o.push(x); } return x.next; };

// ── 공용 ──
// G02 「석 장 다 보고 하나 고른다」(셋 중 고르기) → 복채를 더 내고 한 장 더 — 무작위 그대로
Object.assign(opt("G02", 1), { label: "복채를 더 얹고 한 장 더 뒤집는다", say: "앨리스가 복채부터 받는다. 해석은 덤이다.", gamble: [{ p: 0.5, out: [{ k: "dupe", n: 1 }], say: "\"버릴 패야.\" 반대였다 — 패가 둘로 늘었다." }, { p: 0.5, out: [{ k: "remove", n: 1 }], say: "\"늘어날 패야.\" 반대였다 — 하나가 사라졌다." }] });
delete opt("G02", 1).choose;
// G03 벤치에서 존다 — 초재생으로 회복이 이어진다
nextOf(opt("G03", 1).out).buff = { "초재생": 1 };
// G04 기초 체력 — 불굴 · 기본기 반복을 그만둔다 — 시작 카드만 제거
nextOf(opt("G04", 1).out).buff = { "불굴": 1 };
opt("G04", 2).out = [{ k: "remove", n: 1, basic: true }, { k: "hp", v: -0.08 }];
opt("G04", 2).say = "기본기 카드 한 장을 덱에서 내려놓는다. 손이 허전하다.";
// G05 노래를 따라 부른다 — 계몽(고학년을 쓰면 드로우)
nextOf(opt("G05", 1).out).buff = { "계몽": 1 };
// G08 분신을 둘 — 정신 붕괴
opt("G08", 1).out = [{ k: "hp", v: -0.1 }, { k: "dupe", n: 2 }, mb(1)];
opt("G08", 1).say = "분신이 셋, 넷… 누가 진짜인지 헷갈린다. 한동안 정신이 없다.";
// G12 들것 — 약화 대신 회피
// B2 심부름 실패 — 정신 붕괴
opt("B2", 1).gamble[1].out = [{ k: "curse", id: "curse_hex" }, mb(1)];
opt("B2", 1).gamble[1].say = "심부름 끝에 받은 건 저주문 자수와 잔소리 폭탄이었다.";

// ── 모나티엄 ──
// M1 드론 뜯기 실패 — 충격
opt("M1", 1).gamble[1].out = [{ k: "hp", v: -0.12 }, { k: "next", next: { buff: { "충격": 2 } } }];
opt("M1", 1).gamble[1].say = "남은 전기가 손끝에 찌릿하게 남았다.";
// M2 발명품 시험 실패 — 정신 붕괴
opt("M2", 1).gamble[1].out = [{ k: "hp", v: -0.12 }, mb(1)];
opt("M2", 1).gamble[1].say = "머리가 띵하다. 시제품이 뭘 했는지 아무도 모른다.";
// M4 부서를 고른다(셋 중 고르기) → 서류가 배정되는 대로
Object.assign(opt("M4", 0), { label: "서류를 내고 배정을 기다린다", say: "번호표가 불린다. 어느 부서로 갈지는 창구 마음이다." });
delete opt("M4", 0).choose;
// M6 방패 대열 — 결의 + 반격
nextOf(opt("M6", 0).judge.pass).buff = { "결의": 2, "반격": 2 };

// ── 수인 마을 ──
// F4 쓴 약 부작용 — 포자증식
opt("F4", 1).gamble[1].out = [{ k: "hp", v: -0.12 }, { k: "next", next: { buff: { "포자증식": 3 } } }];
opt("F4", 1).gamble[1].say = "속이 울렁거린다. 몸이 물러진 느낌이다.";
// F6 짐을 내려놓는다 — 시작 카드만
opt("F6", 1).out = [{ k: "remove", n: 1, basic: true }, { k: "next", next: { ap: -1 } }];
// F7 개업 기념 행사 — 그대로

// ── 에르피엔 ──
// E5 근위대와 힘겨루기 — 불굴 + 반격
nextOf(opt("E5", 0).judge.pass).buff = { "불굴": 1, "반격": 2 };
// E6 배급소 창고 — 곰팡이 핀 빵
opt("E6", 1).gamble[1].out = [{ k: "hp", v: -0.1 }, { k: "curse", id: "curse_spore" }];
opt("E6", 1).gamble[1].say = "창고 구석에서 집어 온 건 곰팡이 핀 빵이었다.";
// E7 거울 속 카드 — 정신 붕괴
opt("E7", 0).out = [{ k: "dupe", n: 1 }, mb(1)];
opt("E7", 0).say = "거울 속 카드가 손에 들린다. 거울 속 내가 웃은 것 같다 — 한동안 멍하다.";

// ── 용족 터 ──
// D2 운동 교본 — 불굴 + 결의
nextOf(opt("D2", 1).out).buff = { "불굴": 1, "결의": 1 };
// D6 동석에게 길을 묻는다 — 그대로

// ── 유령 늪 ──
// H2 같이 잔다(코미) — 회피
opt("H2", 2).out = [{ k: "hp", v: 0.3 }, { k: "shinPick", n: 1 }, { k: "next", next: { buff: { "회피": 1 } } }];
// H3 야유 실패 — 정신 붕괴
opt("H3", 1).gamble[1].out = [{ k: "curse", id: "curse_haunt" }, mb(1)];
opt("H3", 1).gamble[1].say = "개그맨 유령이 따라붙었다. 귓가에서 밤새 개그를 한다.";
// H5 틈 — 실패에 정신 붕괴
opt("H5", 0).gamble[2].out = [{ k: "hp", v: -0.15 }, { k: "curse", id: "curse_haunt" }, mb(1)];

// ── 정령산 ──
// S1 불길을 막아선다 — 실패에 고통
opt("S1", 0).judge.fail = [{ k: "hp", v: -0.15 }, { k: "next", next: { buff: { "고통": 3 } } }];
// S5 모닥불 — 결정화 + 실드 보존
nextOf(opt("S5", 1).out).buff = { "결정화": 2, "실드 보존": 1 };
// S6 라디오를 부순다 — 다음 턴 드로우
nextOf(opt("S6", 0).out).buff = { "다음 턴 드로우": 2 };

for (const f of files) fs.writeFileSync(path.join(DST, f), JSON.stringify(data[f], null, 2) + "\n");
console.log("이벤트", Object.keys(ev).length);
