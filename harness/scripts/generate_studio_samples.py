"""Generate CC0 procedural costume samples and a native .voxpet test character.

Development only: Pillow. The published app reads the generated files without Python.
"""
import io
import json
import math
from pathlib import Path
import zipfile
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'src/VoxPet.App/Assets/Samples'
OUT.mkdir(parents=True, exist_ok=True)
BASE = ROOT / 'src/VoxPet.App/Assets/ImageSetTemplate'
INK = '#243c55'
CATEGORIES = [
 ('hat','모자 · 헬리콥터 모자','head',310), ('glasses','안경 · 둥근 안경','head',320),
 ('faceDecoration','얼굴 장식 · 미소 볼터치','mouth',330), ('earrings','귀걸이 · 별 귀걸이','head',315),
 ('top','상의 · 민트 스웨터','body',70), ('bottom','하의 · 남색 반바지','body',75),
 ('onePiece','한벌옷 · 줄무늬 멜빵','body',80), ('shoes','신발 · 노란 운동화','body',85),
 ('outerwear','겉옷 · 코랄 재킷','body',90), ('cape','망토 · 펄럭이는 날개','body',-20),
 ('headAccessory','머리 장식 · 리본','head',340), ('backAccessory','등 장식 · 작은 날개','body',-10),
 ('handAccessory','손 장식 · 손목 밴드','rightHand',350), ('effect','효과 · 반짝임','root',400)]

HAIRSTYLES = [('hair-short','헤어 · 단발머리'), ('hair-long','헤어 · 장발머리'), ('hair-afro','헤어 · 폭탄머리')]

def png(kind, frame=0):
 im = Image.new('RGBA',(1024,1024)); d=ImageDraw.Draw(im)
 def box(rect, color, radius=10):
  d.rounded_rectangle(tuple(int(x*2) for x in rect),radius=radius*2,fill=color,outline=INK,width=4)
 def ellipse(rect,color): d.ellipse(tuple(int(x*2) for x in rect),fill=color,outline=INK,width=4)
 def polygon(points,color): d.polygon([(int(x*2),int(y*2)) for x,y in points],fill=color,outline=INK,width=4)
 def line(points,color,width=4):d.line([(int(x*2),int(y*2)) for x,y in points],fill=color,width=width*2,joint='curve')
 def star(x,y,size,color):
  import math
  polygon([(x+math.sin(i*math.pi/5)*(size if i%2==0 else size*.45),y-math.cos(i*math.pi/5)*(size if i%2==0 else size*.45)) for i in range(10)],color)
 if kind=='hat':
  ellipse((164,54,348,104),'#ffca69');box((154,91,358,106),'#ec885f',7)
  line([(256,57),(256,24)],INK,6)
  for i,color in enumerate(['#f0748f','#66cddd','#8d9dec','#9dde91']):
   angle=(frame/8+i/4)*math.tau
   ux,uy=math.cos(angle),math.sin(angle)*.23;vx,vy=-math.sin(angle),math.cos(angle)*.23
   polygon([(256+ux*13+vx*8,24+uy*13+vy*8),(256+ux*100+vx*8,24+uy*100+vy*8),(256+ux*100-vx*8,24+uy*100-vy*8),(256+ux*13-vx*8,24+uy*13-vy*8)],color)
  ellipse((247,18,265,30),'#fdf4db')
 elif kind=='glasses':
  ellipse((185,105,243,148),'#c1edff88');ellipse((270,105,328,148),'#c1edff88');line([(243,121),(270,121)],INK);line([(168,113),(185,120)],INK);line([(328,120),(347,113)],INK)
 elif kind=='faceDecoration':ellipse((176,152,215,172),'#ff969bb8');ellipse((299,152,338,172),'#ff969bb8')
 elif kind=='earrings':
  for x in [159,353]:line([(x,145),(x,173)],'#ffd76b',3);star(x,180,12,'#ffd76b')
 elif kind in ['top','outerwear','onePiece']:
  color={'top':'#58c8ae','outerwear':'#ef8a78','onePiece':'#9276ce'}[kind]
  polygon([(181,225),(150,249),(137,302),(177,313),(188,279),(186,382),(326,382),(324,279),(335,313),(375,302),(362,249),(331,225),(294,239),(218,239)],color)
  box((233,279,278,319),'#e7fff5',7)
  if kind=='outerwear':line([(256,239),(256,382)],'#fff3de',8)
  if kind=='onePiece':box((191,368,245,440),color);box((268,368,321,440),color);line([(202,256),(310,256)],'#fff3de',8)
 elif kind=='bottom':box((182,352,330,405),'#3d5c91');box((185,385,244,440),'#3d5c91');box((269,385,327,440),'#3d5c91');line([(188,364),(324,364)],'#ffcf6d',6)
 elif kind=='shoes':
  for x in [184,265]:box((x,435,x+65,476),'#ffce68');line([(x+8,458),(x+57,458)],'#fff7dc',6)
 elif kind=='cape':
  angle=math.radians([-30,-10,20,45,20,-10][frame%6])
  def wing(points, mirror=False):
   mapped=[(202+x*math.cos(angle)-y*math.sin(angle),260+x*math.sin(angle)+y*math.cos(angle)) for x,y in points]
   return [(512-x,y) for x,y in mapped] if mirror else mapped
  outline=[(0,0),(-40,-28),(-120,-100),(-150,-95),(-135,-60),(-110,-28),(-145,-28),(-128,10),(-93,35),(-113,44),(-82,68),(-30,65),(0,28)]
  for mirror in [False,True]:
   polygon(wing(outline,mirror),'#e2dcfc')
   line(wing([(-8,17),(-70,-5),(-128,-69)],mirror),'#a0a9d8',3)
   line(wing([(-30,30),(-79,28),(-124,-8)],mirror),'#a0a9d8',3)
 elif kind=='headAccessory':polygon([(257,68),(217,34),(204,92)],'#f38eb5');polygon([(257,68),(299,34),(313,92)],'#f38eb5');ellipse((245,56,271,82),'#ffd76b')
 elif kind=='backAccessory':
  for pts in [[(205,263),(91,221),(115,331),(205,350)],[(307,263),(421,221),(397,331),(307,350)]]:polygon(pts,'#c9e9f4')
 elif kind=='handAccessory':box((332,321,388,344),'#f7bc5a');star(360,333,12,'#fff0be')
 elif kind=='effect':
  for x,y,s in [(115,160,19),(391,194,24),(119,392,15),(394,414,18)]:star(x,y,s,'#ffd45e')
 elif kind.startswith('hair-'):
  fringe=kind.endswith('-fringe');style=kind.removesuffix('-fringe')
  color={'hair-short':'#503d51','hair-long':'#854b43','hair-afro':'#4b3543'}[style]
  if fringe:
   if style=='hair-afro':
    for i in range(11):
     x=164+i*18;y=79+10*math.sin(i*.7);ellipse((x-18,y-19,x+18,y+19),color)
   else:
    polygon([(156,95),(169,61),(214,39),(294,42),(344,70),(355,103),(319,91),(300,72),(277,107),(255,82),(225,109),(206,81),(181,103)],color)
    line([(184,72),(214,61),(252,62)],'#bb858c',3)
  elif style=='hair-short':
   ellipse((139,35,373,258),color);box((139,114,194,267),color,18);box((318,114,373,267),color,18)
  elif style=='hair-long':
   box((137,39,375,425),color,82)
   for x in [149,345]:line([(x,161),(x-8,251),(x+4,338),(x-2,405)],'#b57560',5)
  else:
   ellipse((108,34,404,260),color)
   for i in range(18):
    a=i*math.tau/18;x=256+128*math.cos(a);y=139+91*math.sin(a);ellipse((x-28,y-28,x+28,y+28),color)
 elif kind=='heart':
  polygon([(256,240),(210,198),(212,170),(236,161),(256,180),(276,161),(300,170),(302,198)],'#ff83ad')
 elif kind=='toy-gun':box((345,275,428,299),'#59b6ec');box((348,293,375,333),'#5795d1');box((406,278,438,293),'#ffcc71',3)
 elif kind=='toy-sword':polygon([(352,325),(343,314),(400,219),(414,211),(412,230)],'#9fe5f0');line([(333,305),(374,329)],'#ffcf68',7)
 elif kind=='magic-wand':line([(354,331),(399,239)],'#8468c5',10);star(402,232,27,'#ffd968')
 elif kind=='claw':
  for x in [363,382,401]:line([(x,251),(x-18,292),(x+1,305)],'#fff2b4',7)
 elif kind=='bow':
  d.arc((350*2,210*2,432*2,350*2),270,90,fill='#eab36b',width=12);line([(391,210),(391,350)],'#f6ecd4',3);line([(349,280),(440,280)],'#c5f2fb',5);polygon([(438,280),(426,272),(426,288)],'#c5f2fb')
 else:raise ValueError(kind)
 im=im.resize((512,512),Image.Resampling.LANCZOS);b=io.BytesIO();im.save(b,format='PNG');return b.getvalue()

def build():
 files={}
 for name in ['head','body','arm-l','arm-r','leg-l','leg-r','mouth-0','mouth-1','mouth-2','eyes-open','eyes-closed','eyes-happy']:
  files['images/'+name+'.png']=(BASE/(name+'.png')).read_bytes()
 for cat,_,_,_ in CATEGORIES:files['samples/'+cat+'.png']=png(cat)
 for kind,total in [('hat',8),('cape',6)]:
  for frame in range(1,total):files[f'samples/{kind}-{frame}.png']=png(kind,frame)
 for id,_ in HAIRSTYLES:
  files['samples/'+id+'.png']=png(id);files['samples/'+id+'-fringe.png']=png(id+'-fringe')
 for effect in ['heart','toy-gun','toy-sword','magic-wand','claw','bow']:files['samples/'+effect+'.png']=png(effect)
 anchors={'root':{},'body':{'x':256,'y':320,'parent':'root'},'head':{'x':0,'y':-175,'parent':'body'},'face':{'parent':'head'},'eyes':{'parent':'face'},'mouth':{'x':0,'y':25,'parent':'face'},'leftShoulder':{'x':-92,'y':-72,'parent':'body'},'rightShoulder':{'x':92,'y':-72,'parent':'body'},'leftHand':{'x':-16,'y':85,'parent':'leftShoulder'},'rightHand':{'x':16,'y':85,'parent':'rightShoulder'},'hips':{'x':0,'y':70,'parent':'body'},'back':{'parent':'body'},'leftFoot':{'x':-40,'y':75,'parent':'hips'},'rightFoot':{'x':40,'y':75,'parent':'hips'}}
 parts=[{'id':n,'type':t,'file':'images/'+f+'.png','anchor':a,'zIndex':z} for n,t,f,a,z in [('leg-left','leg','leg-l','body',10),('leg-right','leg','leg-r','body',11),('body','body','body','body',20),('arm-left','arm','arm-l','leftShoulder',30),('arm-right','arm','arm-r','rightShoulder',31),('head','head','head','head',100)]]
 character={'schemaVersion':2,'canvas':{'width':512,'height':512},'parts':parts,'anchors':anchors,'mouth':{'frames':['images/mouth-'+str(i)+'.png' for i in range(3)],'anchor':'mouth','curve':'legacy3'},'expressions':[{'id':k,'name':n,'eyesOpen':'images/'+o+'.png','eyesClosed':'images/eyes-closed.png'} for k,n,o in [('neutral','평상','eyes-open'),('happy','행복','eyes-happy')]],'face':{'requiredVisiblePart':'head'},'motion':{'idle':{'breathing':.4,'sway':.15},'voice':{'bodyBounce':4,'headBounce':1,'headRotation':1}}}
 attachments=[]
 for cat,label,anchor,z in CATEGORIES:
  path='attachments/'+cat+'.json';attachments.append(path)
  files[path]={'schemaVersion':2,'id':cat,'name':label,'category':cat,'layers':[{'id':'image','file':'samples/'+cat+'.png','anchor':anchor,'zIndex':z,'deformWithRig':cat in ['top','outerwear','onePiece'],'inherit':{'mouthMotion':cat=='faceDecoration'}}]}
  if cat in ['hat','cape']:
   total=8 if cat=='hat' else 6
   files[path]['layers'][0]['animation']={'frames':['samples/'+cat+'.png']+[f'samples/{cat}-{i}.png' for i in range(1,total)],'frameDurationMs':80 if cat=='hat' else 140}
 for id,label in HAIRSTYLES:
  path='attachments/'+id+'.json';attachments.append(path)
  files[path]={'schemaVersion':2,'id':id,'name':label,'category':'headAccessory','exclusiveGroup':'hairstyle','layers':[{'id':'back','file':'samples/'+id+'.png','anchor':'head','zIndex':-5},{'id':'fringe','file':'samples/'+id+'-fringe.png','anchor':'head','zIndex':295}]}
 specs=[('claw','법규','Gesture',-28,35),('toy-gun','장난감 총','Firearm',-10,-35),('heart','손하트','Gesture',-42,42),('toy-sword','장난감 칼','OneHand',10,-65),('magic-wand','마법봉','OneHand',-10,-45),('firearm','권총 발사','Firearm',0,-22),('bow','활쏘기','Bow',-35,25),('one-hand','한손 휘두르기','OneHand',0,-60),('two-hand','양손 휘두르기','TwoHand',-40,40)]
 actions=[]
 for id,label,template,left,right in specs:
  timeline=[]
  for ms,factor in [(0,0),(160,1),(420,-.6),(650,1),(880,0)]:
   for target,angle in [('leftShoulder',left),('rightShoulder',right)]:timeline.append({'time':ms,'transform':{'target':target,'rotation':angle*factor}})
  effect=id if id in ['claw','toy-gun','heart','toy-sword','magic-wand','bow'] else 'toy-gun' if id=='firearm' else 'toy-sword'
  timeline.append({'time':160,'effect':{'anchor':'root','file':'samples/'+effect+'.png','duration':620}})
  path='actions/'+id+'.json';actions.append(path)
  files[path]={'schemaVersion':2,'id':id,'name':label+' · '+template,'duration':1100,'priority':80,'timeline':timeline,'playback':{'mode':'once','returnToIdle':True},'triggers':[{'id':id+'-manual','type':'manual','key':id}]}
 files['character.json']=character
 files['manifest.json']={'format':'voxpet','formatVersion':2,'id':'voxpet.samples.studio','name':'테스트 샘플 · 의상 17 / 동작 9','version':'1.1.0','author':'VoxPet','description':'공개 제작 틀과 CC0 절차 도형. 의상 14분류/17종·헤어 3종·움직이는 헬리콥터 모자/날개·손동작 5종·모든 액션 템플릿을 시험하세요.','character':'character.json','thumbnail':'images/head.png','actions':actions,'attachments':attachments}
 archive=OUT/'studio-samples.voxpet'
 with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
  for name,data in sorted(files.items()):
   entry=zipfile.ZipInfo(name,(2020,1,1,0,0,0));entry.compress_type=zipfile.ZIP_DEFLATED
   z.writestr(entry,data if isinstance(data,bytes) else json.dumps(data,ensure_ascii=False,indent=2).encode())
 (OUT/'LICENSE.txt').write_text('VoxPet Studio Samples — CC0-1.0\nProcedural artwork and original public VoxPet template. No personal character assets.\n',encoding='utf-8')
 print(archive,len(files),'files; 14 costume categories, 17 attachments, 3 hairstyles, 2 animations, 9 actions')

if __name__=='__main__':build()
