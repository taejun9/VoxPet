#!/usr/bin/env python3
"""Verify release asset contracts and forbidden recording/network APIs; not Windows runtime QA."""
from pathlib import Path
import json
import struct
import sys
import zipfile

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
# 조합 제작 틀은 개인 자료 없이 재생성한 합성 PNG다. 모든 파일의 동일 Canvas 계약을 확인한다.
example = ROOT / 'src/VoxPet.App/Assets/ImageSetTemplate'
image_set = json.loads((example / 'imageset.json').read_text(encoding='utf-8'))
for path in example.glob('*.png'):
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or struct.unpack('>II', data[16:24]) != (image_set['CanvasSize'],) * 2 or data[25] != 6:
        errors.append(f'Invalid image-set template RGBA PNG: {path.name}')
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
for character in ('neulbo', 'pelly'):
    try:
        with zipfile.ZipFile(ROOT / 'src/VoxPet.App/Assets/Samples' / f'companion-{character}.voxpet') as pack:
            m = json.loads(pack.read('manifest.json'))
            attachments = [json.loads(pack.read(p)) for p in m['attachments']]
            definition = json.loads(pack.read('character.json'))
            if (pack.testzip() is not None or m['id'] != f'voxpet.samples.{character}' or len(attachments) != 17
                    or len({a['category'] for a in attachments}) != 14 or len(m['actions']) != 9
                    or len(definition['parts']) != 6 or any(p['file'] != 'samples/empty.png' for p in definition['parts'])
                    or any(p.startswith('legacy/') or p.startswith('images/') for p in pack.namelist())):
                errors.append(f'Incomplete or non-accessory companion pack: {character}')
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error:
        errors.append(f'Cannot validate companion samples {character}: {type(error).__name__}')
for name in ('src/VoxPet.App/packages.lock.json', 'tests/VoxPet.Core.Tests/packages.lock.json', 'VoxPet.sln', 'global.json'):
    if not (ROOT / name).is_file(): errors.append(f'Missing release contract: {name}')
if errors:
    for error in errors: print(f'FAIL: {error}')
    sys.exit(1)
print('PASS: 36 RGBA PNG headers, two complete companion packs, pinned packages, no recording/network/blocking UI APIs')
