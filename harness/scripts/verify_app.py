#!/usr/bin/env python3
"""Verify release asset contracts and forbidden recording/network APIs; not Windows runtime QA."""
from pathlib import Path
import json
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
errors = []
# manifest와 기본 PNG의 개수/크기/RGBA 헤더 계약을 대조한다. 픽셀 정렬의 시각 품질은 별도 QA로 확인한다.
assets = ROOT / 'src/VoxPet.App/Assets/Characters'
manifest = json.loads((assets / 'manifest.json').read_text(encoding='utf-8'))
if len(manifest['states']) != 6 or manifest['size'] != [512, 512]:
    errors.append('Character manifest must have six matching states')
expressions = manifest.get('expressions', {})
if set(expressions) != {'happy', 'sad', 'angry', 'surprised', 'sleepy'} or any(len(states) != 6 for states in expressions.values()):
    errors.append('Expression manifest must have five variants with six states each')
for name in manifest['states'] + [name for states in expressions.values() for name in states]:
    data = (assets / name).read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or struct.unpack('>II', data[16:24]) != (512, 512) or data[25] != 6:
        errors.append(f'Invalid RGBA PNG contract: {name}')
# MVP의 저장·네트워크·UI 동기 대기 금지 계약을 소스 텍스트에서 검사한다.
# 주석도 검색 대상이므로 정적 검사 통과가 런타임 개인정보 검증을 대신하지 않는다.
for path in (ROOT / 'src').rglob('*.cs'):
    if 'obj' in path.parts or 'bin' in path.parts:
        continue
    content = path.read_text(encoding='utf-8')
    for forbidden in ('WasapiLoopbackCapture', 'WaveFileWriter', 'HttpClient', 'WebClient', 'TcpClient', '.Wait()', '.Result'):
        if forbidden in content:
            errors.append(f'{path.relative_to(ROOT)} contains forbidden API {forbidden}')
# 배포에 필요한 잠금 파일과 고정 SDK/솔루션의 존재를 확인한다.
for name in ('src/VoxPet.App/packages.lock.json', 'tests/VoxPet.Core.Tests/packages.lock.json', 'VoxPet.sln', 'global.json'):
    if not (ROOT / name).is_file(): errors.append(f'Missing release contract: {name}')
if errors:
    for error in errors: print(f'FAIL: {error}')
    sys.exit(1)
print('PASS: 36 aligned PNG states, pinned packages, no recording/network/blocking UI APIs')
