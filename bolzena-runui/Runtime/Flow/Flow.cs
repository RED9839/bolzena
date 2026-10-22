using System;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>전투 화면(트랙 C)이 꽂히는 자리 — 판이 연 싸움을 넘겨받아 끝나면 결과를 돌려준다. 비어 있으면 「전투 결과 받기」 자리표시.</summary>
    public interface IFightScreen
    {
        void Open(RunPort port, Action<RunPort.FightOutcome> done);
    }

    // 판의 흐름 — 웹판 js/main.js 를 옮겼다. 화면은 partial 파일마다 하나(Flow.Lobby.cs …).
    //   로비 → (단계 고르기) → 마을 고르기 → 마을 공개 → 파티 편성 → 지도 ⇄ (싸움 → 보상 · 이벤트 · 캠프 ⇄ 상점) → … → 2층 보스 → 끝 화면
    // 칸을 마칠 때마다 저장한다(이어하기 — 적어 둔 화면으로 곧장).
    public partial class Flow : MonoBehaviour
    {
        public Stage Stage;
        public RunPort P;
        public IFightScreen FightScreen;
        /// <summary>이번 판 적 속성 — 모험을 시작할 때(마을과 함께) 정하고, 마을 공개 · 편성에 보이고, 판을 열 때 넘긴다. null 이면 보이지 않는다.</summary>
        public string FoeNature;
        /// <summary>이번 판 씨앗 · 층마다 보스 줄 — 마을 공개에서 미리 고르고 같은 씨앗으로 판을 연다(화면의 보스 = 실제 보스).</summary>
        public long FoeSeed;
        public System.Collections.Generic.List<System.Collections.Generic.List<string>> FoeBosses;
        /// <summary>이번 판 난이도 단계(성장 켬일 때 — 모험 시작에서 고른다, Flow.Tier.cs). 0 = 단계 없음(성장 끔 — 지금 판).</summary>
        public int RunTier;
        /// <summary>전투 화면(FightScreen)에 싸움을 넘겨 두고 돌아오기를 기다리는 중.</summary>
        public bool Fighting { get; private set; }
        public static Flow Me { get; private set; }

        public static Flow Boot()
        {
            var go = new GameObject("RunUI Flow");
            var f = go.AddComponent<Flow>();
            return f;
        }

        void Awake()
        {
            Me = this;
            Settings.Apply();
            Stage = Stage.Create();
            Stage.transform.SetParent(transform, false);
            P = RunPort.Boot();
        }

        void Start()
        {
            if (!Demo.Active) Lobby();
        }

        // ── 판의 흐름 ──
        public void NewAdventure()
        {
            if (!P.Ready) { Toast.Show("콘텐츠 데이터가 없다 — 마을 · 사도 셋이 있어야 떠날 수 있다"); return; }
            Hitch.Mark("모험 시작");
            RunTier = ProgStore.On ? TierDefault() : 0;   // 성장 켬 — 난이도 단계는 마을 고르기 상세 판 안에서 고른다(Flow.Prog.cs TierRow, 2026-10-22). 꺼짐 = 단계 없음
            PickVillage();
        }

        /// <summary>마을 고르기(Flow.VillagePick.cs — 성장 켬이면 상세 판에 난이도 단계 줄) → 마을 공개 → 편성. 뒤로는 로비.</summary>
        void PickVillage()
        {
            Action back = Lobby;
            VillagePick(id =>
            {
                bool rolled = id == null;   // 「무작위」 — 지금처럼 씨앗으로 하나
                string village = rolled ? P.RollVillage() : id;
                FoeSeed = DateTime.Now.Ticks & 0x7fffffff;
                FoeNature = P.RollFoeNature(village);
                FoeBosses = P.PickBosses(village, FoeNature, FoeSeed);
                Hitch.Mark(rolled ? "마을 무작위 " + village : "마을 고름 " + village);
                VillageReveal(village, () => Party(village), rolled, PickVillage);
            }, back);
        }

        public void StartRun(System.Collections.Generic.List<string> party, string village)
        {
            Hitch.Mark("판 열기(편성 끝)");
            RunPort.ClearSave();
            using (Hitch.Span("판 열기 NewRun")) P.NewRun(party, village, FoeNature != null ? FoeSeed : DateTime.Now.Ticks & 0x7fffffff, FoeNature, RunTier);
            MapStep();
        }

        public void Resume()
        {
            var w = P.Load();
            if (w == null) { Lobby(); return; }
            var (where, kind) = w.Value;
            try
            {
                switch (where)
                {
                    case "event": EventStop(); return;
                    case "camp": Camp(kind ?? "camp"); return;
                    case "shop": Shop(kind ?? "campshop"); return;
                    case "fight": FightStop(); return;
                    case "gear": Settle(MapStep); return;
                    default: MapStep(); return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("이어하기 실패 — 저장을 버립니다 " + e);
                RunPort.ClearSave();
                Lobby();
            }
        }

        public void MapStep()
        {
            P.Save("map");
            Map();
        }

        public void EnterNode(Core.MapNode node)
        {
            switch (node.Type)
            {
                case "fight": case "elite": case "boss": FightStop(); return;
                case "event":
                    if (!P.EventLeft) { MapStep(); Toast.Show("조용한 길이었다 — 아무 일도 없었다"); return; }
                    EventStop(); return;
                case "camp": case "campshop": Camp(node.Type); return;
                default: MapStep(); return;
            }
        }

        public void EventStop()
        {
            P.EnterEvent();
            P.Save("event");
            Event();
        }

        public void FightStop()
        {
            P.Save("fight");
            if (FightScreen != null) { Fighting = true; FightScreen.Open(P, o => { Fighting = false; FightDone(o); }); }
            else FightStub();
        }

        public void FightDone(RunPort.FightOutcome o)
        {
            if (!o.Won) { End("lose"); return; }
            if (o.Event) { P.Save("event"); Event(); return; }
            Reward(o);
        }

        /// <summary>보상을 다 챙긴 뒤 — 보스였으면 고유 카드 복제를 고르고 층을 넘긴다. 그 밖은 지도로.</summary>
        public void AfterReward()
        {
            P.ClearElite();
            if (!P.IsBoss) { MapStep(); return; }
            var offer = P.BossCopyOffer();   // (10-18 사용자 「2층 클론을 잡았을 때도 1층처럼 복제 한 번 더」) 마지막 층도 — 고른 뒤 NextFloor 가 판 끝(clear)으로
            if (offer.Count > 0) { P.Save("map"); BossCopyPick(offer, id => { var cs = CardSnap(); P.BossCopy(id); GainCards(NewCards(cs), NextFloor); }); return; }
            NextFloor();
        }

        void NextFloor()
        {
            P.Advance();
            if (P.Cleared) { End("clear"); return; }
            Toast.Show($"{P.S.Floor + 1}층 · {P.Floor.Name} — 새 지도가 펼쳐진다");
            MapStep();
        }

        public void Camp(string kind)
        {
            P.EnterCamp(kind);
            P.Save("camp", kind);
            CampScreen(kind);
        }

        public void Shop(string kind)
        {
            P.Shop(false);
            P.Save("shop", kind);
            ShopScreen(kind);
        }

        public void LeaveCamp() => MapStep();

        public void End(string kind)
        {
            PlayRecord.Send(P, kind == "clear" ? "win" : "lose");   // 플레이 기록(Kit/PlayRecord.cs)
            ProgStore.Earn(P.Run, kind == "clear" ? "win" : "lose");   // 성장 켬 — 판 끝 보상(서버 계산) · 최고 완주 단계(Data/ProgStore.cs)
            RunPort.ClearSave();
            if (kind == "clear") PartyStore.CountClear(P.S);   // 사도별 완주 횟수(편성 화면 · 사도 목록에 「완주 N」)
            if (kind == "clear") SlotStore.HoldClear(P.Run);   // 사도 저장(프론티어) — 판 끝 사도 셋을 떠 둔다(로그인 전이어도 저장함에서 마저 저장, Flow.HeroSave.cs)
            EndScreen(kind);
        }

        /// <summary>받은 장비(Bag)가 남았으면 하나씩 끼기/팔기를 묻고, 다 정하면 then.</summary>
        public void Settle(Action then)
        {
            if (P.S.Bag.Count == 0) { then(); return; }
            GearDialog(P.S.Bag[0], () => Settle(then));
        }
    }
}
