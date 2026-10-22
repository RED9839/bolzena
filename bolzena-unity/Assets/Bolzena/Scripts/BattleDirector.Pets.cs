using System.Collections;
using System.Collections.Generic;
using Bolzena.View;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 따라다니는 소환수 — 원작에서 사도 곁에 늘 서 있는 작은 동료(에피카의 에피칸). 원작 영상(ult_videos/에피카 기본공격 · 저학년 · 고학년, 2026-10-15):
    //   에피칸은 에피카 왼쪽 뒤에 내내 서 있고, 강공에 뛰어나가 불을 뿜고(Attack2_1), 저학년에 같이 춤춘다(Skill1_1). 고학년 동안에도 제자리에 서 있다(10-18 원작 고학년 영상 대조 — 전엔 「제자리에 없다」로 보고 숨겼다)
    public partial class BattleDirector
    {
        class PetSpec
        {
            public string Spine;                    // Resources/Spine 폴더
            public Vector2 Off;                     // 사도 발에서(사도가 보는 쪽이 +x)
            public Dictionary<Motion, string> Anim; // 사도 카드 몸짓 → 소환수 몸짓
            public HashSet<Motion> Shoots;          // 이 몸짓의 투사체는 소환수 입(Point_Attack1)에서 나간다 — 소환수 몸짓의 쏘는 이벤트가 투사체 떠나는 때에 오게 몸짓을 당긴다
        }

        static readonly Dictionary<string, PetSpec> PETS = new Dictionary<string, PetSpec>
        {
            ["에피카"] = new PetSpec
            {
                Spine = "epicon", Off = new Vector2(-1.05f, 0.02f),
                Anim = new Dictionary<Motion, string> { [Motion.Attack1] = "Attack1_1", [Motion.Attack2] = "Attack2_1", [Motion.Skill1] = "Skill1_1" },
                Shoots = new HashSet<Motion> { Motion.Attack1 },   // (10-17 정밀 분석) 강공 불붙은 에피콘 탄은 에피카 손에서   // (10-17 사용자) 평타도 에피콘이 던진다(탬버린 · 음표)   // 원작 강공 — 에피칸이 뛰어나가 불덩이를 뱉는다(에피카 손이 아니라)
            },
        };

        readonly Dictionary<UnitView, UnitView> pets = new Dictionary<UnitView, UnitView>();
        UnitView ultOwnerView;   // 지금 고학년을 쓰는 사도(그 사도의 소환수는 숨긴다)

        void PetSpawn(UnitView hero, string hid)
        {
            if (hero == null || !PETS.TryGetValue(hid, out var sp) || pets.ContainsKey(hero) || Res.Spine(sp.Spine) == null) return;
            var pet = UnitView.Create(FieldRoot, "pet_" + sp.Spine, sp.Spine, "Normal", hero.Facing, UnitScale, PetPos(hero, sp), hero.BaseOrder - 1);
            pets[hero] = pet;
            StartCoroutine(PetFollow(hero, pet, sp));
        }

        static Vector3 PetPos(UnitView hero, PetSpec sp) => hero.transform.localPosition + new Vector3((hero.Facing ? 1 : -1) * sp.Off.x, sp.Off.y, 0);

        // 사도 자리를 따라가고(퍼지기 · 줄 바꿈), 사도가 안 보이면 숨는다
        IEnumerator PetFollow(UnitView hero, UnitView pet, PetSpec sp)
        {
            float a = 1;
            while (pet != null)
            {
                if (hero == null) { Destroy(pet.gameObject); yield break; }
                pet.transform.localPosition = PetPos(hero, sp);
                bool show = hero.gameObject.activeInHierarchy;   // (10-18 원작 대조) 고학년 동안에도 왼쪽 제자리에 보인다(원작 영상 0.0~2.4 내내) — 전엔 그 사도 고학년 중 숨겼다
                a = Mathf.MoveTowards(a, show ? 1 : 0, Time.deltaTime / 0.2f);
                pet.SetAlpha(a);
                pet.SetOrder(Mathf.Max(1, hero.BaseOrder - 1));
                yield return null;
            }
        }

        // hitAt = 첫 타격(화면 초, 몸짓 시작에서). 쏘는 몸짓이면 소환수 이벤트(1배속 초)가 투사체 떠나는 때(hitAt − 비행)에 오게 앞을 건너뛴다
        void PetAct(UnitView hero, Motion m, float speed, float hitAt)
        {
            if (hero == null || !pets.TryGetValue(hero, out var pet) || pet == null) return;
            if (!PETS.TryGetValue(hero.FxKey ?? "", out var sp) || !sp.Anim.TryGetValue(m, out var anim) || !pet.Has(anim)) return;
            pet.Play(anim, speed);
            if (sp.Shoots == null || !sp.Shoots.Contains(m)) return;
            var a = pet.SkelData.FindAnimation(anim);
            float ev = -1;
            if (a != null) foreach (var tl in a.Timelines) if (tl is Spine.EventTimeline et) foreach (var e in et.Events) if (e.Data.Name == "Event" && e.Time > 0.05f) { ev = e.Time; break; }
            float launch = Mathf.Max(0, hitAt - Bolzena.Fx.UltFx.PROJ_LAG / 1000f);
            var cur = pet.Sa.AnimationState.GetCurrent(0);
            if (ev > 0 && cur != null) cur.TrackTime = Mathf.Max(0, ev - launch * speed);
        }

        // 쏘는 자리 — 소환수 입(Point_Attack1 · 없으면 몸 앞). 이 몸짓에 쏘는 소환수가 없으면 null
        System.Func<Vector3?> PetMouth(UnitView hero, Motion m)
        {
            if (hero == null || !pets.TryGetValue(hero, out var pet) || pet == null) return null;
            if (!PETS.TryGetValue(hero.FxKey ?? "", out var sp) || sp.Shoots == null || !sp.Shoots.Contains(m)) return null;
            return () => pet != null ? (pet.Fx.Bone("Attack1") ?? pet.Fx.At(Bolzena.Fx.FxAnchor.Front)) : (Vector3?)null;
        }

        // 고학년 소환수 — 갈래 스킨으로 시전자 앞에 띄워, 던지는 이벤트(0.9초)가 맞음마다(hitsMs, 몸짓 ms) 오게 몸짓을 되풀이하고 끝나면 걷는다
        //   (에피카 갈래별 에피콘: 매드맥스 · 해적 · 간달프, 10-17 사용자 · fix_Y 3b — 전엔 한 번 던지고 말았다)
        IEnumerator UltPetCo(UnitView hero, string spine, string skin, string anim, int[] hitsMs, float speed)
        {
            Debug.Log($"[UltPet] {spine} {skin} {anim} 박자 {(hitsMs != null ? string.Join(",", hitsMs) : "없음")} — 스파인 {(Res.Spine(spine) != null ? "있음" : "없음")}");
            if (hero == null || Res.Spine(spine) == null || hitsMs == null || hitsMs.Length == 0) yield break;
            const float EV = 0.9f;                                              // epicon_ult Attack1_N 이벤트(원작 스파인)
            float T(int ms) => Rm(ms / 1000f) / Mathf.Max(0.01f, speed);      // 몸짓 ms → 화면 초
            float t0 = Time.time;
            var made = new List<UnitView>();
            for (int i = 0; i < hitsMs.Length; i += 2)   // (10-18 사용자 4차 「너무 많이 · 빠르게」) 박자 둘에 하나 — 9 → 5마리
            {
                int j = i / 2;
                float start = T(hitsMs[i]) - EV / Mathf.Max(0.01f, speed);    // 던지는 이벤트가 그 맞음 박자에
                while (Time.time - t0 < start) { if (hero == null || !InUlt) { foreach (var q in made) if (q) Destroy(q.gameObject); yield break; } yield return null; }
                // (10-18 사용자 2차 「앞에 한 마리만 · 빨리 감김」) 원작은 박자마다 작은 에피콘이 하나씩 나와 떼로 달린다 — 한 마리를 3배속까지 되감던 것(전엔 Spawn1_1 한 마리 + 박자마다 1~3배속 다시 틀기)
                //   자리 흩뿌림(앞 1.2 + 0.25·(i%3), 아래 0.15·(i%2))은 원작 떼 모양을 흉내 낸 값. 사도 실제 자리 앞(곁 소환수 PetPos 와 같은 좌표)
                // (10-18 3차 「떼가 안 보임」) 코드는 탔다(motionlab_shots.log 388 「[UltPet] epicon_ult Normal3 …」) — 순서가 hero.BaseOrder + 1(평소 줄 순서)이라 고학년 중 시전자(330 · 배 · 차) · 고학년 장(325~335 · 파도) · 적(316~320) 뒤에 깔렸다.
                //   원작 영상(에피카 고학년 1.5 · 2.5 · 3.0 · 5.0초)은 떼가 배 · 차 앞, 시전자 발보다 아래(화면 앞쪽)에 그려진다 → 시전자 앞 장(335) 위 340, 발에서 키의 0.3배 아래
                // (10-18 사용자 4차 「한곳에 몰림 — 분포」) 원작 1.4~2.0초: 차 앞바퀴 앞 한 줄에 세로로 층층이(발 높이 · 그 아래 …) — 가로는 조금만, 세로를 키의 0.3배씩(값은 영상 눈대중)
                float h = hero.Fx.Height;
                var pos = hero.transform.localPosition + new Vector3((hero.Facing ? 1 : -1) * (0.9f + 0.2f * (j % 2)), -(0.05f + 0.3f * (j % 4)) * h, 0);
                var p = UnitView.Create(FieldRoot, "ultpet_" + spine + j, spine, skin, hero.Facing, UnitScale, pos, 340 + j);   // 아래층이 앞에 그려지게
                if (p.Has(anim)) p.Play(anim, speed, false);
                made.Add(p);
                StartCoroutine(DestroyAfter(p, Mathf.Min(p.Duration(anim) / Mathf.Max(0.01f, speed) + 0.1f, 1.0f)));   // 원작 한 칸에 3~5마리 — 1초 넘게 남기지 않는다
            }
        }
        IEnumerator DestroyAfter(UnitView p, float s) { yield return Clock.Wait(s); if (p) Destroy(p.gameObject); }

        // 꽂아 두는 소환물 — 카드 몸짓 도중 앞쪽 땅에 세우고 그 뒤로 서 있는 것(네르(빡침) 저학년 깃발 nerrageflag: 원작 영상 1.2초에 꽂혀 끝까지 선다).
        //   다시 쓰면 앞의 것을 걷고 새로 꽂는다. Fwd = 사도 발에서 앞으로(그림 높이 배)
        class DropSpec { public string Spine, Spawn; public int AtMs; public float Fwd; }
        static readonly Dictionary<string, DropSpec> DROPS = new Dictionary<string, DropSpec>
        {
            [MKey("네르_빡침", Motion.Skill1)] = new DropSpec { Spine = "nerrageflag", Spawn = "Spawn", AtMs = 1100, Fwd = Bolzena.Fx.UltFx.NER_FLAG_FWD },   // Skill1_1 첫 이벤트 1100 · 앞 거리는 스킬 장(UltFx NER_FLAG — 빛기둥 · 균열)과 한 값(10-18 4차)
        };
        readonly Dictionary<UnitView, UnitView> drops = new Dictionary<UnitView, UnitView>();

        void DropAct(UnitView hero, Motion m, float speed)
        {
            if (hero == null) return;
            // 다른 동작을 하면 앞서 꽂은 것을 걷는다(10-17 사용자 「네르 평타 · 강공에 스킬 깃발 남아 있음」)
            if (!DROPS.TryGetValue(MKey(hero.FxKey ?? "", m), out var sp) || Res.Spine(sp.Spine) == null)
            {
                if (drops.TryGetValue(hero, out var prev) && prev != null)
                {
                    // (10-18 2차 사용자 「깃발이 끝에 사라지는 거지?」) 원작 깃발 스파인의 Die 를 틀고 걷는다 — 그 자리에서 바로 지우던 것(걷는 때는 그대로)
                    if (prev.Has("Die")) { float dd = prev.Play("Die", 1f, false); Destroy(prev.gameObject, Mathf.Max(0.1f, dd)); }
                    else Destroy(prev.gameObject);
                }
                drops.Remove(hero);
                return;
            }
            StartCoroutine(DropCo(hero, sp, speed));
        }

        IEnumerator DropCo(UnitView hero, DropSpec sp, float speed)
        {
            yield return Clock.Wait(sp.AtMs / 1000f / Mathf.Max(0.1f, speed));
            if (hero == null) yield break;
            if (drops.TryGetValue(hero, out var old) && old != null) Destroy(old.gameObject);
            var pos = hero.Fx.Feet + new Vector3((hero.Facing ? 1 : -1) * sp.Fwd * hero.Fx.Height, 0, 0);   // (10-18 4차) 스킬 장(UltFx NER_FLAG)과 같은 셈(발 = UnitView.Feet) — 깃발 밑에 균열 · 빛기둥
            var d = UnitView.Create(FieldRoot, "drop_" + sp.Spine, sp.Spine, "Normal", hero.Facing, UnitScale, pos, Mathf.Max(1, hero.BaseOrder - 3));
            if (d.Has(sp.Spawn)) d.Play(sp.Spawn, speed);
            drops[hero] = d;
        }
    }
}
