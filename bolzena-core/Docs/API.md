# com.bolzena.core — 화면이 부르는 API (v2, 2026-10-05 — 키워드 사전)

네임스페이스 `Bolzena.Core`. 순수 C#(UnityEngine 참조 없음) · JSON 은 Newtonsoft.
**바꾸면 이 문서를 고친다** — 맨 아래 「바뀐 것」 에 적는다. 데이터 틀은 [데이터.md](데이터.md).

규칙: 엔진 함수는 실패를 예외로 던지지 않고 **까닭 글(string)** 또는 `PlayResult.Why` 로 돌려준다(null = 됐다). 화면은 그 글을 그대로 보여 주면 된다.

---

## 0. 데이터

```csharp
GameData data = GameData.FromFolder(string dir);              // heroes · cards · enemies · villages · events · equips .json
GameData data = new GameData().Add(heroesJson, cardsJson, enemiesJson, villagesJson, eventsJson, equipsJson); // 없는 것은 null
Validator v = Validator.Check(data);                           // v.Ok · v.Errors · v.Warnings
CardText text = new CardText(data);                            // 데이터 → 한국어 글

data.Hero(id) → HeroDef        data.Card(id) → CardDef        data.Enemy(id) → EnemyDef
data.Equip(id) → EquipDef      data.Event(id) → EventDef      data.Villages[id] → VillageDef
data.View(cardId, flash = 0) → CardView      // 신탁을 얹은 카드의 모습(코스트 · 종류 · 태그 · 효과 · Target)
data.UniquesOf(heroId) → List<string>        data.BuildDeck(party) → List<string>
```

### 글 생성기(`CardText`)

```csharp
string Card(CardView c)            // 카드 면 — 「연계. 적 1명에게 공격력 80% 피해. 파괴: AP +1」
string Card(CardDef c)
string Oracle(CardDef c, OracleDef o)   // 신탁 고르기 창의 한 줄(코스트가 바뀌면 「코스트 N.」)
string Bless(BlessDef b)
string Passives(List<PassiveRule> r)    // 「맞불: 피해를 받으면 반격 1 (턴당 1회) · …」
string PowerText(Fx power)              // 강화 지속 규칙 — 「이 전투 동안 매 턴 시작 시 아군 전원에게 「물총」 +1」(카드 면도 이 글을 쓴다)
string PowerRules(List<PassiveRule> r)  // 같은 글에서 「이 전투 동안」 머리만 뺀 것(강화 칩 툴팁)
string Keyword(KeywordDef k)
string Ult(UltDef u)
string Hero(HeroDef h)             // 도감 한 장(여러 줄)
string Intent(Intent it, int? shown = null)   // 적 머리 위 · 정보 창. shown = battle.IntentHit(e)
string Outcomes(List<Outcome> outs) / Outcome(Outcome o)   // 이벤트 결과
string Equip(EquipDef e)           static string StatsText(Stats s)
string Enemy(EnemyDef e)           // 적 정보 창 덧붙임(쌓이는 수치 · 희귀종 · 영혼 공유 · 패시브, 여러 줄)
string Counter(CounterDef c)       // 「분노: 1개당 주는 피해 +10%. …」
// 칩 · 툴팁
static IReadOnlyDictionary<string,string> CardText.TIPS        // 엔진 키워드 → 짧은 설명(키워드.md 뜻을 우리 말로)
static string CardText.Tip(string keyword)                     // 「소멸 2」 도 됨(수는 떼고 찾는다), 없으면 null
Dictionary<string,string> Tips()                               // TIPS + 데이터의 사도 고유 효과(이름 → 「X의 고유 효과 — …」) + 적의 쌓이는 수치
List<string> Chips(CardView c)                                 // 그 카드 글에 나오는 키워드 이름(칩 차례) — Tips() 로 설명을 찾는다
```

카드 모습 `CardView.Choices`(두 갈래 이름 둘 — 있으면 화면이 낼 때 고르게 하고 `PlayOpts.Choice` 1 · 2 로).

카드 id 꼬리: `xxx~` = 전투 중 만든 맨 카드(신탁 · 축복 없음), `xxx^` = 복제본(그림을 뒤집어 보인다). `GameData.BaseId(id)` 로 원래 id(그림 찾기).

---

## 1. 전투 — `Battle`

### 열기

```csharp
// 판에서(보통): 
(Battle b, RewardState loot) = run.OpenFight(double hpx = 1, double dmgx = 1, List<Cue> cues = null, Action<Cue> onCue = null);
// 손으로(시험 · 데모):
Battle b = Battle.Start(GameData data, BattleSetup setup, List<Cue> cues = null, Action<Cue> onCue = null);

class BattleSetup {
  List<string> Party /* 편성 순서 — 맨 앞이 맞는 모습 */; List<string> Deck; List<string> Enemies;
  int? PartyHp, PartyMaxHp; long Seed; bool NoNature;
  Dictionary<string,Stats> Gear; Dictionary<string,List<PassiveRule>> GearRules;
  Dictionary<string,int> Flash; Dictionary<string,string> Shin; Dictionary<string,Glow> Glow;
  double? EnemyHp, EnemyDmg; NextFight Next; int Gauge; bool Elite;
}
```

`Start` 이 끝나면 이미 1턴 시작이다(손 · AP · 적의 수 · 개막 카드까지 돌았다). 그동안 생긴 쪽지가 `cues` 에 쌓여 있다.

### 하기

```csharp
string     b.CanPlay(string cardId, bool free = false, int? handIdx = null)   // null = 낼 수 있다
int        b.CostOf(string cardId, int? handIdx = null)                         // 지금 코스트(공짜 · 주도 · 축복 · 다음 카드 코스트 반영)
CardView   b.CardOf(string cardId)          // .Target: "적"(적 하나를 고르게) · "아군" · "없음"
int        b.DiscardChoice(int handIdx)     // > 0 이면 버릴 카드를 그 장수만큼 골라 PlayOpts.Discard 로
PlayResult b.PlayCard(int handIdx, int targetIdx, PlayOpts opts = null)
           // targetIdx = 적의 Idx(아군 카드면 사도 Idx). → { Ok, Why, Finale }. Finale 이면 화면이 턴을 넘긴다
class PlayOpts { List<string> Discard; int? Ally; int? Choice; }   // Choice — 두 갈래 카드(CardView.Choices)에서 고른 갈래 1 · 2

Glow       b.GlowOf(string cardId)          // 빛나는 카드면 not null — 내기 전에 고른다
string     b.ApplyEpiphany(string cardId, int choice)   // 고른 뒤 그 카드를 PlayCard. 돌려줌 "card" · "hero"
  // Glow { Kind: "card" → Picks[i] { N(신탁 번호), Shin } · "hero"(은총) → Options[i](고유 카드 id), Hero }
  // 고르는 창: Kind == "card" → text.Oracle(data.Card(id), data.Card(id).Oracles[N-1]) (+ Shin 이면 축복)

string     b.CanUlt(string heroKey)         // null = 쓸 수 있다(게이지)
UltDef     b.UltOf(string heroKey)
PlayResult b.UseUlt(string heroKey, int targetIdx = 0)

Battle     b.EndTurn()                      // 턴 끝 · 적의 차례 · 다음 턴 시작까지 한 번에(쪽지가 차례대로 쌓인다)
```

### 미리보기(판을 복사해 실제로 내 본다 — 원본은 안 바뀐다)

```csharp
List<PreviewFoe> b.PreviewCard(int handIdx, int targetIdx)  // 적 Idx 마다 { Hp, Guard, Kill, Max(무작위 — 최대), Tough, Brk } 또는 null
List<PreviewFoe> b.PreviewUlt(string heroKey, int targetIdx)
PreviewParty     b.PreviewPartyOf(int handIdx, int targetIdx) // { Heal, Block, Shield, Lose, Over } 또는 null
int              b.HitAmount(Unit u, double ratio, ...)       // 카드 면 숫자(지금 스탯으로)
```

### 상태 읽기(스냅샷)

```csharp
string b.Over           // null · "win" · "lose"
int b.Turn, b.Ap, b.Gauge, b.ApJam(다음 턴 깎일 AP), b.NextCheaper
Unit b.Pool             // 파티 한 몸 — Hp · MaxHp · Block · Shield · Status(파티 층)
List<Unit> b.Party      // 사도 — Key · Name · Idx · Row · Role · Nature · Atk · Def · Crit · Mods(증감) · Status(사도 층: 사기)
                        //        Hp/Block/Shield/Dead 는 Pool 을 가리킨다
List<Unit> b.Enemies    // 적 — Key · Name · Idx · Row · Hp · MaxHp · Block · Shield · Status · Mods · Dead · Boss
                        //      Intent(수) · Tough · ToughMax(소수 — 1/3 · 1/6 눈금) · Broken · Sealed(다음 차례 못 움직임) · RushCnt · Phased
List<string> b.Hand, b.Draw, b.Discard, b.Gone          // 카드 id(Draw 의 끝이 다음에 뽑힐 카드)
int    b.St(Unit u, string id)          // 상태 겹(사기는 사도마다, 나머지는 파티 몸)
int    b.StackOf(string heroKey, string kw)   // 자기 주머니 키워드 겹. 표식(적 · 아군)은 b.St(holder, kw)
Dictionary<string,KwRt> b.Kw            // 이 전투의 키워드 — kw.Def(KeywordDef) · kw.Owner
double b.StatMod(Unit u, string stat)   // "atk" · "def" · "crit" · "dealt" · "taken" 증감 합
int    b.AtkNow(Unit u), b.DefNow(Unit u)
int?   b.IntentHit(Unit e)              // 머리 위 피해 숫자(층 배율 · 약화 · 사기 반영). 치는 수가 아니면 null. 보스 고학년(T "ult")이면 안의 치는 수 합
int?   b.UltHit(Unit e)                 // 보스 클론 고학년 피해 — 예고 턴(charge → Next "ult")에도 다음 턴 피해를 준다. 고학년이 아니면 null
int?   b.ChargeHit(Unit e)              // 힘 모으기(charge) 중인 적의 다음 턴 피해(합 — 층 배율 · 약화 · 사기) — 엘리트 · 일반의 힘 모으기와 고학년 예고 모두. 예고 칸을 크게 보일 때. 예고가 아니면 null(2026-10-07)
int    b.RushOf(Unit e)                 // 즉시 행동까지 장수(0 = 안 당겨짐). 남은 장수 = RushOf(e) - e.RushCnt
int?   b.ActionCount(Unit e)            // 행동 카운트 — 이 적이 행동하기까지 남은 카드 수(둔화 · 급속 반영). 당겨지지 않는 수 · 이미 행동했으면 null
List<StatusView> b.StatusViews(Unit u)  // 상태 칩 + 건 쪽 — 적이면 그 적, b.Pool 이면 파티 층, 사도면 개인 층(구속) + 그 사도 고유 효과
   // StatusView   { string Id; int Stacks; string Layer("party"·"hero"·"enemy"); bool Keyword(사도 고유 효과); bool Counter(적의 쌓이는 수치); List<StatusSource> Sources }
   // StatusSource { string Kind("hero"·"gear"·"enemy"·"neutral"·"event"); string Hero(사도 키 — hero · gear); int? Enemy(적 Idx — enemy); int Stacks }
   // Sources 의 합은 Stacks 를 넘지 않는다(겹이 줄면 먼저 건 쪽부터 덜어 보인다). 적의 쌓이는 수치는 Sources 가 비어 있다
Dictionary<string,int> b.PartyStatus()  // 파티 층 상태(사기 · 불굴 · 취약 … 파티원 전원에게 든다)
Dictionary<string,int> b.HeroStatus(Unit hero)   // 개인 층(구속) + 그 사도 고유 효과 겹(자기 주머니 · 사도 표시)
int    b.CardStOf(string cardId, string st)      // 카드에 붙은 상태(독 · 봉쇄 · 침체 · 빙결 · 탐구심 · 비용 · 카드 값)
int    b.BondOf(string cardId)          // 결속 겹친 수(1~5) — 3 이상이면 b.CardOf 가 강해진 카드 모습
bool   b.IsFrozen(string cardId)        // 빙결 — 이번 턴 낼 수 없다(b.CanPlay 도 까닭을 준다)
List<string> b.Removed                  // 「제거」 로 덱에서 뺄 카드(run.AfterFight 가 뺀다)
Dictionary<string,Stats> b.GrowthGain   // 이 싸움의 판 단위 성장(run.AfterFight 가 RunState.Growth 에)
List<string> b.WeakOf(Unit e)           // 약점 성격(판의 적 속성이 있으면 그것을 이기는 성격 하나)
bool   b.IsWeakHit(Unit from, Unit to, HashSet<string> tags)
bool   b.WeakFor(Unit hero, Unit e)     // 그 사도가 치면 약점인가(성격 · 공명은 늘 · 적 표식) — 약점 표시용
string b.EnemyNature                    // 판의 적 속성(null = 적마다 데이터 성격)
ToughView b.ToughViewOf(Unit e)         // 강인도 화면 값 { double Left, Max; int Pips(=Max 올림); bool None(강인도 없음 = Max 0 → 막대를 숨긴다), Broken(격파 상태), Resting(다음 차례 쉼); string Nature; List<string> Weak; double Fill(i)(i 번째 칸 0~1) }
   // 강인도 없음 = 보스가 noTough 수로 세운 소환물(격파 안 됨). 그 밖의 적은 최대 3 이상. 사도 클론의 Nature 는 판의 적 속성이 아니라 그 사도 성격(EnemyDef.Clone).
   // 단위 = 약점 공격 비용 1. 약점 아니면 1/3 · 0코 1/6 이 깎이니 칸을 정수로 반올림하지 말고 Fill(i) 로 칸마다 채워 그린다.
   // 미리보기 PreviewFoe.Tough 도 소수(1/3 = 0.333…) — 「◆-1/3」 처럼 소수로 보인다. tough 쪽지 From · To 도 소수.
int    b.ToughHits, b.ToughWeakHits, b.Breaks; double b.ToughDealt   // 이 싸움 통계(시뮬 · 시험 — 저장 안 함)
List<string> b.Log                      // 기록(한국어 한 줄씩)
List<(string hero,string type)> b.PlayLog   // 이번 턴 낸 카드(잇기 · 앞이 … 표시)
int    b.SeqStep(When w, string heroKey)    // 「차례로 내면」 패시브 진행(칩)
Dictionary<string,List<RuleRt>> b.Passives  // 사도마다 걸린 규칙(패시브 · 키워드 규칙 · 장비)
```

### 변신(form) — 모습 + 능력

고학년 등의 효과 `form` 으로 사도가 **변신 상태**가 된다(개인 층 — 그 사도에게만). 데이터 틀은 [데이터.md](데이터.md) §16.

```csharp
bool     b.InForm(string heroKey)          // 변신 중인가
FormView b.FormOf(string heroKey)          // 아니면 null — { Hero, Id, Name, Skin, Anim, Left(남은 내 턴, 0 = 전투 끝까지), UntilFightEnd }
string   b.FormSkin(string heroKey)        // 모습 키(스파인 스킨) — 변신 중이 아니면 null(기본 모습). 애니 접두어는 FormOf(..).Anim
FormDef  b.FormDefOf(string heroKey)       // 지금 든 변신의 정의(글: text.Form(def) · text.Short(def))
Dictionary<string,FormRt> b.Forms          // 사도 키 → { Id, Left, Count } (저장된다)
void     b.EnterForm(Unit hero, string formId) · b.EndForm(Unit hero, string why)   // 시험 · 연출 도구용(보통은 효과가 부른다)
```

- **모습** — 전투 화면은 쪽지 `formOn`(Label = 모습 키 Skin · T = 애니 접두어 Anim)을 받으면 그 사도 스파인을 그 스킨으로, `formOff` 면 기본으로. 이어하기(`Battle.Load`) 뒤에는 사도마다 `b.FormSkin(key)` 로 맞춘다.
- **카드** — 손 · 더미의 카드 id 는 그대로다. `b.CardOf(id)` 가 변신판 모습(이름 · 코스트 · 글 · 효과)을 주니 카드는 늘 `b.CardOf` 로 그린다. 변신판 그림은 `b.CardOf(id).Def.Id`(바꾼 카드의 id — `GameData.BaseId(id)` 는 원래 카드). 변신이 들거나 풀릴 때 손의 카드마다 쪽지 `card`(hand → hand, Label `"form"`) — 그 카드를 다시 그린다.
- **칩** — `b.StatusViews(사도)` 에 `StatusView.Form == true` 칩(Id = 변신 이름 · Stacks = 남은 턴, 0 = 전투 끝까지). 설명은 `text.Tips()[이름]`.
- 같은 변신을 다시 쓰면 지속이 처음으로(`formOn` 이 다시 온다, `formOff` 없음). 다른 변신이면 `formOff`(Label `switch`) → `formOn`.

### 강화 카드 지속 규칙(power) — 강화 칩

강화 카드 효과 `power`(데이터 틀은 [데이터.md](데이터.md) §17)를 내면 그 규칙이 **이 전투 끝까지** 카드 주인의 패시브처럼 켜져 있다.
UI 는 아직 붙이지 않았다 — 붙일 때 아래만 읽으면 된다.

```csharp
List<PowerView> b.PowersOf(string heroKey = null)   // 켜진 강화(켠 차례) — { Hero(주인 사도 키), Id, Name(카드 이름), Card(카드 id — 그림), Stacks(겹), Text(규칙 글, 「매 턴 시작 시 …」) }
int             b.PowerStacks(string heroKey, string id)   // 그 강화 겹(없으면 0)
List<PowerRt>   b.Powers                            // 저장되는 상태 — { Hero, Id, Name, Card, N(겹), Rules }
```

- **칩** — `b.StatusViews(b.Pool)`(파티 버프 목록) 끝에 강화마다 `StatusView.Power == true` 칩: Id = 카드 이름 · Stacks = 겹 · Layer `"party"` · Sources[0].Hero = 주인 사도. 툴팁은 `b.PowersOf()` 의 `Text`(또는 `text.PowerRules(pw.Rules)`).
- **쪽지** — 켜질 때 · 겹이 늘 때 `powerOn`(Hero · Id · Name · CardId · V = 겹). 화면은 이 쪽지로 칩을 띄우거나 겹 숫자를 올린다. 규칙이 발동하면 평소 쪽지(status · shield · hurt …)가 오고, 기록 줄은 「사도 · 강화 「이름」」.
- **이어하기** — `Battle.Load` 가 `Powers` 를 되살린다. 칩은 `b.StatusViews(b.Pool)` 로 다시 그린다.
- **겹치기** — 같은 사도가 같은 id(없으면 카드 이름)를 또 켜면 겹 +1(칩은 하나). 발동할 때 효과가 겹 수만큼 돌고, 횟수 제한은 발동 수로 센다.

### 이벤트(연출 쪽지) — `Cue`

`Battle.Start(..., cues, onCue)` 또는 `b.OnCue += c => …`(둘 다 됨). `b.OnLog += line => …`(기록 한 줄).
쪽지는 **일어난 차례대로** 쌓인다 — 화면은 앞에서부터 꺼내 연출하면 된다(판은 이미 끝난 상태다).
`c.Side`(Party · Enemy) · `c.Idx`(그 몸의 Idx — 파티 몸의 일은 연출 자리 사도의 Idx, 드물게 -1 이면 파티 전체) 는 누구에게 일어났는지. `card` 쪽지는 Idx -1.

| K | 뜻 | 쓰는 칸 |
|---|---|---|
| `turn` | 내 턴 시작 | V = 턴 |
| `foeTurn` | 적의 차례 시작 | V = 턴 |
| `act` | 움직임 — 카드를 냄(사도) · 적의 수 · 고학년 | Anim("attack" · "skill" · "ult") · CardId · Name · Say(적의 말) · T(수 종류) · **Rush**(즉시 행동이면 true) |
| `hurt` | 맞음 | V(HP 로 들어간 피해) · Guard(방어 · 실드가 받은 몫) · **Crit** · From → To(HP) |
| `heal` | 회복 | V · From → To · Over(넘친 몫) |
| `block` · `shield` | 방어 · 실드 얻음 | V |
| `status` | 상태 걸림 · 꼬리표 | Id(「취약」 · 「반격!」 · 「AP +1」 · 「공격력 +10%」 · 「「마탄」 +1」 …) · Up(좋은 일) |
| `unguard` | **방어 · 실드를 잃음**(맞아서 깎인 것 말고) — 적의 차례 시작에 적 방어가 사라짐 · 「실드 전부 파괴」 | V(잃은 양) · To(남은 방어+실드) |
| `tough` | 강인도 바뀜 | From → To · Up(되찾음) |
| `break` | **격파** | V(얻은 AP) |
| `die` | 적 쓰러짐 | |
| `auto` | 손에서 저절로 나감(연계 · 천상 · 개막 · 연쇄) | Tag · Label(「연계!」) · Name · CardId · Hero · Target |
| `epiphany` | 신탁 · 은총 고름 | CardId · Id("card" · "hero") · V(고른 번호) · Hero |
| `card` | **카드 이동** | CardId(null 이면 섞기) · Pile → ToPile(draw · hand · discard · gone · play · new) · Label(burn · grace · connect · recall · curtain · foe · make · discard · evaporate · turnEnd · shuffle · **forget**(망각) · **remove**(제거) · **bond**(결속으로 겹침) · **evolve**(진화) · **transform** · **pull**) |
| `talk` | 사도 대사 때 | Hero · Moment(start · hit · kill · win · down · ego) — 줄은 화면이 고른다 |
| `over` | 전투 끝 | Id("win" · "lose") |
| `summon` | 적이 새로 섰다(적의 수 summon) | Name — `b.Enemies[c.Idx]` 가 그 적 |
| `revive` | 쓰러진 적이 되살아났다(재 속 · 가사) | V(HP) · Name |
| `formOn` | **사도가 변신했다**(같은 변신이면 지속 갱신) | Hero · Id(변신 id) · Name · **Label(모습 키 Skin)** · T(애니 접두어 Anim) · V(턴, 0 = 전투 끝까지) |
| `formOff` | **변신이 풀렸다** | Hero · Id · Name · Label(까닭 — time 지속이 다함 · until 풀리는 계기 · card 효과 formEnd · switch 다른 변신) |
| `powerOn` | **강화가 켜졌다**(같은 강화를 또 내면 겹이 늘었다) | Hero · Id(강화 id) · Name(카드 이름) · CardId(카드 — 그림) · V(겹) |
| `foeUltWarn` | **보스 클론이 고학년을 예고**(내 턴 시작 — 그 적의 수가 charge, Next 가 ult) | Hero(사도 키) · Name(고학년 이름) · V(다음 턴 피해 — `b.UltHit`) · T(치는 꼴 attackAll · attack · multi) |
| `foeChargeWarn` | **엘리트 · 일반 적이 힘을 모은다**(그 적의 수가 charge, Next 가 ult 가 아닌 것 — 수를 세울 때) | Name(쏟을 수 이름) · Say(모으기 이름) · V(다음 턴 피해 — `b.ChargeHit`, 치지 않는 수면 0) · T(쏟을 수 꼴 attack · back · multi · attackAll) |
| `foeUlt` | **보스 고학년 시작**(적의 차례 — 바로 뒤에 `act` T "ult") | Hero · Name · V(피해 합) |
| `foeUltHit` | 보스 고학년의 치는 수 하나(바로 뒤에 그 수의 `hurt` · `status` …) | Hero · Name · V(몇 번째, 0부터) · T(attackAll · attack · multi) |
| `foeUltEnd` | 보스 고학년 끝 | Hero · Name |
| `foeUltCut` | **보스 고학년이 끊김**(예고 · 사용 턴에 격파 — 그 자리, 기절 — 적의 차례에 쉴 때) | Hero · Name · Label("격파" · "기절") · T(charge = 예고 중 · ult = 사용 턴) |

카드를 내면: `card`(hand → play) → `act` → (효과의 hurt · tough · break · status …) → `card`(play → discard/gone/hand).
즉시 행동은 `act` 의 `Rush == true`. 고학년은 `act` 의 `Anim == "ult"`.
보스 클론의 고학년(`BossUlt` — 데이터.md §18): `foeUlt` → `act`(Side Enemy · **T "ult"** · Anim 은 치는 수가 있으면 "attack" — 옛 화면도 그대로 돈다 · Hero · Name) → (`foeUltHit` → `hurt` · `status` …) 반복 → `foeUltEnd`.

### 저장

```csharp
string json = b.Save();                         // 내 턴, 카드를 기다리는 자리에서
Battle b    = Battle.Load(GameData data, string json, List<Cue> cues = null);
```

---

## 2. 판 — `Run`

```csharp
string Run.RollVillage(GameData d, double r01)   // 모험 시작 — 마을 하나(파티를 고르기 전에 보인다)
string Run.RollNature(GameData d, string village, double r01)   // 모험 시작 — 그 마을에서 고를 수 있는 적 속성 하나(마을과 함께 보인다). 옛 RollNature(r01) 은 마을을 모를 때만
List<string> Run.NaturesFor(GameData d, string village)         // 그 마을에서 고를 수 있는 속성 — 클론 보스 자리 수만큼 그 종족 · 그 성격 사도가 있는 것(공명 빼고)
string Run.NatureBySeed(GameData d, string village, long seed)  // 속성을 안 준 판은 씨앗으로(고를 수 있는 것 가운데)
List<List<string>> Run.PickBosses(GameData d, string village, string nature, long seed)   // 층마다 보스 줄 — 클론 자리를 그 성격 사도의 클론으로
List<(int floor,int idx,string id)> Run.CloneSlots(d, village) · List<string> Run.CloneCandidates(d, village, nature, int floor = -1)   // floor 를 주면 그 층 성급만
bool Run.StarFits(int floor, int star)   // 보스 성급 — 1층(floor 0) 1~2성 · 2층 3성. 그 층 성급 사도가 없으면 그 층만 다른 성급(PickBosses)
Run    Run.New(GameData d, List<string> party, long seed, string village = null, string enemyNature = null)   // party 순서 = 편성 순서(자리 1 · 2 · 3 — 맨 앞이 적의 공격에 맞는 모습, 관통은 맨 뒤). 사도 열(옛 rows)은 없다
string run.EnemyNature   // 판의 적 속성 = run.S.EnemyNature(저장 · 이어하기에 남는다) — 이 판 모든 적의 성격(두 보스 클론도 그 성격 사도). 약점 성격 = R.WeakTo(run.EnemyNature). null = 옛 저장
List<List<string>> run.Bosses   // 층마다 보스 줄(적 id) = run.S.Bosses(새 판에 정함 · 저장에 남는다). 옛 저장이면 마을 데이터
List<string> run.BossHeroes     // 층마다 보스 클론의 사도 키 — 판 시작 화면(마을 · 속성과 함께)이 그 사도 그림 · 이름으로 미리 보인다
   // 빌린 몸 클론: 클론 데이터가 없는 사도는 id 「clone~사도키~몸」(GameData.CloneId) — data.Enemy(id) 가 만들어 준다(이름 「X (클론)」 · Nature · Clone = 사도, Art 는 비어 있다 → Clone 의 사도 그림을 쓴다)
   // 「클론인가」 는 id 꼴(clone_) 말고 data.Enemy(id).Clone != null 로 본다
string run.Save();   Run Run.Load(GameData d, string json)
RunState run.S       // 판의 모든 것(읽기 · 저장): Village · Party · PartyHp · PartyMaxHp · Deck · Flash · Shin · Gold · Gauge
                     //   Gear[사도][칸] · Bag(정할 장비) · Floor(0·1) · Node · Done("clear") · Map · Reward · Shop · Camp · Event · NextFight …
VillageDef run.VillageDef;  FloorDef run.CurrentFloor;  bool run.IsBoss;  bool run.IsLastFloor;  bool run.PartyWiped
bool run.MindBroken    // 정신 붕괴 — 카드 얻기 · 신탁 · 제거가 막혔다(그 함수들이 까닭 글을 준다). RunState.MindBreak · Growth · CardVals
CardView run.ViewOf(cardId)    // 덱 카드의 지금 모습(신탁 반영)
```

### 지도

```csharp
MapState run.MapOf()            // Rows[줄][칸] MapNode { Id, Row, Col, X(0~1), Type(start·fight·elite·camp·campshop·event·boss), Next[], Foes[] } · At · Seen
List<string> run.Reachable()    // 지금 고를 수 있는 칸 id
HashSet<string> run.AheadOf()   // 앞으로 닿는 칸(나머지는 흐리게)
MapNode run.EnterNode(string id)       // 못 가면 null
List<string> run.EnemiesAt(MapNode n)  // 칸에서 만날 적(미리 보기)
string run.StageName(MapNode n)        // 「1-5」
static Dictionary<string,string> Run.KIND_KO
```

칸 종류에 따라: fight · elite · boss → 싸움 / event → 이벤트 / camp · campshop → 캠프(+상점).

### 싸움 앞뒤 · 보상

```csharp
(Battle b, RewardState loot) run.OpenFight(...)    // 위 §1
void   run.AfterFight(Battle b)          // 끝나면 반드시 — HP · 게이지 · 얻은 카드/신탁을 판에
// 이겼으면:
void   run.TakeGold()                    // loot.Gold
string run.TakeEquip(string equipId)     // loot.Equip[0] — 받으면 run.S.Bag 에 선다(→ 장비 창)
string run.ClaimGlow(string cardId, Glow g, int choice)   // 빛났지만 안 낸 카드 — 끝난 뒤 하나 받기(b.Glow 에 남은 것)
// 보스였으면:
List<string> run.BossCopyOffer()         // 가진 고유 카드 셋
string run.BossCopy(string id)           // 고른 것의 복제본(id^)
void   run.Advance()                     // 층을 넘긴다 — run.S.Done == "clear" 면 완주
// 졌으면 run.PartyWiped — 판 끝
```

### 캠프

```csharp
CampState run.EnterCamp(string kind)     // { Key, Train: FlashOffer { CardId, Picks[] } (수련 선택지, 없으면 null) }
int    run.CampHealOf()                  // 쉬면 찰 HP
string run.CampRest()
string run.CampTrain(int n)              // Train.Picks 가운데 신탁 번호
```

### 상점(캠프+상점 칸)

```csharp
ShopState run.RollShop()                 // Items[i] { Id, Kind("neutral" · "equip"), Price, Sold, Delivery }
string run.Buy(int idx, string heroKey = null)   // 장비면 Bag 에(산 것은 팔 수 없다 — 껴야 한다). 교주 카드는 heroKey 사도 덱에(없으면 PendingNeutral 줄)
string run.PendingNeutral;  string run.AssignNeutral(string heroKey)   // 주인 고르기 — 상점 · 이벤트 · 보상 어디서 얻든
int    run.RerollPrice;  string run.RerollShop()
int    run.RemovePrice;  string run.RemoveCard(string cardId)   // 한 번 들를 때 한 번
```

### 장비

```csharp
// 얻은 장비는 run.S.Bag 에 선다 → 화면이 곧장 끼기 or 팔기를 묻는다
string run.Equip(string heroKey, string equipId, bool replace = false)  // replace — 낀 것은 팔린다
string run.SellEquip(string equipId)     // 산 것(run.IsBought)은 안 된다
int    run.SellPrice(string equipId)
Dictionary<string,string> run.GearOf(string heroKey)   // 칸 → 장비 id
Stats  run.StatsOf(string equipId, string heroKey)      // 애착 포함
```

### 이벤트

```csharp
EventState run.EnterEvent()              // { Choices[], Id(하나면 정해짐), Phase("choose"·"fight"·"result"), Log[], Pending[], Say }
string run.PickEvent(string id)          // 「지도 공개」 로 둘이면 하나를 고른다
List<EventOption> run.OptionsOf(EventDef ev)   // 보이는 선택지 + 맨 끝 「떠난다」(o.Leave)
List<Outcome> run.OutOf(EventOption o)   // text.Outcomes(...) 로 글
string run.LockOf(EventOption o)         // 못 고르는 까닭(골드 …) — null 이면 고를 수 있다
JudgeResult run.JudgeOf(EventOption o)   // 판정 미리 보기 { Pass, Who, Value, Need, Hp }
(bool fight, string why) run.Choose(int idx)
   // fight == true → run.OpenFight() 로 싸우고 → run.AfterFight(b) → run.AfterEventFight(b.Over == "win")
// 고르는 것: run.S.Event.Pending[0] { K: remove · dupe · card(Cards) · flash(Offer) · shinPick(Kind) · gambleChoice(Options) }
string run.ResolvePending(object value)  // 카드 id · 신탁 번호(int) · 고른 번호(int). null = 받지 않음
List<string> run.ShinAble(string kind);  bool run.DupeOk(string id)   // 고를 수 있는 카드 거르기
void   run.LeaveEvent()
```

### 판 기록(밸런스 수집)

```csharp
Dictionary<string,object> run.Record(string stage)
```

---

## 3. 봇 · 시뮬

```csharp
new Bots(data).SmartPlay(Battle b)                     // 이번 턴 카드를 다 낸다(EndTurn 은 안 한다)
new RunBot(data).RunFull(List<string> party, long seed, SimOpts o) → SimResult
MetaSim.Run(data, rounds, seed, hpx, dmgx, threads) → MetaSim.Result;  MetaSim.Report(data, result) → string
// 유니티: -executeMethod Bolzena.Core.EditorTools.SimCli.MetaSim | SimCli.Validate (README)
```

---

## 바뀐 것

- **v2.8(2026-10-07) — 평소 수는 무작위 · 예고 강화.** 덧붙이기만. `b.ChargeHit(e)` · 쪽지 `foeChargeWarn` · `Battle.CHARGE_GAP` · 고학년 끊김 뒤 보스 취약(`BossUlt.CUT_VULN`) · `BossUlt.DEBUFF_PLUS`. 고학년 기준 피해 1층 300 · 2층 360(옛 210 · 250).

- **v2.7(2026-10-06) — 강인도 회복 규칙.** API 꼴은 그대로(덧붙이기만: 적의 수 `brace` — 글 `text.Intent` 가 「강인도 회복 v」). **규칙이 바뀐 것**: 강인도는 저절로 차지 않는다 — 격파되면 다음 내 턴 시작에 가득(전과 같음), 그 밖엔 회복 스킬(수 `brace` · 수에 붙은 `tough` · 패시브 `do: brace`)이 있는 적만. 판(phase)이 바뀌어도 안 찬다(옛: 다 참). 수의 `tough` 는 그 적 자신만(옛: guard 면 적 전체). `R.TOUGH.Boss` 10 → 7. tough 쪽지 `Up` 은 회복 스킬 · 격파에서 일어섬에서만 나온다. 글: `TIPS["강인도"]` · `TIPS["격파"]` 고침, 수의 덧글 「강인도 +1」 → 「강인도 회복 1」.

- **v2.6(2026-10-06) — 보스 클론의 고학년.** 덧붙이기만. 보스 클론(`boss` + `clone`)이 그 사도 고학년을 예고(charge) → 다음 턴 사용(T "ult" · `Intent.Then`). `BossUlt`(변환 · 손보정 표 `FIX` · `On` · `Plan` · `Table`) · `b.UltHit(e)` · `b.IntentHit` 가 ult 를 센다 · `Unit.UltAt`(저장됨) · 쪽지 `foeUltWarn` · `foeUlt` · `foeUltHit` · `foeUltEnd` · `foeUltCut` · 글 `text.Intent` 가 「고학년 예고 — 다음 턴 고학년 「…」: …」. 콘솔 `bz bossult [--out]` · 모든 명령에 `--bossult 0`(끄기).

- **v2.5(2026-10-06) — 한 전투에 사도마다 신탁 · 은총 하나만.** API 꼴은 그대로, 값이 바뀐 것: `run.RollEpiphany()`(= `run.OpenFight` 의 `b.Glow`)가 같은 사도(교주 카드는 주인 사도)에게 은총과 카드 신탁을 같이 세우지 않는다. 꼭 떠야 하는 카드 신탁(엘리트 · `rewardFlash`)에 빈 사도가 없으면 은총 하나를 카드 신탁으로 바꾼다(엘리트는 은총이 둘 이상일 때만). `CardText.TIPS` 에 「신탁」 · 「은총」. 화면은 신탁 번호(`GlowPick.N` · `OracleOption.N`)를 보이지 않는다 — 번호는 고르는 값으로만 쓴다.

- **v2.4(2026-10-05 일곱째) — 강화 카드 지속 규칙(power).** 덧붙이기만. 효과 `power`(`Fx.Rules` — 패시브 규칙 배열) · `FxK.Power`.
  전투: `b.Powers`(+ `PowerRt`, 저장 `BattleSave.Powers`) · `b.PowersOf`(+ `PowerView`) · `b.PowerStacks` · `b.AddPower` · 쪽지 `powerOn` · `StatusView.Power`(파티 층 칩) · `RuleRt.Power`.
  글: `text.PowerText(fx)` · `text.PowerRules(rules)` · 강화 카드 면의 세기형 버프(사기 · 결의 · 결정화 · 불굴 · 고동 · 근면 · 계몽 · 집중)는 「이 전투 동안 …」 으로 읽힌다 · 사도 표시(hero) 고유 효과에 대상이 있으면 「아군 전원에게 「물총」 +1」.
  값어치: `CardValue.PowerValue(fx)` · `RuleWorth(rule)` · `RuleFires(rule)` · `POWER_T` · `POWER_W` — 봇 점수(`Bots.Score`)가 켜진 강화의 남은 몫을 센다. 검사: power 는 강화 카드(· 그 신탁)에만 · rules 필수 · fightStart 금지 · always 는 증감만 · 강화 카드에 남는 효과가 없으면 주의.

- **v2.3(2026-10-05 여섯째) — 변신(form).** 덧붙이기만. 효과 `form`(id) · `formEnd` · `HeroDef.Forms`(`FormDef` · `FormBonus`) · `GameData.Form(id)` · `GameData.HeroOfForm(id)`.
  전투: `b.InForm` · `b.FormOf`(+ `FormView`) · `b.FormSkin` · `b.FormDefOf` · `b.Forms`(+ `FormRt`, 저장 `BattleSave.Forms`) · `b.EnterForm` · `b.EndForm` · `b.FormKeyOf`(봇 캐시) · 쪽지 `formOn` · `formOff` · card 쪽지 Label `form` · `StatusView.Form` · `RuleRt.Own` · `RuleRt.Form`.
  글: `text.Form(f)` · `text.Short(f)` · `text.Detail(f)` · `Short(UltDef)` 는 변신을 자르지 않는다 · `Hero` · `HeroShort` · `Tips` · `Chips`. 값어치: `CardValue.FormValue(f, data, hero)` · `CardValue.FORM_VAL`.
  계기 둘: 효과 `form`(고학년 등 — 다시 쓰면 지속 갱신) · 고유 효과 `keyword.onMax { form, consume }`(`KwOnMax` — 최대에 닿으면, 이미 그 변신이면 아무 일 없음).
  `b.CardOf(id)` 가 변신 중인 사도의 카드를 변신판 모습으로 준다(같은 API, 변신이 없으면 그대로). 스키마(`Docs/schema`)에 `forms` · `form` · `formEnd`.

- **v2.2(2026-10-05 다섯째) — 교주 카드 주인 사도.** 덧붙이기만(기본값 인자라 옛 호출 그대로 됨).
  덱의 교주 카드는 `「카드id@사도키」`(꼬리는 뒤: `n_x@rico^`). 전투에서 그 카드는 **주인 사도의 카드**로 낸다 — 피해 = 주인 공격력, 실드 · 회복 = 주인 방어력, 연계 · 「다른 사도 카드」 · 사도별 묶음 · 구속도 주인 기준.
  `GameData.OWNER`("@") · `GameData.OwnerOf(id)` · `GameData.WithOwner(id, hero)` · `GameData.NoOwner(id)` · `GameData.BaseId` 는 주인도 뗀다(그림 찾기 그대로).
  `CardView.Owner`(교주 카드의 주인, 사도 카드는 null) · `CardView.Hero` = Owner ?? 사도 · `CardView.Neutral`. `data.View(id, flash, owner)` — id 에 주인이 없을 때 볼 사도(전투는 첫 사도).
  판: `string run.PendingNeutral`(주인 기다리는 교주 카드 id, 없으면 null) · `string run.AssignNeutral(heroKey)` · `string run.GainCard(id, heroKey = null)` · `run.Buy(idx, heroKey = null)` · `run.ResolvePending(value, heroKey = null)` · `RunState.NeutralWait`.
  heroKey 없이 얻으면(상점 · 이벤트 card · gift · 은총) 줄에 선다 — 화면은 `PendingNeutral` 이 있으면 사도를 고르게 하고 `AssignNeutral`. 옛 저장의 주인 없는 교주 카드는 불러올 때 첫 사도 것으로(신탁 · 축복 · 카드 값도 따라감).
  봇: `RunBot.PickOwner(run, cardId)` — 피해 카드는 공격력 1위, 실드 · 회복은 방어력 1위, 그 밖은 서포터(없으면 카드가 가장 적은 사도). 검사: 카드 id 에 `@` 금지.
  화면 목록(CardOrder)은 정의(`data.Card(id).Hero == null`)로 교주 묶음을 가르니 그대로 맨 끝 — 주인 표시는 `GameData.OwnerOf(id)` 또는 `run.ViewOf(id).Owner`.

- **v2.2(2026-10-05 다섯째) — 강인도 · 판의 적 속성**. 덧붙이기만: `Run.RollNature` · `Run.NatureBySeed` · `Run.New(…, enemyNature)` · `run.EnemyNature` · `RunState.EnemyNature` · `BattleSetup.EnemyNature` · `b.EnemyNature` ·
  `b.ToughViewOf(e)`(+ `ToughView`) · `b.WeakFor` · `b.ToughHits` · `b.ToughWeakHits` · `b.Breaks` · `b.ToughDealt` · `Run.RollNature(d, village, r01)` · `Run.NaturesFor` · `Run.NatureBySeed(d, village, seed)` · `Run.PickBosses`(1층 1~2성 · 2층 3성) · `Run.StarFits` · `Run.CloneSlots` · `Run.CloneCandidates` · `run.Bosses` · `run.BossHeroes` · `RunState.Bosses` · `GameData.CloneId` · `GameData.CLONE_MARK` · `R.FOE_NATURES` · `R.TOUGH.EliteMinion` · `R.TOUGH.Grid` · `R.TOUGH.Min`(3) · `ToughView.None` · `EnemyDef.Clone` · `Intent.NoTough` · `b.FoeNature(def)`. 지운 것: `R.TOUGH.Hit` · `R.TOUGH.Weak`(안 쓰던 옛 값).
  **규칙이 바뀐 것**: 새 판은 적 속성이 하나(1층 · 2층 일반 · 엘리트 · 소환 · 두 보스 — 보스는 그 마을 종족에서 그 성격 사도의 클론을 골라 세우고, 클론은 제 사도 성격 그대로라 판 전체가 한 성격) · 강인도 최소 3(보스의 noTough 소환물만 0) · 강인도 기본 칸 일반 4 · 엘리트 6 · 보스 10 · 엘리트 싸움의 여린 적 +1 · 강인도를 깎은 뒤 1/60 눈금으로(1/6 을 여러 번 빼면 1e-15 가 남아 격파가 안 나던 것을 고침) ·
  광역 판정은 퍼짐으로 넓어진 공격도.
  **적 실드**: 적의 방어(Block)는 적의 차례가 **시작될 때 한꺼번에** 사라진다(가호=실드 보존이면 남고 1 감소 · 실드 유지면 반) — 예전엔 적마다 제 행동 직전에 지워 앞 적이 준 「적 전체 방어」 를 뒤 적이 곧바로 잃었다. 사라짐 · 「실드 전부 파괴」 는 쪽지 `unguard`(새로), 파괴는 `guardBreak` 적 패시브 · `foeShieldBreak` 계기도 돈다. 결정화의 고정 실드(Shield)는 남는다.
  화면이 적 방어를 쪽지로 더해 셀 땐 `unguard` 의 To 로 맞추고, 확실하게는 `turn` · `foeTurn` 쪽지마다 `b.Enemies[i].Block + Shield` 로 다시 맞춘다. 기록에 「이름: 강인도 a → b / 최대」 한 줄. 강인도 단위 표는 데이터.md §15.

- **v2.1(2026-10-05 넷째)** — 덧붙이기만. 신탁 고르기 창: `List<OracleOption> b.EpiphanyOptions(cardId)`(전투) · `run.FlashOptions(FlashOffer)`(이벤트 · 캠프) — `OracleOption { int N; string Name, Text, Shin, BlessName, BlessText; bool Blessed }`(고른 것의 차례 번호를 `b.ApplyEpiphany` · 번호 N 을 `run.ResolvePending` · `run.CampTrain` 에).
  `FlashOffer.Shins`(후보마다 축복) · `run.RollOracles(cardId, not)` · `run.OfferOf(cardId, not, swap)` · `R.ORACLE_PICKS`(3) · `R.ORACLE_BLESS`(0.15, `R.DIVINE` 도 이 값).
  `b.CostMods` · `b.DebtNow` · `b.CardMatch(id, who, type, unique, basic, tag, owner)` · 쪽지 그대로. 시뮬: `MetaSim.PairsV2` · `CompTable` · `Solo` · `SoloMarkdown` · `MarkdownV2` · `Mesh` · `Gives` · `Hears`, 콘솔 `report`(새 표) · `solo`.
  규칙: 신탁은 어디서든 무작위 셋(이벤트 flash all 없앰) · 그을림이 파티에도 돈다(파티가 신속 아닌 카드를 낼 때).

- **v2(2026-10-05) — 키워드 사전**([키워드.md](키워드.md) · [키워드-엔진.md](키워드-엔진.md)). 덧붙이기만(이름 바꾼 것 · 지운 것 없음):
  `b.ActionCount` · `b.StatusViews`(+ `StatusView` · `StatusSource`) · `b.PartyStatus` · `b.HeroStatus` · `b.CardStOf` · `b.BondOf` · `b.IsFrozen` · `b.Removed` · `b.GrowthGain` · `b.CardSt` · `b.Bond` · `b.Later` ·
  `PlayOpts.Choice` · `CardView.Choices` · `CardText.TIPS` · `CardText.Tip` · `text.Tips()` · `text.Chips()` · `text.Enemy()` · `text.Counter()` · `run.MindBroken` · 쪽지 `revive` · card 쪽지 Label 몇.
  **규칙이 바뀐 것**(같은 API, 다른 값): 사기 파티 층 · 합연산 / 불굴 · 취약 · 피해 감소 합연산 / 실드는 턴이 바뀌면 사라짐 / 반격 · 협공은 건 사도 능력치 / 약점 +25% / 강인도는 카드 한 장에 AP 1당 1/3칸(약점 1칸) /
  증발은 소멸로 친다 / 조건 문장은 따로따로. 옛 운영 방식 부품은 「옛」 으로 남김(`FxK.Old`).

- v1(2026-10-04) — 첫 초안. 쪽지에 `card` · `turn` · `foeTurn` · `epiphany` · `over` 를 더했다.
- v1.3 — 적의 수 `summon`(쪽지 `summon` — 새 적이 b.Enemies 끝에 붙는다, Idx = 자리) · 조건부 수(`Intent.If`) · `EnemyDef.Art`(그림 키). 이벤트 선택지 `Hidden`(확률을 숨겨 보인다) · 고르는 것 remove 의 `Pending.Basic`(시작 카드만 — `run.IsBasic(id)`). play 패시브의 아군 1명 = 카드를 낸 사도.
- v1.2 — 데이터: `GameData.FromFolders(경로들)`(폴더 통째 · 묶음 파일) · `data.SourceOf(kind, id)`. 전투 상태: `b.PlayIds` · `b.ApSpent` · `b.PaidHp` · `b.DiscardedTurn` · `b.TakenPrev` · `b.Held`(카드 → 손에 묵은 턴) · `b.DebuffKinds(e)`. 대상 `topEnemy` · `lowEnemy`. 「손패 N장 소멸」(burn)도 `DiscardChoice` 가 센다 — 고른 카드는 `PlayOpts.Discard`. card 쪽지 Label 에 `burn`.
- v1.1 — 턴 종료 시 · 적의 차례에 패시브가 주는 AP 는 다음 턴으로 넘어간다(전에는 사라졌다). API 꼴은 그대로.
