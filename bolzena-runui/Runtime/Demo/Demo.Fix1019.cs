using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bolzena.RunUI
{
    // 10-19 지적 확인 캡처(-demo-fix1019, 서버 없이) — ① 계정 창 로그인 단추(구글 먼저 · 작게 — 웹 줄을 PC 에서 그려 찍는다)
    //   ② 프론티어 입구 「주요 패턴」 펼친 목록(7줄 · 판끼리 안 겹침 · PC 는 스크롤 없이, 폰 -phone 은 넘치면 판 안 스크롤)
    //   ③ 도감 → 사도 상세에 「모션 확인」 단추가 없는지(공개판 — 에디터 · -motionlab 이 아니면) · 「저장한 덱」 → 그 사도로 거른 저장함 → 뒤로. 저장은 -save 로 따로 두고 돌린다.
    public partial class Demo
    {
        IEnumerator Fix1019_()
        {
            int f0 = fails;
            yield return Wait(0.5f);
            yield return Until(() => Online.Now != Online.State.Unknown, 10);
            f.Lobby();
            yield return Screen_("lobby", 1.2f);

            // ① 계정 창 — 로그인 전(웹 단추 줄)
            Online.DemoSet(Online.State.Out);
            Flow.DemoWebLogin = true;
            f.AccountPanel();
            yield return Wait(0.9f);
            yield return Shot("fix_account_web");
            var g = Hot("acct.google"); var d = Hot("acct.discord");
            Expect(g != null && d != null, "구글 · 디스코드 단추가 있음");
            if (g != null && d != null)
            {
                var gr = g.GetComponent<RectTransform>(); var dr = d.GetComponent<RectTransform>();
                Debug.Log($"[Fix] 로그인 단추 — 구글 x {gr.position.x:0} · 디스코드 x {dr.position.x:0} · 크기 {gr.rect.width:0}×{gr.rect.height:0} / {dr.rect.width:0}×{dr.rect.height:0} · Compact {Theme.Compact}");
                Expect(gr.position.x < dr.position.x, "구글이 디스코드보다 앞(왼쪽)");
                Expect(gr.rect.height <= Theme.BtnH && gr.rect.width <= 210, $"로그인 단추가 보조 단추보다 작다 — {gr.rect.width:0}×{gr.rect.height:0}");
            }
            Flow.DemoWebLogin = false;
            CloseModals();
            f.AccountPanel();   // PC 줄(브라우저로 로그인)
            yield return Wait(0.9f);
            yield return Shot("fix_account_pc");
            CloseModals();
            Online.DemoSet(Online.State.Off);

            // ② 프론티어 입구 — 주요 패턴 펼친 목록(2026-10-19 칩 + 팝업을 없앰): 7줄 · 판끼리 안 겹침 · 넘치면 판 안 스크롤(폰)
            f.FrontierScreen();
            yield return Screen_("frontier", 2.0f);
            yield return Shot("fix_frontier_entry");
            Expect(f.FrontierMarks >= 7, $"M.E.O.W 패턴 줄 {f.FrontierMarks}(출력 · 심심함 · 캐논 · 프레스 · 드론 · 통제 신호 · 사냥 · 폭주)");
            Expect(f.FrontierGap >= 0, $"보스 판이 내 기록 판과 안 겹침 — 틈 {f.FrontierGap:0}");
            Expect(Hot("frontier.mark0") == null, "패턴 칩(팝업) 없음 — 늘 보이는 목록");
            Debug.Log($"[Fix] 패턴 목록 {f.FrontierMarks}줄 · 판 안 스크롤 {f.FrontierMarksScroll} · Compact {Theme.Compact} · 화면 {Screen.width}×{Screen.height}");
            if (!Theme.Compact) Expect(!f.FrontierMarksScroll, "PC — 패턴 7줄이 스크롤 없이 다 보인다");

            // ③ 도감 → 사도 상세 — 「모션 확인」 없음
            f.Dex(null);
            yield return Screen_("dex", 1.2f);
            var h = Roster.Players.FirstOrDefault();
            if (h != null)
            {
                f.HeroDetail(h.key, null, null);
                yield return Wait(1.6f);
                yield return Shot("fix_dex_detail");
                Debug.Log($"[Fix] 모션 확인 손 {(Flow.MotionLab != null ? "있음" : "없음")} · 단추 {(Hot("detail.motionlab") != null ? "보임" : "없음")} · 에디터 {Application.isEditor}");
                Expect(Hot("detail.motionlab") == null, "공개판 도감 상세에 「모션 확인」 단추 없음");
                Expect(Hot("detail.saved") != null, "도감 사도 상세에 「저장한 덱」");
                if (Hot("detail.saved") != null)
                {
                    yield return Press("detail.saved", 0.3f);
                    yield return Screen_("slotbox", 1.2f);
                    yield return Shot("fix_dex_saved");
                    yield return Press("back");
                    yield return Wait(1.4f);
                    Expect(f.Stage.ModalLayer.Find("modal herodetail") != null, "저장함에서 뒤로 → 도감 사도 상세로");
                }
            }
            CloseModals();
            Debug.Log($"[Demo] 10-19 확인 데모 끝 — 단언 실패 {fails - f0}");
            yield return Wait(0.3f);
            Application.Quit(fails - f0 == 0 ? 0 : 7);
        }
    }
}
