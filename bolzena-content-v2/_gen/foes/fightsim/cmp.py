import sys,csv
def load(p):
  r={}
  for x in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
    if x['kind']=='event': continue
    r[(x['floor'],x['kind'],x['line'])]=x
  return r
a=load(sys.argv[1]); b=load(sys.argv[2]); keys=sorted(set(a)|set(b))
for k in keys:
  x=a.get(k); y=b.get(k)
  f=lambda z,c: z[c] if z else '-'
  d = f'{float(y["losePct"])-float(x["losePct"]):+.1f}' if x and y else ''
  print(f'{k[0]} {k[1]:5} {k[2][:62]:62} n {f(x,"n"):>5}/{f(y,"n"):>5}  패배 {f(x,"losePct"):>5}→{f(y,"losePct"):>5} {d:>6}  HP손실 {f(x,"hpLostPct"):>5}→{f(y,"hpLostPct"):>5}  턴 {f(x,"turns")}→{f(y,"turns")}')
