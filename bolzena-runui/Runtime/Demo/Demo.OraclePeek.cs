using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카드 상세 · 신탁 미리보기 점검(-demo-oraclepeek, 2026-10-07 사용자 「번뜩임들 보여 주고 축복은 보여 주지 말자」 · 카제나 「카드 상세」 배치)
    //   도감 → 사도 상세(카드 탭) → 고유 카드 상세(축복 목록 없음) → 눈(그림만) → 다음 카드 → 「신탁」 단추 → 신탁 다섯 장 창 → 뒤로 키 둘.
    //   만들어지는 카드가 있는 고유 카드(왼쪽 작은 카드)도 한 장. -card 로 처음 카드를 고른다.
    public partial class Demo
    {
        IEnumerator OraclePeek_()
        {
            AudioListener.volume = 0f;   // 점검은 음소거
            yield return Wait(0.5f);
            f.Lobby();
            yield return Wait(1.0f);
            AudioListener.volume = 0f;
            var d = f.P.Data;
            bool Five(Core.CardDef c) => c != null && c.Unique && c.Hero != null && c.Oracles.Count >= 5;
            bool Makes(Core.CardDef c) { var v = f.P.View(c.Id); return v != null && CardTerms.Of(d, f.P.Text, v, f.P.Text.Card(v)).Any(t => t.IsCard); }
            var first = d.Card(Arg("-card") ?? "포셔_u2");
            if (!Five(first)) first = d.Cards.Values.Where(Five).OrderBy(c => c.Id, System.StringComparer.Ordinal).First();
            var maker = d.Cards.Values.Where(Five).Where(Makes).OrderBy(c => c.Id, System.StringComparer.Ordinal).FirstOrDefault();

            f.Dex(() => f.Lobby());
            yield return Wait(1.2f);
            var hero = Roster.OfCore(first.Hero);
            f.HeroDetail(hero.key, null, null, "카드");
            yield return Wait(1.4f);
            yield return Shot("op_01_hero_cards");
            // 처음 카드를 누른다(사도 상세 카드 목록 → 카드 상세)
            var all = CardOrder.Sort(d.Hero(first.Hero).Starter, d, new[] { first.Hero }).Concat(CardOrder.Sort(d.UniquesOf(first.Hero), d, new[] { first.Hero })).ToList();
            int idx = all.IndexOf(first.Id);
            var hot = f.Stage.Hot.Keys.FirstOrDefault(k => k == "detail.card고유 카드" + CardOrder.Sort(d.UniquesOf(first.Hero), d, new[] { first.Hero }).IndexOf(first.Id));
            if (hot != null) yield return Press(hot, 1.2f); else { f.CardZoom(first.Id, all, idx); yield return Wait(1.2f); }
            Expect(f.Stage.Hot.ContainsKey("zoom.oracle"), "신탁 있는 카드 — 「신탁」 단추");
            Expect(!Texts().Any(t => t.Contains("겨우살이의 축복") || t.Contains("공용 축복")), "카드 상세에 받을 수 있는 축복 목록이 없다");
            yield return Shot("op_02_detail");
            f.DemoEye(true);
            yield return Wait(0.5f);
            yield return Shot("op_03_eye");
            f.DemoEye(false);
            yield return Wait(0.3f);
            yield return Press("zoom.oracle", 1.3f);
            Expect(Texts().Any(t => t.Contains("이 카드에는 아래의 신탁이 나올 수 있습니다")), "신탁 미리보기 안내 글");
            Expect(!Texts().Any(t => t.Contains("축복")), "신탁 미리보기에 축복이 없다");
            yield return Shot("op_04_oracles");
            f.Stage.Back();
            yield return Wait(0.5f);
            yield return Shot("op_05_back_to_detail");
            yield return Press("zoom.next", 1.0f);
            yield return Shot("op_06_detail_next");
            f.Stage.Back();
            yield return Wait(0.5f);
            if (maker != null)
            {
                f.CardZoom(maker.Id);
                yield return Wait(1.2f);
                yield return Shot("op_07_detail_made_" + maker.Id);
                yield return Press("zoom.oracle", 1.3f);
                yield return Shot("op_08_oracles_" + maker.Id);
                CloseModals();
                yield return Wait(0.3f);
            }
            // 신탁이 없는 카드 — 「신탁」 단추가 없다(교주 카드도 신탁이 있으니 시작 카드 · 상태 카드에서 찾는다)
            var plain = d.Hero(first.Hero).Starter.FirstOrDefault(id => (d.Card(id)?.Oracles.Count ?? 0) == 0)
                ?? d.Cards.Values.Where(c => c.Oracles.Count == 0).Select(c => c.Id).OrderBy(x => x, System.StringComparer.Ordinal).FirstOrDefault();
            if (plain != null)
            {
                f.CardZoom(plain);
                yield return Wait(1.0f);
                Expect(!f.Stage.Hot.ContainsKey("zoom.oracle"), $"신탁 없는 카드({plain}) — 단추 없음");
                yield return Shot("op_09_plain_detail");
                CloseModals();
            }
            // 교주 카드(도감 교주 카드 탭에서 열기)
            f.Dex(() => f.Lobby());
            yield return Wait(1.0f);
            yield return Press("tab:교주 카드", 1.0f);
            yield return Press(FirstHot("dexcard:"), 1.2f);
            yield return Shot("op_10_leader_detail");
            Debug.Log($"[Demo] 신탁 미리보기 점검 끝 — 처음 {first.Id} · 만들어지는 카드 {maker?.Id ?? "없음"} · 단언 실패 {fails}");
            Application.Quit(fails > 0 ? 1 : 0);
        }

        /// <summary>지금 창(맨 위 모달)의 글 전부.</summary>
        List<string> Texts()
        {
            var ml = f.Stage.ModalLayer;
            if (ml.childCount == 0) return new List<string>();
            return ml.GetChild(ml.childCount - 1).GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Select(t => t.text ?? "").ToList();
        }
    }
}
