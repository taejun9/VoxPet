#!/usr/bin/env python3
"""Export code-drawn educational PNGs for both VoxPet upload formats.

Pillow is needed only to generate this optional download kit, never by the app.
No private character artwork is read. Existing destinations are never replaced.
"""
import argparse
import copy
import html
import json
import math
from pathlib import Path
import zipfile
from PIL import Image, ImageDraw

SIZE = 512
SCALE = 2
SKIN = '#EDCDA7'
OUTLINE = '#173747'
MINT = '#56CDBE'
POSES = {
    'idle': ((142, 340), (369, 340)),
    'wave-ready': ((142, 340), (381, 236)),
    'wave-left': ((142, 340), (347, 133)),
    'wave-right': ((142, 340), (395, 146)),
    'firearm-ready': ((142, 340), (395, 277)),
    'firearm-fire': ((142, 340), (390, 267)),
    'onehand-ready': ((142, 340), (369, 237)),
    'onehand-swing': ((142, 340), (386, 305)),
    'twohand-ready': ((332, 283), (376, 245)),
    'twohand-swing': ((351, 335), (389, 312)),
    'bow-ready': ((395, 287), (328, 283)),
    'bow-release': ((395, 287), (355, 283)),
}


def layer():
    return Image.new('RGBA', (SIZE*SCALE, SIZE*SCALE))


def ellipse(image, box, color, outline=OUTLINE, width=3):
    ImageDraw.Draw(image).ellipse(tuple(round(v*SCALE) for v in box), fill=color, outline=outline, width=width*SCALE)


def line(image, points, color, width):
    d = ImageDraw.Draw(image)
    d.line([(round(x*SCALE), round(y*SCALE)) for x,y in points], fill=color, width=width*SCALE, joint='curve')
    for x,y in (points[0],points[-1]):
        r=width/2
        ellipse(image,(x-r,y-r,x+r,y+r),color,None)


def polygon(image, points, color):
    ImageDraw.Draw(image).polygon([(round(x*SCALE),round(y*SCALE)) for x,y in points],fill=color)


def output(image, path):
    path.parent.mkdir(parents=True,exist_ok=True)
    image.resize((SIZE,SIZE),Image.Resampling.LANCZOS).save(path)


def arm(side, pose, sleeve=False):
    image=layer()
    start=(180,252) if side=='l' else (332,252)
    end=POSES[pose][0 if side=='l' else 1]
    points=[start,end]
    if sleeve:
        end=(start[0]+(end[0]-start[0])*.72,start[1]+(end[1]-start[1])*.72)
        points=[start,end]
    line(image,points,OUTLINE,42 if sleeve else 38)
    line(image,points,MINT if sleeve else SKIN,36 if sleeve else 32)
    if not sleeve:
        ellipse(image,(end[0]-21,end[1]-21,end[0]+21,end[1]+21),SKIN)
        if pose.startswith('wave') and side=='r':
            for dx in (-12,0,12):
                line(image,[(end[0]+dx,end[1]-8),(end[0]+dx,end[1]-32)],SKIN,10)
    return image


def weapon(template, pose):
    image=layer()
    hand=POSES[pose][1]
    x,y=hand
    if template=='Firearm':
        if pose=='idle': x,y=x+5,y-14
        polygon(image,[(x,y-24),(x+78,y-24),(x+78,y-7),(x+24,y-7),(x+18,y+17),(x+1,y+17)],OUTLINE)
        line(image,[(x+10,y-17),(x+72,y-17)],'#687F8C',9)
        line(image,[(x+76,y-22),(x+76,y-9)],'#FFBC52',5)
    elif template in ('OneHand','TwoHand'):
        angle={'idle':15,'onehand-ready':-72,'onehand-swing':38,'twohand-ready':-65,'twohand-swing':35}[pose]
        length=83 if template=='OneHand' else 96
        ex=x+math.cos(math.radians(angle))*length
        ey=y+math.sin(math.radians(angle))*length
        line(image,[(x-10,y),(ex,ey)],OUTLINE,12)
        if template=='OneHand':
            line(image,[(x+12*math.cos(math.radians(angle)),y+12*math.sin(math.radians(angle))),(ex,ey)],'#D1EBF4',8)
            line(image,[(x-3,y-13),(x+4,y+13)],'#FFBC52',6)
        else:
            line(image,[(x,y),(ex,ey)],'#CA985D',8)
            line(image,[(ex-16,ey-16),(ex+16,ey+16)],OUTLINE,31)
            line(image,[(ex-16,ey-16),(ex+16,ey+16)],'#A6BDC8',25)
    else:
        bx,by=(369,340) if pose=='idle' else (395,285)
        d=ImageDraw.Draw(image)
        d.arc(((bx-63)*SCALE,(by-76)*SCALE,bx*SCALE,(by+76)*SCALE),-85,85,fill=OUTLINE,width=9*SCALE)
        pull=330 if pose=='bow-ready' else bx-29
        line(image,[(bx-29,by-76),(pull,by),(bx-29,by+76)],'#ECE5D0',2)
        if pose!='bow-release':
            line(image,[(pull,by),(bx+55,by)],'#CA985D',4)
            polygon(image,[(bx+63,by),(bx+51,by-6),(bx+51,by+6)],'#D1EBF4')
    return image


def build(folder):
    assets=folder/'01_파츠조합_등록용'
    assets.mkdir()
    def save(name,image): output(image,assets/name); return name
    head=layer()
    ellipse(head,(153,50,359,238),'#80C8DC')
    ellipse(head,(171,84,341,221),'#FFF0D9',None)
    ellipse(head,(175,145,196,157),'#F4A3A8',None)
    ellipse(head,(316,145,337,157),'#F4A3A8',None)
    body=layer(); ellipse(body,(176,217,336,414),SKIN)
    parts=[{'Part':'Head','File':save('parts/head.png',head)}, {'Part':'Body','File':save('parts/body.png',body)}]
    for side,part in [('l','ArmL'),('r','ArmR')]:
        parts.append({'Part':part,'File':save(f'parts/arm-{side}-idle.png',arm(side,'idle'))})
    for side,part,x in [('l','LegL',190),('r','LegR',267)]:
        im=layer(); ellipse(im,(x,390,x+57,468),SKIN)
        parts.append({'Part':part,'File':save(f'parts/leg-{side}.png',im)})
    mouths=[]
    for i,height in enumerate((3,7,12,18,24,31,39,48)):
        im=layer(); ellipse(im,(238,172,274,172+height),OUTLINE,None)
        if i>=3: ellipse(im,(246,172+height*.65,266,169+height),'#EE878D',None)
        mouths.append(save(f'face/mouth-{i}.png',im))
    expressions=[]
    for kind in ('Neutral','Happy','Angry','Sad'):
        eyes=layer();closed=layer();brows=layer()
        for x in (208,302):
            if kind=='Happy':
                ImageDraw.Draw(eyes).arc(((x-13)*SCALE,117*SCALE,(x+13)*SCALE,144*SCALE),180,360,fill=OUTLINE,width=5*SCALE)
            else:
                ellipse(eyes,(x-12,117,x+12,143),OUTLINE,None)
                ellipse(eyes,(x-6,121,x-1,126),'#FFFFFF',None)
            line(closed,[(x-12,133),(x+12,133)],OUTLINE,4)
            left=x<256
            dy=8 if kind=='Angry' else -8 if kind=='Sad' else 0
            line(brows,[(x-14,104-(dy if left else -dy)),(x+14,104+(dy if left else -dy))],OUTLINE,4)
        expressions.append({'Kind':kind,'EyesOpen':save(f'face/{kind.lower()}-eyes-open.png',eyes),'EyesClosed':save(f'face/{kind.lower()}-eyes-closed.png',closed),'Brows':save(f'face/{kind.lower()}-brows.png',brows)})
    upper=layer(); ellipse(upper,(174,220,338,407),MINT)
    line(upper,[(234,246),(232,278)],'#F0FFF9',3);line(upper,[(279,246),(281,278)],'#F0FFF9',3)
    line(upper,[(223,350),(287,350)],'#219E91',4)
    save('outfit/upper-idle.png',upper)
    arms={s:{'idle':f'parts/arm-{s}-idle.png'} for s in ('l','r')}
    sleeves={s:{'idle':save(f'outfit/sleeve-{s}-idle.png',arm(s,'idle',True))} for s in ('l','r')}
    for pose in list(POSES)[1:]:
        for side in ('l','r'):
            if POSES[pose][0 if side=='l' else 1]==POSES['idle'][0 if side=='l' else 1]:continue
            arms[side][pose]=save(f'action/arm-{side}-{pose}.png',arm(side,pose))
            sleeves[side][pose]=save(f'outfit/sleeve-{side}-{pose}.png',arm(side,pose,True))
    base_layers=[{'Part':'Upper','Anchor':'Canvas','Poses':{'idle':'outfit/upper-idle.png'}}, {'Part':'ArmL','Anchor':'Canvas','Poses':sleeves['l']}, {'Part':'ArmR','Anchor':'Canvas','Poses':sleeves['r']}]
    glasses=layer()
    for x in (208,302):
        ImageDraw.Draw(glasses).ellipse(((x-22)*SCALE,109*SCALE,(x+22)*SCALE,150*SCALE),outline=OUTLINE,width=4*SCALE)
    line(glasses,[(231,127),(278,127)],OUTLINE,3)
    save('decoration/glasses-eye.png',glasses)
    mustache=layer();ellipse(mustache,(230,162,257,178),OUTLINE,None);ellipse(mustache,(255,162,282,178),OUTLINE,None)
    save('decoration/mustache-mouth.png',mustache)
    outfits=[{'Name':'민트 후드 예제','Layers':copy.deepcopy(base_layers)}]
    for name,file,anchor in [('민트 후드 + 안경','glasses-eye','Eye'),('민트 후드 + 콧수염','mustache-mouth','Mouth')]:
        outfits.append({'Name':name,'Layers':copy.deepcopy(base_layers)+[{'Part':'FaceDecoration','Anchor':anchor,'Poses':{'idle':f'decoration/{file}.png'}}]})
    actions=[]
    def action(name,template,poses):
        frames=[]
        for pose,duration in poses:
            replace={}
            for s,part in [('l','ArmL'),('r','ArmR')]:
                if pose!='idle' and pose in arms[s]:replace[part]=arms[s][pose]
            f={'DurationMs':duration,'Pose':pose,'Replace':replace}
            if pose=='firearm-fire':f['Anchors']={'Head':{'X':-2,'Y':0}}
            frames.append(f)
        actions.append({'Name':name,'Template':template,'Frames':frames})
    action('손 흔들기','Gesture',[('wave-ready',150),('wave-left',180),('wave-right',180),('wave-left',180),('idle',120)])
    action('권총 발사','Firearm',[('firearm-ready',90),('firearm-fire',80),('firearm-fire',80),('idle',100)])
    action('한손 휘두르기','OneHand',[('onehand-ready',140),('onehand-swing',110),('idle',140)])
    action('양손 휘두르기','TwoHand',[('twohand-ready',160),('twohand-swing',120),('idle',140)])
    action('활 발사','Bow',[('bow-ready',220),('bow-release',100),('idle',140)])
    actions.append({'Name':'오른팔 숨김 규칙 예제','Template':'Gesture','Frames':[{'DurationMs':500,'Pose':'hidden','Replace':{'ArmR':None}},{'DurationMs':200,'Pose':'idle','Replace':{}}]})
    weapons=[]
    empty=save('effect/empty.png',layer())
    for template,label,poses in [('Firearm','장난감 권총',['idle','firearm-ready','firearm-fire']),('OneHand','한손 검',['idle','onehand-ready','onehand-swing']),('TwoHand','양손 망치',['idle','twohand-ready','twohand-swing']),('Bow','활',['idle','bow-ready','bow-release'])]:
        mapping={p:save(f'weapon/{template.lower()}-{p}.png',weapon(template,p)) for p in poses}
        layers=[{'Part':'Weapon','Anchor':'Canvas','Poses':mapping}]
        if template in ('Firearm','Bow'):
            effect=layer()
            if template=='Firearm':
                polygon(effect,[(470,245),(505,234),(492,252),(510,270),(477,270),(480,282),(462,268)],'#FFD657')
                polygon(effect,[(469,251),(494,245),(487,263),(472,268)],'#FFF9D0')
                pose='firearm-fire'
            else:
                line(effect,[(443,285),(493,285)],'#CA985D',4)
                polygon(effect,[(504,285),(488,277),(488,293)],'#D1EBF4');pose='bow-release'
            layers.append({'Part':'Effect','Anchor':'Canvas','Poses':{'idle':empty,pose:save(f'effect/{template.lower()}-attack.png',effect)}})
        weapons.append({'Name':label+' 예제','Template':template,'Layers':layers})
    manifest={'Version':1,'CharacterId':'neulbo','CanvasSize':SIZE,'Parts':parts,'Mouths':mouths,'Expressions':expressions,'Outfits':outfits,'Weapons':weapons,'Actions':actions,'MouthOffsets':[{'X':0,'Y':i//2} for i in range(8)]}
    for cid in ('neulbo','pelly'):
        manifest['CharacterId']=cid
        (assets/f'imageset-{cid}.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return assets,manifest


def compose(assets,manifest,expression='Neutral',mouth=0,closed=False,outfit=0,weapon_index=-1,frame=None):
    images=[]
    replacements=frame.get('Replace',{}) if frame else {}
    pose=frame['Pose'] if frame else 'idle'
    head_offset=frame.get('Anchors',{}).get('Head',{'X':0,'Y':0}) if frame else {'X':0,'Y':0}
    order=['CapeBack','ArmL','LegL','LegR','Body','Lower','Upper','Shoes','Head','Eyes','Brows','Mouth','FaceDecoration','ArmR','Weapon','Effect']
    for part in manifest['Parts']:
        file=replacements.get(part['Part'],part['File'])
        if file:images.append((part['Part'],file,'Head' if part['Part']=='Head' else 'Canvas',0))
    face=next(e for e in manifest['Expressions'] if e['Kind']==expression)
    images.extend([('Eyes',face['EyesClosed' if closed else 'EyesOpen'],'Eye',0),('Brows',face['Brows'],'Face',0),('Mouth',manifest['Mouths'][mouth],'Mouth',0)])
    if outfit>=0:
        for l in manifest['Outfits'][outfit]['Layers']:
            if l['Part'] in replacements and replacements[l['Part']] is None:continue
            images.append((l['Part'],l['Poses'].get(pose,l['Poses']['idle']),l['Anchor'],1))
    if weapon_index>=0:
        for l in manifest['Weapons'][weapon_index]['Layers']:
            if pose in l['Poses'] and (l['Part']!='Effect' or frame):images.append((l['Part'],l['Poses'][pose],l['Anchor'],1))
    result=Image.new('RGBA',(SIZE,SIZE))
    for part,file,anchor,costume in sorted(images,key=lambda i:(order.index(i[0]),i[3])):
        with Image.open(assets/file) as im:
            offset=(0,0) if anchor=='Canvas' else (head_offset['X'],head_offset['Y']+(manifest['MouthOffsets'][mouth]['Y'] if anchor=='Mouth' else 0))
            result.alpha_composite(im,dest=offset)
    return result


def extras(folder,assets,manifest):
    preview=folder/'03_미리보기_업로드하지않음';preview.mkdir()
    sheets=folder/'02_기존PNG시트_등록용';sheets.mkdir()
    for expression in ('Neutral','Happy','Angry','Sad'):
        for columns in (3,8):
            sheet=Image.new('RGBA',(SIZE*columns,SIZE*2))
            for row in range(2):
                for col in range(columns):
                    mouth=col if columns==8 else (0,3,7)[col]
                    sheet.alpha_composite(compose(assets,manifest,expression,mouth,bool(row)),dest=(col*SIZE,row*SIZE))
            sheet.save(sheets/f'{expression.lower()}-{columns}x2.png')
    cards=[]
    def card(name,image):
        file=f'{len(cards)+1:02d}.png';image.save(preview/file);cards.append((name,file))
    card('평상 · 파츠6개 + 표정 + 입 + 민트 후드',compose(assets,manifest))
    for expression in ('Happy','Angry','Sad'):card('표정 · '+expression,compose(assets,manifest,expression,5))
    card('눈 감음',compose(assets,manifest,closed=True))
    card('안경 · Eye Anchor',compose(assets,manifest,outfit=1))
    card('콧수염 · Mouth Anchor',compose(assets,manifest,mouth=7,outfit=2))
    for action in manifest['Actions']:
        wi=next((i for i,w in enumerate(manifest['Weapons']) if w['Template']==action['Template']),-1)
        for index,frame in enumerate(action['Frames']):
            card(f"{action['Name']} · {index+1} · {frame['Pose']}",compose(assets,manifest,mouth=3,weapon_index=wi,frame=frame))
    contact=Image.new('RGB',(256*6,288*math.ceil(len(cards)/6)),'#F1F6F9')
    for index,(_,file) in enumerate(cards):
        with Image.open(preview/file) as image:
            image=image.resize((256,256),Image.Resampling.LANCZOS)
            x,y=(index%6)*256,(index//6)*288
            contact.paste(image,(x,y),image)
            ImageDraw.Draw(contact).text((x+12,y+262),f'{index+1:02d}',fill=OUTLINE)
    contact.save(preview/'모아보기.jpg',quality=94)
    sections=''.join(f'<figure><img src="03_미리보기_업로드하지않음/{file}"><figcaption>{html.escape(name)}</figcaption></figure>' for name,file in cards)
    asset_cards=''.join(f'<figure><img src="01_파츠조합_등록용/{file.relative_to(assets).as_posix()}"><figcaption>{html.escape(file.relative_to(assets).as_posix())}</figcaption></figure>' for file in sorted(assets.rglob('*.png')))
    page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>VoxPet 업로드 이미지 예시</title><style>body{margin:0;background:#071722;color:#eaf5ff;font:16px system-ui;padding:32px;line-height:1.65}main{max-width:1280px;margin:auto}h1{font-size:32px}p{max-width:900px;color:#b3cedd}h2{margin-top:40px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px}figure{margin:0;padding:12px;background:#0e2535;border:1px solid #26465d;border-radius:12px}img{display:block;width:100%;background:repeating-conic-gradient(#e1e7ec 0% 25%,#f6f8fa 0% 50%) 50%/24px 24px;border-radius:8px}figcaption{padding:8px 2px;font-size:13px;overflow-wrap:anywhere}code{color:#70e6d5}a{color:#64b1ff}</style><main><h1>VoxPet 업로드 이미지 예시</h1><p>512×512 투명 PNG로 만든 교육용 도형 캐릭터입니다. 실제 늘보군·펠리 그림은 포함하지 않습니다. 같은 Canvas에 제자리에 그린 파츠를 조합하며, 액션마다 팔·소매·무기가 같이 바뀝니다.</p><p><b>새 조합 방식:</b> 캐릭터 → 이미지 세트 → 이미지 세트 등록에서 늘보군은 <code>01_파츠조합_등록용/imageset-neulbo.json</code>, 펠리는 <code>imageset-pelly.json</code>을 선택하세요. 두 JSON은 같은 예시 그림을 각 캐릭터 ID에 등록합니다. 개별 PNG만 등록하지 마세요.</p><p><b>기존 시트 방식:</b> 기존 PNG 사용으로 돌아간 뒤 <code>02_기존PNG시트_등록용</code>의 3×2 또는8×2 PNG를 불러오세요. 미리보기 그림은 등록용이 아닙니다. 자세한 절차는 <a href="사용방법.txt">사용방법.txt</a>를 보세요.</p><h2>조합 결과와 액션 순서</h2><div class="grid">'''+sections+'''</div><h2>등록용 개별 PNG · 같은512px Canvas</h2><p>체크무늬는 화면 미리보기 배경입니다. 실제 PNG의 빈 영역은 투명합니다. 부위 크기에 맞춰 Canvas를 자르지 마세요.</p><div class="grid">'''+asset_cards+'</div></main></html>'
    (folder/'미리보기.html').write_text(page,encoding='utf-8')
    (folder/'사용방법.txt').write_text('''VoxPet 업로드 이미지 예시
==========================
이 파일들은 제작 규격을 이해하고 등록을 시험하는 교육용 도형입니다.
늘보군·펠리 개인 원본을 포함하거나 수정하지 않았습니다.
맥에서는 미리보기.html과 PNG를 볼 수 있습니다. VoxPet 화면 실행은 Windows가 필요합니다.

1. 새 이미지 조합 방식
- ZIP이면 먼저 전체를 풀어 주세요. 폴더 구조와 파일 이름을 유지합니다.
- VoxPet에서 늘보군 또는 펠리를 선택합니다.
- 캐릭터 → 이미지 세트 → 이미지 세트 등록을 누릅니다.
- 늘보군: 01_파츠조합_등록용/imageset-neulbo.json
- 펠리:   01_파츠조합_등록용/imageset-pelly.json
  두 JSON은 같은 교육용 그림을 서로 다른 등록 대상에 연결합니다.
- 의상 → 이미지 의상에서 민트 후드/안경/콧수염을 선택합니다.
- 액션 → 프레임 액션에서 액션과 대응 무기를 선택한 뒤 재생합니다.
  권총 발사 ↔ 장난감 권총 / 한손 휘두르기 ↔ 한손 검
  양손 휘두르기 ↔ 양손 망치 / 활 발사 ↔ 활
  손 흔들기·팔 숨김에는 무기를 선택하지 않습니다.
- 마이크 없이 데모로 입8단계를, 표정 선택으로 눈·눈썹 변화를 시험합니다.

2. 파일 역할 (01_파츠조합_등록용)
parts/: Head, Body, ArmL, ArmR, LegL, LegR 필수6개. Head에 눈·입 없음.
face/: 표정별 눈 뜸/눈 감음/눈썹, 입0(닫힘)~7(크게 열림).
action/: 포즈별 교체 팔. Replace의 해당 팔을 대체하며 기본 팔과 중복하지 않음.
outfit/: 몸통 상의와 포즈별 양팔 소매. 동작 중에도 해당 소매가 함께 바뀜.
weapon/: 무기4종의 idle/준비/공격 포즈. 총구·발사는 오른쪽 방향.
effect/: 총구 효과와 발사 화살, 완전 투명 empty.png(의도한 빈 이미지).
decoration/: 안경은 Eye, 콧수염은 Mouth Anchor를 따름.
imageset-*.json: 모든 파일과 시간/레이어/숨김/포즈/Anchor를 연결하는 등록 파일.
모든 PNG는512×512,8비트 RGBA, 배경 투명. 빈 공간까지 동일 Canvas 유지.
샘플을 그림 도구에 열어 다른 그림으로 교체할 때 위치와 Canvas를 유지합니다.
의상은 액션 팔과 소매를 별도로 준비하며 액션 팔에 소매를 중복해서 그리지 않습니다.
프레임의 Replace에 ArmR:null이면 오른팔과 소매가 함께 숨겨집니다.
연타는 첫 프레임부터 다시 시작하며 액션이 끝나면 기본 상태로 복귀합니다.

3. 기존 완성 PNG 시트 방식 (02_기존PNG시트_등록용)
새 조합 모드를 쓰고 있다면 캐릭터 → 이미지 세트 → 기존 PNG 사용을 누릅니다.
기본 시트는 PNG 시트 불러오기/개인 기본 PNG 선택에 사용합니다.
표정 시트는 표정 슬롯 선택 → 슬롯 PNG 선택 → 슬롯 저장으로 등록합니다.
neutral=기본, happy=기쁨, angry=화남, sad=슬픔.
-3x2.png:1536×1024, 셀512px. 열=입 닫힘/중간/크게 열림.
-8x2.png:4096×1024, 셀512px. 열=입0~7.
두 방식 모두 첫째 행=눈 뜸, 둘째 행=눈 감음.
시트에는 완성 캐릭터가 들어 있고 JSON 조합용 파츠 PNG와 서로 다릅니다.
표정 슬롯 PNG는 얼굴 한 장이 아니라 같은 규격의 완성 시트입니다.

4. 미리보기
미리보기.html을 브라우저로 열면 각 파일과 조합 결과를 볼 수 있습니다.
03_미리보기_업로드하지않음의 PNG/JPG는 설명용이며 등록하지 않습니다.
HTML의 체크무늬는 PNG 배경이 아니라 투명을 보여주는 화면 효과입니다.
''',encoding='utf-8')
    return len(cards)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('destination',type=Path)
    args=parser.parse_args()
    folder=args.destination.resolve()
    archive=folder.with_name(folder.name+'.zip')
    if folder.exists() or archive.exists():parser.error('Destination or ZIP already exists; choose a new name.')
    folder.mkdir(parents=True)
    assets,manifest=build(folder)
    previews=extras(folder,assets,manifest)
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
        for file in sorted(folder.rglob('*')):
            if file.is_file():z.write(file,Path(folder.name)/file.relative_to(folder))
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None:raise ValueError('ZIP CRC validation failed')
    print(json.dumps({'folder':str(folder),'archive':str(archive),'partPngs':len(list(assets.rglob('*.png'))),'legacySheets':8,'composedPreviews':previews},ensure_ascii=False))


if __name__=='__main__':main()
