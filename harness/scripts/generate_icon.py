"""Rasterize the repo-native microphone/pet geometry to Windows ICO sizes."""
from pathlib import Path
from PIL import Image, ImageDraw
root=Path(__file__).resolve().parents[2]/'src/VoxPet.App/Assets'
root.mkdir(exist_ok=True)
svg='''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><rect x="8" y="8" width="240" height="240" rx="64" fill="#111927"/><path d="M75 63L83 23L104 51M151 51L173 23L181 63" fill="#B8A0EA"/><rect x="101" y="48" width="54" height="90" rx="27" fill="#66E3C4"/><path d="M75 105V127a53 53 0 00106 0V105M128 180V204M96 204H160" fill="none" stroke="#66E3C4" stroke-width="14" stroke-linecap="round"/></svg>'''
(root/'VoxPet.svg').write_text(svg+'\n')
s=4;im=Image.new('RGBA',(256*s,256*s));d=ImageDraw.Draw(im)
def box(values):return tuple(round(v*s) for v in values)
d.rounded_rectangle(box((8,8,248,248)),radius=64*s,fill='#111927')
for points in [[(75,63),(83,23),(104,51)],[(151,51),(173,23),(181,63)]]:d.polygon([(x*s,y*s) for x,y in points],fill='#B8A0EA')
d.rounded_rectangle(box((101,48,155,138)),radius=27*s,fill='#66E3C4')
d.arc(box((75,74,181,180)),0,180,fill='#66E3C4',width=14*s)
for start,end in [((75,105),(75,127)),((181,105),(181,127)),((128,180),(128,204)),((96,204),(160,204))]:
 d.line([tuple(v*s for v in start),tuple(v*s for v in end)],fill='#66E3C4',width=14*s)
 for x,y in [start,end]:d.ellipse(box((x-7,y-7,x+7,y+7)),fill='#66E3C4')
im=im.resize((256,256),Image.Resampling.LANCZOS);im.save(root/'VoxPet.png');im.save(root/'VoxPet.ico',sizes=[(n,n) for n in [16,24,32,48,64,128,256]])
print('Generated native SVG/PNG/7-size ICO')
