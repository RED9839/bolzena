using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 사도 한마디 시범(-demo-herosay, 2026-10-09) — 판 중 말풍선은 보스 시작 · 승리 두 갈래만(사용자 결정): 그 둘은 뜨고, 휴식 · 상점 · 이벤트 · 위기 · 보상에는 안 뜨는지 단언한다.
    //   로비 말풍선 · 편성 화면 · 낀 아티팩트 창 · 상점 진열(「아티팩트」 칸) · 승리 띠(「승리!」) · 보상 줄(「아티팩트」)도 같이 찍는다. 싸움은 보스 자리로 열고 파티 HP 를 30% 바로 위로 내린다.
    public partial class Demo
    {
        static GameObject SayObj(string kind) =>
            Resources.FindObjectsOfTypeAll<RectTransform>().FirstOrDefault(r => r && r.gameObject.scene.IsValid() && r.gameObject.activeInHierarchy && r.name == "herosay:" + kind)?.gameObject;

        /// <summary>그 갈래 말풍선이 떠서 다 보일 때까지 기다렸다 찍는다. 뜨면 true.</summary>
        IEnumerator SayShot(string kind, float timeout, System.Action<bool> seen)
        {
            float t = 0;
            GameObject g = null;
            while (t < timeout && (g = SayObj(kind)) == null) { t += Time.unscaledDeltaTime; yield return null; }
            if (g == null) { seen(false); yield break; }
            // 떠오르는 동안(지연 + 0.18초)을 기다린다 — 움직임 줄이기면 더 짧다
            var cg = g.GetComponent<CanvasGroup>();
            float w = 0;
            while (w < 2f && cg && cg.alpha < 0.99f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Shot("say_" + kind);
            seen(true);
        }

        IEnumerator HeroSay_()
        {
            Settings.HeroTalk = true;
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.4f);
            yield return Shot("lobby_bubble");   // 로비 말풍선도 같은 진입점(HeroBubble.Show)
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Shot("term_party");
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);
            f.P.S.Gold = Mathf.Max(f.P.S.Gold, 300);
            bool ok = false;
            // 낀 아티팩트 창(편성 사도마다 무기 · 방어구 · 장신구 칸)
            f.GearView();
            yield return Wait(1.0f);
            yield return Shot("term_gear");
            CloseModals();
            yield return Wait(0.3f);

            // 휴식 · 상점 · 이벤트 — 판 중 대사는 보스 시작 · 승리만(2026-10-09 사용자 결정): 여기서는 말풍선이 뜨지 않아야 한다
            f.Camp("camp");
            yield return SayShot("rest", 2.5f, v => ok = v);
            Expect(!ok, "휴식 도착 — 말풍선 없음");
            f.Shop("campshop");
            yield return SayShot("shop", 2.5f, v => ok = v);
            Expect(!ok, "상점 도착 — 말풍선 없음");
            yield return Shot("term_shop");
            var ev = f.P.Data.Events.FirstOrDefault(e => e.Options.Count >= 2);
            if (ev != null)
            {
                OpenEvent(ev.Id);
                yield return SayShot("event", 3, v => ok = v);
                Expect(!ok, $"이벤트 진입 — 말풍선 없음 ({ev.Id})");
                f.P.LeaveEvent();
            }

            // 싸움 — 보스로(층 보스 자리) · 첫 턴을 넘겨 적의 공격도 받아 본다(위기 한마디는 뜨지 않아야 한다)
            CloseModals();
            f.MapStep();
            yield return Screen_("map", 1.0f);
            var reach = f.P.Reachable();
            var map = f.P.Map();
            var pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite");
            if (pick == null) { Expect(false, "갈 수 있는 싸움 칸이 없다"); yield break; }
            var node = f.P.Enter(pick);
            f.P.S.Node = 3;   // 보스 싸움(Run.IsBoss)
            f.P.S.PartyHp = Mathf.CeilToInt(f.P.S.PartyMaxHp * 0.3f) + 2;
            FightMode = "pass1";   // 첫 턴을 넘겨 적의 공격을 받는다
            f.EnterNode(node);
            bool start = false, low = false, win = false, reward = false;
            float t = 0;
            while (t < 300)
            {
                if (!start && SayObj("bossStart") != null) { yield return Wait(0.3f); yield return SayShot("bossStart", 1, v => start = v); }
                if (!low && SayObj("lowhp") != null) low = true;
                if (!win && SayObj("victory") != null) { yield return Wait(0.45f); yield return SayShot("victory", 1, v => win = v); }
                if (!f.Fighting && f.Stage.Current == "reward") break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            FightMode = "win";
            yield return SayShot("reward", 3, v => reward = v);
            Expect(start, "보스 시작 — 사도 말풍선");
            Expect(win, "보스 승리 직후 — 사도 말풍선");
            Expect(!low, "위기 — 말풍선 없음");
            Expect(!reward, "보상 화면 — 말풍선 없음");
            yield return Wait(0.5f);
            yield return Shot("term_reward");

            // 설정 — 전투 탭의 「사도 말풍선」
            f.SettingsPanel(true);
            yield return Wait(0.8f);
            yield return Press("settings.tab2", 0.7f);
            Expect(Hot("set.herotalk") != null, "설정 — 사도 말풍선 스위치");
            yield return Shot("settings_herotalk");
            yield return Press("settings.close", 0.6f);

            Debug.Log($"[Demo] 사도 한마디 단언 실패 {fails}");
            yield return Wait(0.4f);
            Application.Quit(fails == 0 ? 0 : 4);
        }
    }
}
