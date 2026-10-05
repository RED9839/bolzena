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
    //   로비 → 마을 공개 → 파티 편성 → 지도 ⇄ (싸움 → 보상 · 이벤트 · 캠프 ⇄ 상점) → … → 2층 보스 → 끝 화면
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
            if (!P.Ready) { Toast.Show("콘텐츠 데이터가 없습니다 — 마을 · 사도 셋이 있어야 떠납니다"); return; }
            string village = P.RollVillage();
            FoeSeed = DateTime.Now.Ticks & 0x7fffffff;
            FoeNature = P.RollFoeNature(village);
            FoeBosses = P.PickBosses(village, FoeNature, FoeSeed);
            VillageReveal(village, () => Party(village));
        }

        public void StartRun(System.Collections.Generic.List<string> party, string village)
        {
            RunPort.ClearSave();
            P.NewRun(party, village, FoeNature != null ? FoeSeed : DateTime.Now.Ticks & 0x7fffffff, FoeNature);
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
                    if (!P.EventLeft) { MapStep(); Toast.Show("조용한 길이었습니다 — 아무 일도 없었습니다"); return; }
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
            var offer = P.IsLastFloor ? new System.Collections.Generic.List<string>() : P.BossCopyOffer();
            if (offer.Count > 0) { P.Save("map"); BossCopyPick(offer, id => { P.BossCopy(id); NextFloor(); }); return; }
            NextFloor();
        }

        void NextFloor()
        {
            P.Advance();
            if (P.Cleared) { End("clear"); return; }
            Toast.Show($"{P.S.Floor + 1}층 · {P.Floor.Name} — 새 지도가 펼쳐집니다");
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
            RunPort.ClearSave();
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
