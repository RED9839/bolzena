# 볼제나 원본 에셋에서 시범에 쓰는 것만 유니티 프로젝트로 복사한다(원본은 읽기만).
# python Tools/copy_assets.py
import json, os, shutil
from PIL import Image

SRC = r"C:\projects\볼제나\assets"
DST = r"C:\projects\bolzena-unity\Assets\Bolzena\Resources"

def cp_audio(src, dst):
    # 원작 소리는 Ogg Opus 라 유니티가 못 읽는다 — 풀어서 WAV 로(soundfile = libsndfile)
    import soundfile as sf
    dst = os.path.splitext(dst)[0] + ".wav"
    if os.path.exists(dst):
        return
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    data, sr = sf.read(src)
    sf.write(dst, data, sr, subtype="PCM_16")


def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if not os.path.exists(dst):
        shutil.copyfile(src, dst)

# 스파인 — .skel → .skel.bytes, .atlas → .atlas.txt(spine-unity 가 알아보는 이름).
# 원작 그림은 PMA(색에 알파를 미리 곱함)인데 이 프로젝트는 Linear 색 공간이라 그대로 쓰면 색이 바랜다 —
# 알파를 나눠 곧은 알파로 바꿔 넣고, 재질은 Straight Alpha 로 쓴다(ProjectSetup.SpineMaterials).
import numpy as np
SPINE = {"ingame/티그": "tig", "ingame/리츠": "leets", "ingame/이드": "ed",
         "standing/티그": "st_tig", "standing/리츠": "st_leets", "standing/이드": "st_ed",
         "ingame/리코타": "ricota", "ingame/캬롯": "kyarot", "ingame/시온더다크불릿": "xxionx",
         "standing/리코타": "st_ricota", "standing/캬롯": "st_kyarot", "standing/시온더다크불릿": "st_xxionx",
         "enemy/fairymobcloserange": "fairymobcloserange", "enemy/fairymoblongrange": "fairymoblongrange", "enemy/curburus": "curburus"}
for src, name in SPINE.items():
    sd = f"{SRC}/spine/{src}"
    dd = f"{DST}/Spine/{name}"
    os.makedirs(dd, exist_ok=True)
    for f in os.listdir(sd):
        if f.endswith(".skel"):
            cp(f"{sd}/{f}", f"{dd}/{f}.bytes")
        elif f.endswith(".atlas"):
            cp(f"{sd}/{f}", f"{dd}/{f}.txt")
        elif f.endswith(".png"):
            im = np.asarray(Image.open(f"{sd}/{f}").convert("RGBA")).astype(np.float32)
            a = im[..., 3:4]
            rgb = np.where(a > 0, im[..., :3] * 255.0 / np.maximum(a, 1), 0)
            out = np.concatenate([np.clip(rgb, 0, 255), a], axis=-1).astype(np.uint8)
            Image.fromarray(out, "RGBA").save(f"{dd}/{f}")

# 배경
for b in ["stage3_2", "stage3_3"]:
    cp(f"{SRC}/bg/{b}.jpg", f"{DST}/Bg/{b}.jpg")

# 효과음 — 키 이름으로
SFX = {
    "hit_slash": "monster/elfsoldiercloserange/elfsoldiercloserange_basicattack_hit",
    "hit_blunt": "scenario/sfx_punch",
    "hit_magic": "hero/naia/naia_basicattack_hit",
    "hit_crit": "scenario/sfx_greatswordslash",
    "hit_heavy": "monster/golem/golem_basicattack_hit",
    "hit_small": "monster/curseddoll/curseddoll_skillhit",
    "hurt": "monster/fairymobcloserange/fairymobcloserange_basicattack_hit",
    "block_gain": "sfx_urosminigame_crayonknightshield",
    "block_hit": "scenario/sfx_hitmetal",
    "break": "sfx_velaminigame_breakbarrier",
    "buff": "sfx_ingame_growup",
    "debuff": "sfx_ingame_growdown",
    "death_enemy": "sfx_urosminigame_disappearunit",
    "ult_ready": "sfx_ingame_maxgrowup",
    "ult_cutin": "scenario/sfx_ingame_callhero_start",
    "ult_impact": "scenario/sfx_shortexplosion",
    "turn_start": "sfx_common_notification",
    "turn_end": "sfx_lobby_startbattlebuttondown",
    "battle_start": "sfx_lobby_startbattlebuttondown",
    "boss_entry": "sfx_renewabossentry",
    "victory": "sfx_victory",
    "card_play": "sfx_cardgacha_cardflying",
    "card_draw": "sfx_cardpack_opening",
    "card_hover": "sfx_deck_changeskillinfo",
    "card_swing": "scenario/sfx_swing",
    "card_skill": "sfx_ingame_manausing",
    "card_cant": "sfx_deck_wrongslot",
    "epiphany": "sfx_card_levelup",
    "epiphany_open": "sfx_common_opencontents",
    "ui_click": "sfx_common_buttontouch",
}
for k, v in SFX.items():
    cp_audio(f"{SRC}/sfx/{v}.ogg", f"{DST}/Sfx/{k}.ogg")
HEROES = ["tig", "leets", "ed", "ricota", "kyarot", "xxionx"]   # 시범 셋 + 코어 샘플 셋(rico · carrot · sion)
for d in HEROES:
    for f in os.listdir(f"{SRC}/sfx/hero/{d}"):
        cp_audio(f"{SRC}/sfx/hero/{d}/{f}", f"{DST}/Sfx/hero/{d}/{f}")
for d in ["curburus", "fairymobcloserange", "fairymoblongrange"]:
    for f in os.listdir(f"{SRC}/sfx/monster/{d}"):
        cp_audio(f"{SRC}/sfx/monster/{d}/{f}", f"{DST}/Sfx/monster/{d}/{f}")

# 목소리 — 쓰는 갈래만
import re
VO = re.compile(r"^(ultimate\d|ultimate\d_\d|ultimate\d_\d_\d|powerattack\d|powerattack\d_\d|spskill\d|spskill\d_\d|victory\d|hit\d|spawn\d|shout\d|basicattack\d_\d_\d)\.ogg$")
for d in HEROES:
    for f in os.listdir(f"{SRC}/voice/{d}"):
        if VO.match(f):
            cp_audio(f"{SRC}/voice/{d}/{f}", f"{DST}/Voice/{d}/{f}")

# 카드 그림 · 초상 — 사도마다 스킬 아이콘 다섯 · 얼굴 하나
for d in HEROES:
    cp(f"{SRC}/heroicons/{d}.png", f"{DST}/Art/{d}.png")
    for n in [f"icon_admissionskill_{d}", f"icon_graduateskill_{d}", f"aside_skill_{d}_1", f"aside_skill_{d}_2", f"aside_skill_{d}_3"]:
        if os.path.exists(f"{SRC}/skillicons/{n}.png"):
            cp(f"{SRC}/skillicons/{n}.png", f"{DST}/Art/{n}.png")

# 이펙트 그림 — 공용 텍스처
TEX = """FX_IN_Glow FX_IN_Spark FX_IN_Spark_01 FX_IN_Flare_Star_01 FX_IN_Ring_ShockWave_01 FX_IN_RIng_Hit_wave_01 FX_IN_Fragment_01
FX_IN_Fragment_02 FX_TEX_Hit_02_lum FX_TEX_Slash_04_lum FX_Tig_Slash08_lum FX_Slash_Seq_Basic FX_Flarespark_Seq FX_Seq_Punch_Smoke
FX_IN_Smoke_01 FX_UI_Light_line_Cut_01 FX_IN_Screen_Ray_Gradient FX_UI_CardBorder_Glow FX_UI_star_02 FX_UI_glow_01 FX_IN_Flare_Ring_01
FX_IN_Crack_Round_Glow FX_IN_Ground_Crack_01 FX_IN_Sliced_Piece_Particle_Gray FX_IN_Particle_Star_01 FX_IN_Star_4_1 FX_UI_light_line_Blur
FX_IN_Flare_Line_01 FX_IN_Ring_Spark_01 FX_IN_Ring_Impact_wave_01 FX_Seq_Shout_01 FX_IN_Ring_ShockWave_03 FX_UI_Flare_Line01_new
FX_TEX_Shock_Wave_05_lum FX_Tig_Dash01_lum FX_IN_Crack_Round_back FX_IN_Ring_ShockWave_02""".split()
for t in TEX:
    cp(f"{SRC}/fx/_shared/{t}.png", f"{DST}/Fx/Tex/{t}.png")

# 구운 타격 이펙트 — webp 를 png 로, 정보는 sheets.json 으로
idx = json.load(open(f"{SRC}/fx-baked/index.json", encoding="utf-8"))["effects"]
out = []
for name, e in idx.items():
    if not (name.startswith("fx_common") or name in ("fx_mago_hit_1", "fx_erpin_ultimate_explosion_1")):
        continue
    if len(e["pages"]) != 1:
        continue
    dst = f"{DST}/Fx/Sheets/{name}.png"
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if not os.path.exists(dst):
        Image.open(f"{SRC}/fx-baked/{e['pages'][0]['file']}").convert("RGBA").save(dst)
    out.append({"name": name, "fps": e["fps"], "frames": e["frames"], "w": e["w"], "h": e["h"], "cols": e["cols"],
                "ax": e["anchor"][0], "ay": e["anchor"][1], "ppu": e["ppu"]})
json.dump({"sheets": out}, open(f"{DST}/Fx/sheets.json", "w", encoding="utf-8"), indent=1)
print("ok", len(out), "sheets")
