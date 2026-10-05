#!/usr/bin/env python3
"""Verify release asset contracts and forbidden recording/network APIs; not Windows runtime QA."""
from pathlib import Path
import json
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
errors = []
assets = ROOT / 'src/VoxPet.App/Assets/Characters'
manifest = json.loads((assets / 'manifest.json').read_text(encoding='utf-8'))
if len(manifest['states']) != 6 or manifest['size'] != [512, 512]:
    errors.append('Character manifest must have six matching states')
for name in manifest['states']:
    data = (assets / name).read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or struct.unpack('>II', data[16:24]) != (512, 512) or data[25] != 6:
        errors.append(f'Invalid RGBA PNG contract: {name}')
for path in (ROOT / 'src').rglob('*.cs'):
    if 'obj' in path.parts or 'bin' in path.parts:
        continue
    content = path.read_text(encoding='utf-8')
    for forbidden in ('WasapiLoopbackCapture', 'WaveFileWriter', 'HttpClient', 'WebClient', 'TcpClient', '.Wait()', '.Result'):
        if forbidden in content:
            errors.append(f'{path.relative_to(ROOT)} contains forbidden API {forbidden}')
for name in ('src/VoxPet.App/packages.lock.json', 'tests/VoxPet.Core.Tests/packages.lock.json', 'VoxPet.sln', 'global.json'):
    if not (ROOT / name).is_file(): errors.append(f'Missing release contract: {name}')
if errors:
    for error in errors: print(f'FAIL: {error}')
    sys.exit(1)
print('PASS: 6 aligned PNG states, pinned packages, no recording/network/blocking UI APIs')
