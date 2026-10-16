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
    definition['parts'] = [{'id': 'sprite', 'type': 'sprite', 'file': 'samples/empty.png', 'anchor': 'root'}]
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
    definition['anchors'] = anchors
    definition['motion'] = {'idle': {'breathing': 0, 'sway': 0}, 'voice': {'bodyBounce': 0, 'headBounce': 0, 'headRotation': 0}}
    manifest = files['manifest.json']
    manifest.update(id='voxpet.samples.' + character_id, name=('늘보군' if character_id == 'neulbo' else '펠리') + ' 의상 · 동작',
                    version='2.0.0', description='선택한 캐릭터에 직접 착용하는17의상/9동작. 새 수채화 의상과 소품. 개인 그림은 포함하지 않습니다.', thumbnail='thumbnails/hat.png')
    for reference in manifest['actions']:
        files[reference]['name'] = files[reference]['name'].split(' · ')[0]
    for reference in manifest['attachments']:
        attachment = files[reference]
        if attachment['id'] == 'faceDecoration':
            attachment['layers'][0]['anchor'] = 'head'
            attachment['layers'][0]['inherit']['mouthMotion'] = False
        if attachment['id'].startswith('hair-'):
            # Generated wigs have a genuine transparent face opening, one registered layer.
            attachment['layers'] = [dict(id='image', file='samples/' + attachment['id'] + '.png', anchor='head', zIndex=295)]
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
