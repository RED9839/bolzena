using System.Collections;
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
            yield return Press("grade", 0.8f);
            yield return Shot("grade_table");
            CloseModals();
            yield return Wait(0.3f);

            // ① 다음 싸움을 이기면 2학년
            f.P.S.Credits = Core.Grades.NEED[2] - 1;
            yield return FightOnce();
            yield return GradeShots("grade_promote", "grade_map_g2");

            // ② 5학년 · 졸업 바로 밑으로 — 이 싸움의 전투 HUD 는 5학년 배지, 이기면 졸업
            f.P.S.Grade = 5; f.P.S.Credits = Core.Grades.NEED[Core.Grades.GRAD] - 1;
            yield return FightOnce();
            yield return GradeShots("grade_graduate", "grade_map_grad");

            // ③ 졸업한 파티의 전투 HUD(금빛 테 · 오라)
            yield return FightOnce();
            yield return Screen_("map", 1.2f);
            yield return Shot("grade_end");
            Debug.Log($"[Demo] 학년 캡처 끝 — {Core.Grades.Name(f.P.Run.Grade)} · 학점 {f.P.S.Credits}");
            yield return Wait(0.4f);
            Application.Quit(f.P.Run.Grade == Core.Grades.GRAD ? 0 : 5);
        }

        /// <summary>지도로 돌아온 뒤 진급 연출을 기다려 찍고(배지가 뒤집힌 뒤), 연출이 닫힌 지도도 찍는다.</summary>
        IEnumerator GradeShots(string promote, string after)
        {
            yield return Screen_("map", 0.2f);
            yield return Until(() => f.Stage.ModalLayer.Find("modal gradeup") != null, 6);
            yield return Wait(1.5f);
            yield return Shot(promote);
            yield return Until(() => f.Stage.ModalLayer.Find("modal gradeup") == null, 8);
            yield return Wait(0.6f);
            yield return Shot(after);
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
    }
}
