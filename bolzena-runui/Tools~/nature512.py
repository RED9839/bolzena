# 성격 아이콘 512 판 — 원작 큰 판(raw_common/Common_UnitPersonality_*_Raw_512)은 흰 글리프뿐이라,
# 지금 128 보석(uiicons/성격_*.png — 원작 commonicons)을 4배로 늘려 옛 글리프 자리를 둘레 색으로 메우고 바깥 테를 또렷하게 한 뒤
# 그 자리에 512 글리프를 얹는다(4K 에서도 선명하게 — 판 · 전투 · 도감 · 약점 아이콘이 다 RunArt/Icons/성격_* 를 쓴다).
#   python "Tools~/nature512.py" <출력 폴더>   (copy_assets.py 가 부른다)
import numpy as np, sys, os
from PIL import Image, ImageFilter
NAT={"순수":"Naive","광기":"Mad","냉정":"Cool","우울":"Gloomy","활발":"Jolly","공명":"Resonance"}
ASSETS = os.path.join("C:\\", "projects", "볼제나", "assets")


def make(ko, out):
    g=np.asarray(Image.open(f"{ASSETS}/uiicons/성격_{ko}.png").convert("RGBA")).astype(np.float32)/255
    rgb,a=g[...,:3],g[...,3]
    mx,mn=rgb.max(-1),rgb.min(-1)
    white=(mn>0.72)&(mx-mn<0.25)&(a>0.9)      # 옛 글리프(희게 비친 자리)
    m=Image.fromarray((white*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))
    bb=Image.fromarray((white*255).astype(np.uint8)).getbbox()
    big=Image.fromarray((g*255).astype(np.uint8)).resize((512,512),Image.LANCZOS)
    B=np.asarray(big).astype(np.float32)/255
    M=np.asarray(m.resize((512,512),Image.BILINEAR)).astype(np.float32)/255
    # 글리프 자리를 둘레 색으로 메운다(정규화 흐림)
    w=(1-M)*B[...,3]
    from scipy.ndimage import gaussian_filter
    num=np.stack([gaussian_filter(B[...,c]*w,24) for c in range(3)],-1)
    den=gaussian_filter(w,24)[...,None]
    fill=num/np.maximum(den,1e-6)
    rgbB=B[...,:3]*(1-M[...,None])+fill*M[...,None]
    aB=B[...,3]
    # 바깥 테를 또렷하게(늘린 알파를 가파르게) — 그림자(어두운 반투명)는 그대로
    dark=rgbB.max(-1)<0.25
    aS=np.where(dark,aB,np.clip((aB-0.5)*2.2+0.5,0,1))
    base=np.dstack([rgbB,aS])
    gl=Image.open(f"{ASSETS}/iconsrc/raw_common/Common_UnitPersonality_{NAT[ko]}_Raw_512.png").convert("RGBA")
    gb=gl.getbbox(); gl=gl.crop(gb)
    # 옛 글리프 자리에 맞춘다(128 bbox ×4)
    x0,y0,x1,y1=[v*4 for v in bb]
    gl=gl.resize((x1-x0,y1-y0),Image.LANCZOS)
    G=np.zeros((512,512,4),np.float32); G[y0:y1,x0:x1]=np.asarray(gl).astype(np.float32)/255
    ga=G[...,3]*0.9
    col=np.clip(np.array([1,1,1])*0.94+base[...,:3]*0.06,0,1)
    outc=base[...,:3]*(1-ga[...,None])+col*ga[...,None]
    Image.fromarray((np.dstack([outc,base[...,3]])*255).astype(np.uint8)).save(out)
    return bb
if __name__ == "__main__":
    for ko in NAT:
        make(ko, os.path.join(sys.argv[1], f"성격_{ko}.png"))
