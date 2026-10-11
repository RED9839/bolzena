import re,sys
cur=None;over=0;L=[]
for l in open(sys.argv[1],encoding='utf-8'):
    l=l.rstrip('\n')
    m=re.match(r'\[(장비|교주)\] (.+?) \(',l)
    if m: cur=m.group(2); kind=m.group(1); continue
    if kind=='교주':
        m=re.match(r'  (기본|신탁\d) ?(「[^」]*」)? ?(.*?)(  \(값 [\d.]+\))?$',l)
        if m and m.group(1):
            s=m.group(3); power='강화 카드.' in s or '유일.' in s and '매 턴' in s or '이 전투 동안' in s or '매 턴' in s
            s2=re.sub(r'^강화 카드\.\s*','',s); n=len(re.sub(r'\s+','',s2)); cap=55 if power else 45
            L.append(n)
            if n>cap or '-a' in sys.argv: over+= n>cap; print(('넘침 ' if n>cap else '     ')+f'{cur} {m.group(1)} {n}/{cap} {s}')
    else:
        if l.startswith('  ') and not l.startswith('  「'):
            for part in l.strip().split(' · '):
                if ':' in part:
                    n=len(re.sub(r'\s+','',part))
                    if n>50 or '-a' in sys.argv: over+=n>50; print(('넘침 ' if n>50 else '     ')+f'{cur} {n}/50 {part}')
L.sort(); print('글',len(L),'중앙',L[len(L)//2],'최대',L[-1],'넘침',over)
