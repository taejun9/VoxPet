#!/usr/bin/env python3
"""Validate real Windows desktop evidence coverage and PNG CRC; no private assets."""
from pathlib import Path
import argparse
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
import zlib

parser = argparse.ArgumentParser()
parser.add_argument('folder', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
ns = {'w': 'http://schemas.microsoft.com/winfx/2006/xaml/presentation', 'x': 'http://schemas.microsoft.com/winfx/2006/xaml'}
tree = ET.parse(root / 'src/VoxPet.App/Views/MainWindow.xaml')
tabs = tree.find('.//w:TabControl[@x:Name="SettingsTabs"]', ns)
expected = set()
for tab, item in enumerate(tabs.findall('w:TabItem', ns)):
    nested = item.find('.//w:TabControl', ns)
    pages = len(nested.findall('w:TabItem', ns)) if nested is not None else 1
    for width, height in [(1000, 730), (960, 540), (640, 480), (480, 320)]:
        for page in range(pages): expected.add(f'layout-{width}x{height}-tab-{tab}-{page}')
for name in ['home','library','basic','parts','anchors','face','expressions','actions','attachments','export']:
    expected.add('editor-'+name)
expressions = ['neutral', 'happy', 'sad', 'angry', 'surprised', 'sleepy', 'confused', 'excited', 'love', 'smug', 'playful', 'shy']
# Enum names are read from the source so the evidence contract follows the established twelve-slot order.
import re
source = (root / 'src/VoxPet.Core/Models/ExpressionProfile.cs').read_text()
block = re.search(r'enum ExpressionKind\s*\{([^}]+)\}', source, re.S).group(1)
expressions = [part.split('=')[0].strip().lower() for part in block.split(',') if part.strip()]
for kind in expressions:
    expected.add('expression-ui-' + kind)
    for mouth in range(3):
        for eye in range(2): expected.add(f'expression-{kind}-mouth-{mouth}-eye-{eye}')
for gesture in ['claw', 'toygun', 'heart', 'toysword', 'magicwand']:
    for frame in range(4): expected.add(f'hand-{gesture}-{frame}')
for angle in [0, 6, 12]: expected.add(f'attachment-head-{angle}')
for level in range(3): expected.add(f'attachment-mouth-{level}')
for case in ['broadcast-transparent', 'broadcast-green-again', 'broadcast-while-settings-in-tray', 'settings-restored-from-tray', 'settings-initial', 'settings-initial-broadcast', 'settings-restarted', 'settings-restarted-broadcast']:
    expected.add(case)
for character in ['neulbo', 'pelly']: expected.add('selection-' + character)
for run in ['unsigned', 'signed']:
    for case in ['fresh12', 'restart-reversed12', 'legacy3-slot', 'corrupt-state', 'restart-pelly', 'missing-character', 'corrupt-character', 'restart-settings', 'public6']: expected.add(f'startup-{run}-{case}')
expected.update(['imageset-preview', 'imageset-broadcast', 'studio-sample-broadcast'])
for tab in range(7): expected.add(f'studio-settings-{tab}')
for width in [1280, 960, 640, 480]:
    for page in range(6): expected.add(f'studio-page-{width}-{page}')
for category in ['hat','glasses','facedecoration','earrings','top','bottom','onepiece','shoes','outerwear','cape','headaccessory','backaccessory','handaccessory','effect','hair-short','hair-long','hair-afro']: expected.add('studio-costume-'+category)
for action in ['claw','toy-gun','heart','toy-sword','magic-wand','firearm','bow','one-hand','two-hand']: expected.add('studio-action-'+action)

for hair in ['hair-short', 'hair-long', 'hair-afro']: expected.add('studio-hair-' + hair)
for frame in range(4):
    expected.add(f'studio-accessory-animation-{frame}')
    expected.add(f'studio-accessory-broadcast-{frame}')
for item in ['hat', 'cape']: expected.add('studio-editor-animation-' + item)

for case in ['keyboard', 'editor', 'restarted']: expected.add('card-library-' + case)
for character in ['neulbo', 'pelly']:
    for category in ['hat','glasses','facedecoration','earrings','top','bottom','onepiece','shoes','outerwear','cape','headaccessory','backaccessory','handaccessory','effect','hair-short','hair-long','hair-afro']:
        expected.add(f'companion-{character}-costume-{category}')
    for action in ['claw','toy-gun','heart','toy-sword','magic-wand','firearm','bow','one-hand','two-hand']:
        expected.add(f'companion-{character}-action-{action}')
    for hair in ['hair-short', 'hair-long', 'hair-afro']: expected.add(f'companion-{character}-{hair}')
    for frame in range(4): expected.add(f'companion-{character}-animation-{frame}')
    for case in ['broadcast','editor']: expected.add(f'companion-{character}-{case}')
expected.update(['companion-library','companion-restarted'])

records = []
for name in sorted(expected):
    metadata = json.loads((args.folder / f'voxpet-screen-{name}.json').read_text())
    assert metadata['valid'] and metadata['actualDesktopPixels'] and not metadata['syntheticRender'] and not metadata['privateAssets'], name
    assert metadata['capture'] == 'desktop-bitblt-captureblt' and metadata['foreground'] == metadata['hwnd'] and metadata['uniqueColors'] > 24, name
    png = args.folder / metadata['fileName']; data = png.read_bytes()
    assert png.name == f'voxpet-screen-{name}.png' and data[:8] == b'\x89PNG\r\n\x1a\n', name
    assert struct.unpack('>II', data[16:24]) == (metadata['width'], metadata['height']), name
    offset = 8
    while offset < len(data):
        size = struct.unpack('>I', data[offset:offset+4])[0]
        chunk = data[offset+4:offset+8+size]
        crc = struct.unpack('>I', data[offset+8+size:offset+12+size])[0]
        assert zlib.crc32(chunk) & 0xffffffff == crc, name
        offset += 12 + size
    assert offset == len(data), name
    records.append(metadata | {'pngSha256': hashlib.sha256(data).hexdigest()})
assert len(tabs.findall('w:TabItem', ns)) == 7 and len(expressions) == 12
report = {'actualDesktopPixels': True, 'privateAssetsUploaded': False, 'count': len(records), 'records': records}
(args.folder / 'voxpet-screen-coverage.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'PASS: {len(records)} real desktop screens, all tabs/scenes/startups, PNG CRC; no private images')
