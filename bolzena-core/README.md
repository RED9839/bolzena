# com.bolzena.core — 볼제나 게임 로직 코어

웹판 볼제나(순수 JS)의 규칙을 **순수 C#**(UnityEngine 참조 없음)으로 다시 짠 UPM 로컬 패키지.
전투 규칙 엔진 · 판(마을 두 층 · 지도 · 보상 · 신탁 · 캠프 · 상점 · 장비 · 이벤트 · 저장) · 데이터 틀 · 카드 글 생성기 · 검사기 · 봇 · 메타 시뮬.

- 데이터 틀: [Docs/데이터.md](Docs/데이터.md) · 샘플 콘텐츠: `Data/Sample/`
- 의존: `com.unity.nuget.newtonsoft-json`(JSON)
- 테스트: `powershell -File run-tests.ps1`(테스트 프로젝트 `C:\projects\bolzena-core-test`)

## 화면이 부르는 것

```csharp
var data = GameData.FromFolder(path);            // 또는 new GameData().Add(json…)
var check = Validator.Check(data);                // check.Errors · check.Warnings
var text = new CardText(data);                    // text.Card(cardView) · Hero(도감) · Passives · Keyword · Intent · Outcomes · Equip

// 판
var run = Run.New(data, new List<string>{ "rico", "carrot", "sion" }, seed, village: Run.RollVillage(data, rnd));
run.MapOf(); run.Reachable(); run.EnterNode(id);  // 지도
var (b, loot) = run.OpenFight(cues: cues, onCue: c => …);
run.AfterFight(b); run.TakeGold(); run.TakeEquip(id); run.ClaimGlow(cardId, glow, i);
run.EnterCamp(kind); run.CampRest(); run.CampTrain(n);
run.RollShop(); run.Buy(i); run.RerollShop(); run.RemoveCard(id);
run.GainEquip(id); run.Equip(hero, id, replace); run.SellEquip(id);
run.EnterEvent(); run.OptionsOf(ev); run.LockOf(o); run.JudgeOf(o); run.Choose(i); run.ResolvePending(v); run.AfterEventFight(won); run.LeaveEvent();
run.BossCopyOffer(); run.BossCopy(id); run.Advance();    // run.S.Done == "clear"
string save = run.Save(); run = Run.Load(data, save);

// 전투
b.PlayCard(handIdx, targetIdx, new PlayOpts { Discard = …, Ally = … });   // PlayResult { Ok, Why, Finale }
b.CanPlay(id, handIdx: i); b.CostOf(id, i); b.CardOf(id).Target; b.DiscardChoice(i);
b.GlowOf(id); b.ApplyEpiphany(id, choice);         // 빛나는 카드 — 내기 전에 고른다
b.CanUlt(hero); b.UseUlt(hero, target);
b.PreviewCard(i, t); b.PreviewPartyOf(i, t); b.PreviewUlt(hero, t);
b.EndTurn();                                       // 턴 끝 · 적의 차례 · 다음 턴 시작
// 상태 읽기: b.Over · b.Turn · b.Ap · b.Gauge · b.Pool(파티 한 몸) · b.Party · b.Enemies · b.Hand · b.Draw · b.Discard · b.Gone
//           b.St(unit, "취약") · b.StackOf(hero, "마탄") · b.StatMod(unit, "atk") · b.IntentHit(e) · b.RushOf(e) · e.RushCnt · e.Tough
// 이벤트: b.OnCue(연출 쪽지) · b.OnLog(기록 한 줄) · b.Cues(목록) · b.Log
string bs = b.Save(); b = Battle.Load(data, bs);

// 봇 · 시뮬
new Bots(data).SmartPlay(b);
new RunBot(data).RunFull(party, seed, new SimOpts());
MetaSim.Report(data, MetaSim.Run(data, rounds: 30));
```
