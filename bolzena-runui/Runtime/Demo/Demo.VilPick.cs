using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 마을 고르기(-demo-vilpick, 2026-10-21) — 모험 시작 → (단계) → 마을 고르기: 노드 6 + 무작위 · 겹침 없음(노드끼리 · 머리 · 상세 판 · 화면 밖) →
    //   마을마다 눌러 상세(이름 · 출발 단추 · 대표 아티팩트 수) → 아티팩트 효과 판 → 무작위 상세 → 미확인 노드(데모만) →
    //   고른 마을 출발 → 마을 공개(그 마을 · 이름 굴리기 없음) → 「다시 고르기」로 되돌아옴 → 무작위 출발 → 공개 → 고른 마을로 편성 → 지도(판의 마을 = 고른 마을)
    public partial class Demo
    {
        IEnumerator VilPick_()
        {
            int f0 = fails;
            var vils = f.P.Villages;
            string target = vils.Any(v => v.Id == "spirit") ? "spirit" : vils[vils.Count / 2].Id;
            yield return Wait(0.5f);

            IEnumerator Open()
            {
                f.Lobby();
                yield return Screen_("lobby", 0.8f);
                yield return Press("start", 0.3f);
                yield return Screen_("vilpick", 1.6f);
            }
            yield return Open();
            Expect(f.Stage.Current == "vilpick", "모험 시작 → 마을 고르기");
            yield return Shot("vilpick");

            // 노드 — 마을마다 하나 + 무작위
            Expect(Hot("vilpick.random") != null, "「무작위」 노드");
            foreach (var v in vils) Expect(Hot("vilpick." + v.Id) != null, $"노드 {v.Name}");
            Expect(Hot("vilpick.go")?.Label?.text == "무작위로 출발", $"처음 고른 것 = 무작위 ({Hot("vilpick.go")?.Label?.text})");
            Expect((Hot("tier.1") != null) == ProgStore.On, ProgStore.On ? "성장 켬 — 무작위 상세 판에도 난이도 단계 줄" : "성장 끔 — 단계 줄 없음(지금 공개판 그대로)");
            CheckLayout();

            // 마을마다 상세
            foreach (var v in vils)
            {
                yield return Press("vilpick." + v.Id, 0.35f);
                var det = Find("detail");
                Expect(Hot("vilpick.go")?.Label?.text == "이 마을로 출발", $"{v.Name} — 출발 단추");
                Expect(det != null && det.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.text == v.Name), $"{v.Name} — 상세 이름");
                int show = f.P.Data.VillageShowcase(v.Id).Count, rows = det != null ? det.GetComponentsInChildren<RectTransform>(true).Count(r => r.name.StartsWith("equip ")) : -1;
                Expect(rows == show, $"{v.Name} — 대표 아티팩트 {rows}/{show}");
                int elites = det != null ? det.GetComponentsInChildren<RectTransform>(true).Count(r => r.name.StartsWith("elite ")) : 0;
                Expect(elites > 0, $"{v.Name} — 엘리트 얼굴 {elites}");
            }
            yield return Press("vilpick." + target, 0.8f);
            yield return Shot("vilpick_detail");
            if (Hot("vilpick.equip0") != null)
            {
                yield return Press("vilpick.equip0", 0.7f);
                Expect(TermPop.IsOpen, "대표 아티팩트 누름 → 효과 판");
                yield return Shot("vilpick_equip");
                TermPop.Close();
                yield return Wait(0.3f);
            }
            else Debug.Log("[Demo] 대표 아티팩트 없음(마을 장비 패치 없는 콘텐츠) — 안내 한 줄");
            yield return Press("vilpick.random", 0.8f);
            Expect(Hot("vilpick.go")?.Label?.text == "무작위로 출발", "무작위 상세 — 출발 단추");
            yield return Shot("vilpick_random");

            // 미확인 노드(나중 잠김 연출 자리) — 데모에서만 하나 감춰 본다
            string hid = vils.Last().Id;
            f.VillagePick(_ => { }, f.Lobby, new[] { hid });
            yield return Screen_("vilpick", 1.6f);
            Expect(Hot("vilpick." + hid) != null && !Hot("vilpick." + hid).Interactable, "미확인 노드 — 누를 수 없음");
            yield return Shot("vilpick_hidden");

            // 고른 마을 → 마을 공개 → 다시 고르기
            yield return Open();
            yield return Press("vilpick." + target, 0.4f);
            yield return Press("vilpick.go", 0.2f);
            yield return Screen_("village", 2.6f);
            var tv = f.P.Village(target);
            Expect(f.Stage.Current == "village", "출발 → 마을 공개");
            Expect(NameOnReveal() == tv.Name, $"공개된 마을 = 고른 마을 ({NameOnReveal()})");
            Expect(f.FoeBosses != null && f.FoeBosses.Count > 0, "고른 마을의 보스 줄");
            yield return Shot("vilpick_reveal");
            Expect(Hot("back")?.Label?.text == "다시 고르기", "마을 공개 — 「다시 고르기」");
            yield return Press("back", 0.2f);
            yield return Screen_("vilpick", 1.0f);
            Expect(f.Stage.Current == "vilpick", "다시 고르기 → 마을 고르기");

            // 무작위 출발 → 공개(이름 굴리기)
            yield return Press("vilpick.go", 0.2f);
            yield return Screen_("village", 2.6f);
            var rn = NameOnReveal();
            Expect(vils.Any(v => v.Name == rn), $"무작위 → 마을 하나 ({rn})");
            yield return Shot("vilpick_reveal_random");

            // 고른 마을로 판까지
            yield return Open();
            yield return Press("vilpick." + target, 0.4f);
            yield return Press("vilpick.go", 0.2f);
            yield return Screen_("village", 2.6f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            Expect(f.P.S != null && f.P.S.Village == target, $"판의 마을 = 고른 마을 ({f.P.S?.Village})");
            if (f.FoeNature != null) Expect(f.P.S != null && f.P.S.Seed == f.FoeSeed, "판 씨앗 = 마을 공개 때 씨앗");
            yield return Shot("vilpick_map");

            Debug.Log($"[Demo] 마을 고르기 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }

        RectTransform Find(string name) => f.Stage.transform.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == name);

        /// <summary>마을 공개의 큰 이름(굴리기가 멈춘 뒤).</summary>
        string NameOnReveal()
        {
            var main = Find("main");
            var ts = main != null ? main.GetComponentsInChildren<TextMeshProUGUI>(true) : new TextMeshProUGUI[0];
            return ts.Length > 1 ? ts[1].text : null;   // 0 = 「이번 모험의 마을」, 1 = 이름
        }

        /// <summary>노드끼리 · 머리 · 상세 판과 겹치지 않고 화면 안에 있는지(월드 사각형으로 — 같은 캔버스).</summary>
        void CheckLayout()
        {
            Rect W(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            var nodes = f.Stage.transform.GetComponentsInChildren<RectTransform>(true).Where(r => r.name.StartsWith("node ")).ToList();
            Expect(nodes.Count == f.P.Villages.Count + 1, $"노드 {nodes.Count}개");
            int bad = 0;
            var rects = nodes.Select(W).ToList();
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    if (rects[i].Overlaps(rects[j])) { bad++; Debug.Log($"[Demo] 겹침 {nodes[i].name} × {nodes[j].name}"); }
            Expect(bad == 0, $"노드끼리 겹침 {bad}");
            var det = Find("detail"); var head = Find("head"); var scr = Find("vilpick");
            var dr = det != null ? W(det) : Rect.zero; var hr = head != null ? W(head) : Rect.zero; var sr = scr != null ? W(scr) : Rect.zero;
            int over = 0, outside = 0;
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Overlaps(dr) || rects[i].Overlaps(hr)) { over++; Debug.Log($"[Demo] 판 겹침 {nodes[i].name}"); }
                if (rects[i].xMin < sr.xMin || rects[i].xMax > sr.xMax || rects[i].yMin < sr.yMin || rects[i].yMax > sr.yMax) { outside++; Debug.Log($"[Demo] 화면 밖 {nodes[i].name}"); }
            }
            Expect(over == 0, $"노드가 머리 · 상세 판과 겹침 {over}");
            Expect(outside == 0, $"노드가 화면 밖 {outside}");
            Expect(det != null && dr.yMin >= sr.yMin - 1 && dr.xMax <= sr.xMax + 1, "상세 판이 화면 안");
        }
    }
}
