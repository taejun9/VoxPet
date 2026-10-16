"""Assemble public costume-only companion packs from registered PNG assets (stdlib).

The character PNGs remain local at runtime; no personal images are read here.
"""
from pathlib import Path
import json
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[2]
ART = Path(sys.argv[1])
OUT = ROOT / 'src/VoxPet.App/Assets/Samples'


def build(character_id):
    with zipfile.ZipFile(OUT / 'studio-samples.voxpet') as reference:
        files = {n: json.loads(reference.read(n)) for n in reference.namelist() if n.endswith('.json')}
    definition = files['character.json']
    part_anchors = {'arm-left': 'leftShoulder', 'leg-left': 'leftFoot', 'leg-right': 'rightFoot', 'body': 'body', 'head': 'head', 'arm-right': 'rightShoulder'}
    part_orders = {'arm-left': 50, 'leg-left': 20, 'leg-right': 30, 'body': 40, 'head': 80, 'arm-right': 100}
    definition['parts'] = [dict(id=part, type='arm' if part.startswith('arm-') else 'leg' if part.startswith('leg-') else part, file='samples/empty.png', anchor=anchor, zIndex=part_orders[part]) for part, anchor in part_anchors.items()]
    skinning = dict(rootY=315 if character_id=='neulbo' else 285, fullY=415 if character_id=='neulbo' else 360)
    for part in definition['parts']:
        if part['type'] == 'arm': part['skinning'] = skinning
    for key in ['mouth', 'expressions', 'face']:
        definition.pop(key, None)
    # Public pack has no stand-in character. The app supplies the selected real sheet.
    anchors = {'root': {}, 'body': {'x': 256, 'y': 330}, 'head': {'x': 256, 'y': 240},
               'mouth': {'x': 256, 'y': 245}, 'leftShoulder': {'x': 110, 'y': 290},
               'rightShoulder': {'x': 402, 'y': 290}, 'leftHand': {'x': 110, 'y': 422},
               'rightHand': {'x': 402, 'y': 422}}
    if character_id == 'pelly':
        anchors.update({'head': {'x': 300, 'y': 200}, 'mouth': {'x': 220, 'y': 180},
                        'leftShoulder': {'x': 125, 'y': 264}, 'rightShoulder': {'x': 386, 'y': 264},
                        'leftHand': {'x': 125, 'y': 390}, 'rightHand': {'x': 415, 'y': 385}})
    anchors['leftShoulder'] = dict(x=145 if character_id=='neulbo' else 125, y=315 if character_id=='neulbo' else 285)
    anchors['rightShoulder'] = dict(x=367 if character_id=='neulbo' else 330, y=315 if character_id=='neulbo' else 285)
    shoulder_left, shoulder_right = anchors['leftShoulder'], anchors['rightShoulder']
    hand_left, hand_right = anchors['leftHand'], anchors['rightHand']
    anchors['body'] = {'parent': 'root'}
    for name in ['head', 'leftShoulder', 'rightShoulder']:
        anchors[name]['parent'] = 'body'
    for hand, shoulder, parent in [(hand_left, shoulder_left, 'leftShoulder'), (hand_right, shoulder_right, 'rightShoulder')]:
        hand.update(x=hand['x'] - shoulder['x'], y=hand['y'] - shoulder['y'], parent=parent)
    anchors.update(hips=dict(parent='body', x=256, y=400), leftFoot=dict(parent='body', x=190 if character_id=='neulbo' else 260, y=465 if character_id=='neulbo' else 435), rightFoot=dict(parent='body', x=322 if character_id=='neulbo' else 340, y=465 if character_id=='neulbo' else 445))
    definition['anchors'] = anchors
    definition['motion'] = {'idle': {'breathing': 0, 'sway': 0}, 'voice': {'bodyBounce': 0, 'headBounce': 0, 'headRotation': 0}}
    manifest = files['manifest.json']
    manifest.update(id='voxpet.samples.' + character_id, name=('늘보군' if character_id == 'neulbo' else '펠리') + ' 의상 · 동작',
                    version='3.0.0', description='여섯 신체 파츠에 연결하는17의상/9동작·헤어3종. 개인 그림은 포함하지 않습니다.', thumbnail='thumbnails/hat.png')
    for reference in manifest['actions']:
        files[reference]['name'] = files[reference]['name'].split(' · ')[0]
        action = files[reference]
        grip = (402, 422) if character_id == 'neulbo' else (415, 385)
        if action['id'] in ['toy-gun', 'toy-sword', 'magic-wand', 'firearm', 'bow', 'one-hand', 'two-hand']:
            for entry in action['timeline']:
                if 'effect' in entry:
                    entry['effect'].update(anchor='rightHand', transform=dict(x=-grip[0], y=-grip[1]))
        if action['id'] == 'claw':
            action['name'] = '법규'
            action['timeline'] = []
            for time, angle in [(0, 0), (160, 52), (700, 52), (1000, 0)]:
                action['timeline'].extend([dict(time=time, transform=dict(target='rightShoulder', rotation=angle)), dict(time=time, transform=dict(target='rightHand', rotation=-angle))])
            action['timeline'].append(dict(time=160, effect=dict(anchor='rightHand', file='samples/claw.png', duration=620, transform=dict(x=-grip[0], y=-grip[1]))))
    for reference in manifest['attachments']:
        attachment = files[reference]
        kind = attachment['id']
        if kind in ['top', 'outerwear']:
            attachment['layers'] = [dict(id=part, file=f'samples/{kind}-{part}.png', anchor=anchor, zIndex=z, requiredVisibleParts=[required]) for part, anchor, z, required in [('left', 'leftShoulder', 51, 'arm-left'), ('body', 'body', 65 if kind=='outerwear' else 60, 'body'), ('right', 'rightShoulder', 101, 'arm-right')]]
            for layer in attachment['layers']:
                if layer['id'] in ['left', 'right']: layer['skinning'] = skinning
        if kind == 'onePiece':
            attachment['layers'] = [dict(id=part, file=f'samples/{kind}-{part}.png', anchor=anchor, zIndex=z, requiredVisibleParts=[required]) for part, anchor, z, required in [('body', 'body', 60, 'body'), ('left', 'leftFoot', 55, 'leg-left'), ('right', 'rightFoot', 55, 'leg-right')]]
        if kind in ['bottom', 'shoes']:
            attachment['layers'] = [dict(id=part, file=f'samples/{kind}-{part}.png', anchor=anchor, zIndex=z, requiredVisibleParts=[required]) for part, anchor, z, required in [('left', 'leftFoot' if kind=='shoes' else 'hips', 25 if kind=='shoes' else 50, 'leg-left' if kind=='shoes' else 'body'), ('right', 'rightFoot' if kind=='shoes' else 'hips', 35 if kind=='shoes' else 50, 'leg-right' if kind=='shoes' else 'body')]]
        if attachment['id'] == 'faceDecoration':
            attachment['layers'][0]['anchor'] = 'head'
            attachment['layers'][0]['inherit']['mouthMotion'] = False
        if attachment['id'].startswith('hair-'):
            # Back locks stay behind the original face/beak; the shallow fringe is in front.
            attachment['layers'] = [dict(id='back', file='samples/' + attachment['id'] + '.png', anchor='head', zIndex=-5),
                                    dict(id='fringe', file='samples/' + attachment['id'] + '-fringe.png', anchor='head', zIndex=295)]
    for p in (ART / character_id).glob('*.png'):
        name = 'thumbnails/' + p.name.removeprefix('thumb-') if p.name.startswith('thumb-') else 'samples/' + p.name
        files[name] = p.read_bytes()
    archive = OUT / ('companion-' + character_id + '.voxpet')
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as target:
        for name, data in sorted(files.items()):
            item = zipfile.ZipInfo(name, (2020, 1, 1, 0, 0, 0)); item.compress_type = zipfile.ZIP_DEFLATED
            target.writestr(item, data if isinstance(data, bytes) else json.dumps(data, ensure_ascii=False, indent=2).encode())
    print(archive, len(files), 'files, no personal/stand-in character')


for selected in ['neulbo', 'pelly']:
    build(selected)
