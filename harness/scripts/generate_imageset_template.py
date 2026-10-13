#!/usr/bin/env python3
"""Generate synthetic, redistributable image-composition examples; never reads private character PNGs."""
from pathlib import Path
import json
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'src/VoxPet.App/Assets/ImageSetTemplate'
SIZE = 512
OUT.mkdir(parents=True, exist_ok=True)


def save(name, shapes):
    pixels = bytearray(SIZE * SIZE * 4)
    for shape, bounds, color in shapes:
        x0, y0, x1, y1 = bounds
        for y in range(max(0, y0), min(SIZE, y1)):
            for x in range(max(0, x0), min(SIZE, x1)):
                if shape == 'ellipse' and ((x - (x0+x1)/2) / ((x1-x0)/2))**2 + ((y - (y0+y1)/2) / ((y1-y0)/2))**2 > 1:
                    continue
                offset = (y * SIZE + x) * 4
                pixels[offset:offset+4] = bytes(color)
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    scanlines = b''.join(b'\0' + pixels[y*SIZE*4:(y+1)*SIZE*4] for y in range(SIZE))
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', SIZE, SIZE, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(scanlines, 9)) + chunk(b'IEND', b'')
    (OUT / name).write_bytes(png)


skin = (169, 212, 232, 255)
shirt = (51, 133, 186, 255)
sleeve = (21, 92, 139, 255)
black = (15, 36, 50, 255)
parts = [('Head', 'head.png', [('ellipse', (150, 45, 360, 228), skin)]),
         ('Body', 'body.png', [('ellipse', (172, 208, 340, 420), shirt)]),
         ('ArmL', 'arm-l.png', [('ellipse', (122, 232, 188, 372), skin)]),
         ('ArmR', 'arm-r.png', [('ellipse', (324, 232, 390, 372), skin)]),
         ('LegL', 'leg-l.png', [('ellipse', (187, 385, 247, 470), skin)]),
         ('LegR', 'leg-r.png', [('ellipse', (265, 385, 325, 470), skin)])]
for _, file, shapes in parts:
    save(file, shapes)
save('eyes-open.png', [('ellipse', (194, 113, 222, 139), black), ('ellipse', (288, 113, 316, 139), black)])
save('eyes-closed.png', [('rect', (194, 128, 222, 133), black), ('rect', (288, 128, 316, 133), black)])
save('eyes-happy.png', [('rect', (194, 121, 222, 126), black), ('rect', (288, 121, 316, 126), black)])
for index, height in enumerate((4, 17, 32)):
    save(f'mouth-{index}.png', [('ellipse', (238, 169, 274, 169+height), black)])
for pose, rect in [('idle', (328, 230, 386, 322)), ('ready', (330, 219, 403, 279)), ('fire', (330, 235, 423, 290)), ('return', (327, 234, 400, 300))]:
    save(f'sleeve-{pose}.png', [('ellipse', rect, sleeve)])
    if pose != 'idle':
        x0, y0, x1, y1 = rect
        save(f'arm-{pose}.png', [('ellipse', (x0, y0, x1+20, y1+22), skin)])
    gx, gy = {'idle': (360, 318), 'ready': (390, 247), 'fire': (411, 255), 'return': (391, 265)}[pose]
    save(f'gun-{pose}.png', [('rect', (gx, gy, gx+68, gy+19), black), ('rect', (gx+4, gy+12, gx+24, gy+44), black), ('rect', (gx+63, gy, gx+69, gy+19), (255, 175, 50, 255))])
save('effect-fire.png', [('ellipse', (483, 247, 510, 281), (255, 219, 64, 255))])
save('effect-idle.png', [])
manifest = {
    'Version': 1, 'CharacterId': 'neulbo', 'CanvasSize': SIZE,
    'Parts': [{'Part': part, 'File': file} for part, file, _ in parts],
    'Mouths': [f'mouth-{i}.png' for i in range(3)],
    'Expressions': [{'Kind': 'Neutral', 'EyesOpen': 'eyes-open.png', 'EyesClosed': 'eyes-closed.png'}, {'Kind': 'Happy', 'EyesOpen': 'eyes-happy.png', 'EyesClosed': 'eyes-closed.png'}],
    'Outfits': [{'Name': '제작 예제 · 파란 소매', 'Layers': [{'Part': 'ArmR', 'Anchor': 'Canvas', 'Poses': {pose: f'sleeve-{pose}.png' for pose in ['idle', 'ready', 'fire', 'return']}}]}],
    'Weapons': [{'Name': '제작 예제 · 장난감 권총', 'Template': 'Firearm', 'Layers': [
        {'Part': 'Weapon', 'Anchor': 'Canvas', 'Poses': {pose: f'gun-{pose}.png' for pose in ['idle', 'ready', 'fire', 'return']}},
        {'Part': 'Effect', 'Anchor': 'Canvas', 'Poses': {'idle': 'effect-idle.png', 'fire': 'effect-fire.png'}}]}],
    'Actions': [{'Name': '권총 발사', 'Template': 'Firearm', 'Frames': [
        {'DurationMs': duration, 'Pose': pose, 'Replace': {'ArmR': f'arm-{pose}.png'}} for duration, pose in [(80, 'ready'), (80, 'fire'), (80, 'fire'), (100, 'return')]]}],
    'MouthOffsets': [{'X': 0, 'Y': 0}, {'X': 0, 'Y': 2}, {'X': 0, 'Y': 5}]
}
(OUT / 'imageset.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Generated synthetic full-Canvas template:', len(list(OUT.glob('*.png'))), 'PNGs')
