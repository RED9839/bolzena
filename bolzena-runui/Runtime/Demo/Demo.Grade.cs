using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 학점제 학년 캡처(-demo-grade) — 지도 학년 알약(1학년) · 학년 보상 표 → 싸움(보상 화면 학점 칩) → 지도 진급 연출(2학년)
    //   → 5학년으로 맞춘 싸움(전투 HUD 학년 배지) → 졸업 연출 → 졸업 지도 → 졸업 전투 HUD. 학점은 시험용으로 바로 넣는다(진급 문턱 바로 밑).
    public partial class Demo
    {
        IEnumerator Grade_()
        {
            Core.R.GRADE_ON = true;   // 학년은 2026-10-09 꺼 두었다 — 이 캡처만 켜고 돈다
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.8f);
            yield return Shot("grade_map_g1");
            ExpectNotebook("1학년", Core.Grades.Need(2) - Core.Grades.Need(1), "grade/gift");
            yield return Press("grade", 0.8f);
            yield return Shot("grade_table");
            CloseModals();
            yield return Wait(0.3f);

            // ① 다음 싸움을 이기면 2학년
            f.P.S.Credits = Core.Grades.NEED[2] - 1;
            GradeBattleShots = 1;   // 이 싸움에서 전투 화면 학년 공책(시작 · 툴팁 · 학점 받는 순간)을 찍는다
            yield return FightOnce();
            ShowcaseOffer();
            yield return GradeShots("grade_stamp", "grade_pick", "grade_map_g2", false);

            // ② 5학년 · 졸업 바로 밑으로 — 이 싸움의 전투 HUD 는 5학년 배지, 이기면 졸업
            f.P.S.Grade = 5; f.P.S.Credits = Core.Grades.NEED[Core.Grades.GRAD] - 1; f.P.S.GradeOffers.Clear();
            yield return FightOnce();
            yield return GradeShots("grade_graduate", null, "grade_map_grad", true);

            // ③ 졸업한 파티의 전투 HUD(금빛 테 · 오라)
            yield return FightOnce();
            yield return Screen_("map", 1.2f);
            yield return Shot("grade_end");
            Debug.Log($"[Demo] 학년 캡처 끝 — {Core.Grades.Name(f.P.Run.Grade)} · 학점 {f.P.S.Credits}");
            yield return Wait(0.4f);
            Application.Quit(f.P.Run.Grade == Core.Grades.GRAD ? 0 : 5);
        }

        /// <summary>캡처용 — 첫 진급 보상을 전투 보상 셋(예습 노트 · 모의고사 · 골드)으로 바꾼다(데모는 예습 노트를 고른다 — 이어서 고를 것 없이, 지도 · 전투 HUD 에 남은 횟수).</summary>
        void ShowcaseOffer()
        {
            var off = f.P.Run.GradeOfferNow;
            if (off == null) return;
            off.Choices = new List<Core.GradeChoice>
            {
                new Core.GradeChoice { Kind = "prep", V = Core.Grades.ValueOf("prep", off.Grade) },
                new Core.GradeChoice { Kind = "mock", V = Core.Grades.ValueOf("mock", off.Grade) },
                new Core.GradeChoice { Kind = "gold", V = Core.Grades.ValueOf("gold", off.Grade) },
            };
        }

        static bool AnyGradeModal(Flow fl) => fl.Stage.ModalLayer.Find("modal gradeup") != null || fl.Stage.ModalLayer.Find("modal gradepick") != null || fl.Stage.ModalLayer.Find("modal graduate") != null;

        /// <summary>지도로 돌아온 뒤 — 진급 도장(또는 졸업장)을 찍고, 보상 고르기 창을 찍고 예습 노트(첫째)를 고른 뒤, 창이 닫힌 지도를 찍는다.</summary>
        IEnumerator GradeShots(string stampShot, string pickShot, string after, bool grad)
        {
            yield return Screen_("map", 0.2f);
            yield return Until(() => f.Stage.ModalLayer.Find(grad ? "modal graduate" : "modal gradeup") != null, 6);
            yield return Wait(grad ? 2.0f : 1.45f);   // 마지막 스티커 · 선물 상자 · 꽃 도장(≈1.2초)까지
            yield return Shot(stampShot);
            {
                var modal = f.Stage.ModalLayer.Find(grad ? "modal graduate" : "modal gradeup");
                var names = modal ? modal.GetComponentsInChildren<RectTransform>(true).Select(r => r.name).ToList() : new List<string>();
                Expect(names.Contains("slap") && names.Contains("perfectstamp") && names.Contains("gift"), $"{(grad ? "졸업" : "진급")} 순간 — 공책 마지막 스티커 「착!」 · 선물 상자 · 원작 「참!잘햇어요.」 도장");
                Expect(modal && modal.Find("ribbon") == null && modal.Find("paper/stamp") == null, $"{(grad ? "졸업" : "진급")} 순간 — 옛 빨간 「진급」 도장 · 분홍 리본 없음(공책 하나로)");
                Expect(names.Contains("sparkle"), "선물 상자가 열렸다(원작 반짝이)");
                var bub = modal ? modal.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "bubble:" + (grad ? "graduate" : "promote")) : null;
                Expect(bub != null && bub.Find("body/ribbon") != null && bub.GetComponentsInChildren<RectTransform>(true).All(r => r.name != "face"), "사도 한마디 — 말풍선(이름 리본 · 얼굴 동그라미 없음)");
            }
            if (!grad)
            {
                yield return Until(() => f.Stage.ModalLayer.Find("modal gradepick") != null, 6);
                yield return Wait(0.8f);
                yield return Shot(pickShot);
                yield return Press("grade.pick0", 0.6f);
                Expect(f.P.S.PrepLeft == Core.Grades.PREP_FIGHTS && f.P.S.PrepHand > 0, $"진급 보상 — 예습 노트를 골랐다(남은 {f.P.S.PrepLeft}전투 · 첫 손패 +{f.P.S.PrepHand})");
            }
            else yield return Press("grade.ok", 0.6f);
            yield return Until(() => !AnyGradeModal(f), 8);
            yield return Wait(0.7f);
            yield return Shot(after);
            if (grad) ExpectNotebook("졸업", Core.Grades.Need(Core.Grades.GRAD) - Core.Grades.Need(Core.Grades.GRAD - 1), "grade/gift");
            else ExpectNotebook($"{f.P.Run.Grade}학년", Core.Grades.Need(f.P.Run.Grade + 1) - Core.Grades.Need(f.P.Run.Grade), "grade/gift");
        }

        /// <summary>지도에서 싸움 하나를 이길 때까지 — 싸움 칸을 먼저(없으면 엘리트), 그 밖의 칸은 지나간다(-demo-quick 과 같은 길).</summary>
        IEnumerator FightOnce()
        {
            for (int step = 0; step < 8; step++)
            {
                float t = 0;
                while (t < 25 && (f.Stage.Busy || f.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") yield break;
                var reach = f.P.Reachable();
                var map = f.P.Map();
                var pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite") ?? reach.First();
                var type = Core.Run.NodeById(map, pick).Type;
                yield return Press("node:" + pick, 0.2f);
                yield return Wait(1f);
                switch (type)
                {
                    case "fight": case "elite": case "boss": yield return GoFight("win", type); yield break;
                    case "event": yield return GoEvent(); break;
                    case "camp": case "campshop": yield return GoCamp(type); break;
                }
            }
        }

        /// <summary>지도 HUD 칭찬 스티커 공책 — 제목에 학년 · 스티커 칸 수 · 끝 칸 그림(선물 상자 · 졸업은 꽃 도장) · 걸린 진급 보상 쪽지.</summary>
        void ExpectNotebook(string grade, int span, string giftSprite)
        {
            var hud = f.Stage.Root.GetComponentsInChildren<RectTransform>(false).FirstOrDefault(r => r.name == "hud.grade");
            var title = hud ? hud.Find("gname")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
            int slots = hud ? hud.GetComponentsInChildren<RectTransform>(false).Count(r => r.name.StartsWith("slot")) : 0;
            var gift = hud ? hud.Find("gift")?.GetComponent<UnityEngine.UI.Image>() : null;
            string plain = title ? System.Text.RegularExpressions.Regex.Replace(title.text, "<[^>]+>", "") : "";
            var cert = hud ? hud.Find("cert")?.GetComponent<UnityEngine.UI.Image>() : null;
            Expect(cert && cert.sprite && cert.sprite.name == "RunArt/Ui/order_cert", "지도 학년 공책 — 원작 「교단 증명서」 장식");
            Expect(hud != null && plain.StartsWith(grade) && slots == span && gift && gift.sprite && gift.sprite.name.EndsWith(giftSprite),
                $"지도 학년 공책 — 「{plain}」 · 스티커 칸 {slots}/{span} · 끝 칸 {gift?.sprite?.name}");
            var hp = f.Stage.Root.GetComponentsInChildren<RectTransform>(false).FirstOrDefault(r => r.name == "hud.left");
            if (hud && hp)
            {
                var a = new Vector3[4]; var b = new Vector3[4]; hud.GetWorldCorners(a); hp.GetWorldCorners(b);
                Expect(a[1].y <= b[0].y + 12, $"학년 공책이 파티 HP 알약과 겹치지 않음(공책 위 {a[1].y:0} · 알약 아래 {b[0].y:0})");
            }
        }
    }
}
