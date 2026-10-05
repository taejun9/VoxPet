#!/usr/bin/env python3
"""Disposable Windows OBS QA: captures only synthetic VoxPet images, never audio/video recording."""
from __future__ import annotations
import argparse
import base64
import hashlib
import io
import json
import os
from pathlib import Path
import platform
import time
import websocket
from PIL import Image
import psutil


class ObsRpc:
    def __init__(self, password: str):
        self.socket = websocket.create_connection('ws://127.0.0.1:4455', timeout=8, http_no_proxy=['127.0.0.1'])
        hello = json.loads(self.socket.recv())['d']
        identify = {'rpcVersion': 1, 'eventSubscriptions': 0}
        auth = hello.get('authentication')
        if auth:
            secret = base64.b64encode(hashlib.sha256((password + auth['salt']).encode()).digest()).decode()
            identify['authentication'] = base64.b64encode(hashlib.sha256((secret + auth['challenge']).encode()).digest()).decode()
        self.socket.send(json.dumps({'op': 1, 'd': identify}))
        if json.loads(self.socket.recv())['op'] != 2:
            raise RuntimeError('OBS identification failed')
        self.sequence = 0

    def call(self, method: str, **params):
        self.sequence += 1
        request_id = str(self.sequence)
        self.socket.send(json.dumps({'op': 6, 'd': {'requestType': method, 'requestId': request_id, 'requestData': params}}))
        while True:
            message = json.loads(self.socket.recv())
            if message['op'] == 7 and message['d']['requestId'] == request_id:
                data = message['d']
                if not data['requestStatus']['result']:
                    raise RuntimeError(f"OBS {method} failed, code {data['requestStatus']['code']}")
                return data.get('responseData', {})

    def screenshot(self, name: str) -> Image.Image:
        data = self.call('GetSourceScreenshot', sourceName=name, imageFormat='png', imageWidth=480, imageHeight=480)['imageData']
        return Image.open(io.BytesIO(base64.b64decode(data.split(',', 1)[1]))).convert('RGBA')

    def close(self):
        self.socket.close()


def classify(image: Image.Image) -> dict:
    pixels = list(image.getdata())
    n = len(pixels)
    return {
        'size': list(image.size),
        'green_ratio': sum(g > 200 and r < 70 and b < 70 and a > 200 for r, g, b, a in pixels) / n,
        'purple_ratio': sum(b > r * 1.04 and r > g * 1.03 and r > 50 and a > 150 for r, g, b, a in pixels) / n,
        'transparent_ratio': sum(a < 20 for _, _, _, a in pixels) / n,
        'pixels_sha256': hashlib.sha256(image.tobytes()).hexdigest(),
    }


def select_window(rpc: ObsRpc, name: str):
    for _ in range(20):
        items = rpc.call('GetInputPropertiesListPropertyItems', inputName=name, propertyName='window')['propertyItems']
        # Never collect or write titles of unrelated applications.
        matches = [item for item in items if 'VoxPet Character' in str(item.get('itemName', '')) and not item.get('itemEnabled') is False]
        if matches:
            return matches[0]['itemValue']
        time.sleep(.5)
    raise RuntimeError('VoxPet Character is not available to OBS Window Capture')


def get_valid_capture(rpc: ObsRpc, name: str, green: bool = True):
    for _ in range(20):
        try:
            image = rpc.screenshot(name)
            metrics = classify(image)
            if metrics['purple_ratio'] > .03 and (not green or metrics['green_ratio'] > .2):
                return image, metrics
        except RuntimeError:
            pass
        time.sleep(.5)
    raise RuntimeError('No usable character frame from this capture method')


def resource_sample(process: psutil.Process, started: float):
    return {'seconds': round(time.monotonic() - started, 2), 'rss': process.memory_info().rss, 'handles': process.num_handles()}


def distinct_frames(rpc: ObsRpc, name: str, first_hash: str) -> int:
    hashes = {first_hash}
    for _ in range(6):
        time.sleep(.25)
        hashes.add(classify(rpc.screenshot(name))['pixels_sha256'])
    if len(hashes) < 2:
        raise RuntimeError('Character capture is frozen')
    return len(hashes)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--voxpet-pid', type=int, required=True)
    parser.add_argument('--long-run-minutes', type=int, default=0)
    parser.add_argument('--background', choices=['green', 'transparent'], default='green')
    args = parser.parse_args()
    if os.name != 'nt':
        parser.error('This QA requires disposable Windows and OBS processes')
    args.output.mkdir(parents=True, exist_ok=True)
    report = {'os': platform.platform(), 'source_commit': os.environ.get('GITHUB_SHA'), 'background': args.background, 'synthetic_only': True, 'recording': False, 'streaming': False, 'methods': [], 'errors': []}
    rpc = None
    try:
        for _ in range(60):
            try:
                rpc = ObsRpc(os.environ['VOXPET_OBS_QA_PASSWORD'])
                break
            except (OSError, websocket.WebSocketException):
                time.sleep(1)
        if rpc is None:
            raise RuntimeError('OBS WebSocket unavailable; inspect OBS renderer/startup state')
        version = rpc.call('GetVersion')
        report['obs_version'] = version['obsVersion']
        report['websocket_version'] = version['obsWebSocketVersion']
        if rpc.call('GetRecordStatus')['outputActive'] or rpc.call('GetStreamStatus')['outputActive']:
            raise RuntimeError('Unexpected recording or streaming activity')
        scene = 'VoxPetQA'
        if scene not in [item['sceneName'] for item in rpc.call('GetSceneList')['scenes']]:
            rpc.call('CreateScene', sceneName=scene)
        rpc.call('SetCurrentProgramScene', sceneName=scene)
        name = 'VoxPet-QA-Window'
        if any(item['inputName'] == name for item in rpc.call('GetInputList')['inputs']):
            rpc.call('RemoveInput', inputName=name)
        rpc.call('CreateInput', sceneName=scene, inputName=name, inputKind='window_capture', inputSettings={'capture_audio': False, 'cursor': False, 'client_area': True}, sceneItemEnabled=True)
        inputs = rpc.call('GetInputList')['inputs']
        if any(item['inputKind'] in ('wasapi_input_capture', 'wasapi_output_capture', 'wasapi_process_output_capture') for item in inputs):
            raise RuntimeError('Audio source found in isolated OBS QA scene')
        window_value = select_window(rpc, name)
        kinds = rpc.call('GetSourceFilterKindList')['sourceFilterKinds']
        key_kind = next((kind for kind in kinds if kind.startswith('chroma_key_filter')), None)
        if key_kind is None:
            raise RuntimeError('Chroma Key filter unavailable')
        methods = ((1, 'BitBlt'), (2, 'WGC')) if args.background == 'green' else ((2, 'WGC'),)
        for method_id, label in methods:
            result = {'method': label}
            report['methods'].append(result)
            try:
                rpc.call('SetInputSettings', inputName=name, inputSettings={'window': window_value, 'method': method_id, 'capture_audio': False, 'cursor': False, 'client_area': True}, overlay=True)
                raw, metrics = get_valid_capture(rpc, name, green=args.background == 'green')
                raw.save(args.output / f'{label}-raw.png')
                result['raw'] = metrics
                result['distinct_frames'] = distinct_frames(rpc, name, metrics['pixels_sha256'])
                if args.background == 'transparent':
                    result['native_alpha_preserved'] = metrics['transparent_ratio'] > .2
                    result['passed'] = True
                    continue
                result['corner_green_ratio'] = classify(raw.crop((450, 450, 480, 480)))['green_ratio']
                if result['corner_green_ratio'] < .995:
                    raise RuntimeError('Broadcast corner contains unwanted controls')
                rpc.call('CreateSourceFilter', sourceName=name, filterName='VoxPet-QA-Key', filterKind=key_kind, filterSettings={'key_color_type': 'green', 'similarity': 400, 'smoothness': 80, 'spill': 100})
                time.sleep(.5)
                keyed = rpc.screenshot(name)
                keyed.save(args.output / f'{label}-keyed.png')
                keyed_metrics = classify(keyed)
                result['keyed'] = keyed_metrics
                if keyed_metrics['transparent_ratio'] < .2 or keyed_metrics['purple_ratio'] < metrics['purple_ratio'] * .65:
                    raise RuntimeError('Chroma Key did not preserve character and remove background')
                corner = keyed.crop((450, 450, 480, 480))
                result['corner_transparent_ratio'] = sum(pixel[3] < 20 for pixel in corner.getdata()) / 900
                if result['corner_transparent_ratio'] < .95:
                    raise RuntimeError('Broadcast corner contains unwanted controls')
                result['passed'] = True
            except Exception as error:
                result['passed'] = False
                result['error'] = str(error)
            finally:
                filters = rpc.call('GetSourceFilterList', sourceName=name)['filters']
                if any(item['filterName'] == 'VoxPet-QA-Key' for item in filters):
                    rpc.call('RemoveSourceFilter', sourceName=name, filterName='VoxPet-QA-Key')
        if not any(result.get('passed') for result in report['methods']):
            raise RuntimeError('No Window Capture method passed on this Windows renderer')
        passed_method = next(result['method'] for result in report['methods'] if result.get('passed'))
        rpc.call('SetInputSettings', inputName=name, inputSettings={'method': 1 if passed_method == 'BitBlt' else 2}, overlay=True)
        if args.long_run_minutes:
            if args.long_run_minutes < 60:
                raise RuntimeError('Long-run evidence requires at least 60 measured minutes')
            process = psutil.Process(args.voxpet_pid)
            started = time.monotonic()
            samples = []
            report['long_run'] = {'state': 'running', 'warmup_minutes': 10, 'requested_minutes': args.long_run_minutes, 'capture_method': passed_method, 'samples': samples, 'motion_checks': 0}
            baseline = None
            while True:
                samples.append(resource_sample(process, started))
                if baseline is None and samples[-1]['seconds'] >= 600:
                    baseline = samples[-1]
                if baseline is not None and samples[-1]['seconds'] - baseline['seconds'] >= args.long_run_minutes * 60:
                    break
                if len(samples) % 6 == 0:
                    print(f"Synthetic QA: {samples[-1]['seconds'] / 60:.1f} minutes elapsed", flush=True)
                    _, current_metrics = get_valid_capture(rpc, name)
                    distinct_frames(rpc, name, current_metrics['pixels_sha256'])
                    report['long_run']['motion_checks'] += 1
                    (args.output / 'result.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
                time.sleep(10)
            measured = [sample for sample in samples if sample['seconds'] >= 600]
            first, last = measured[0], measured[-1]
            final_image, final_metrics = get_valid_capture(rpc, name)
            final_image.save(args.output / 'long-run-final.png')
            report['long_run'].update({'state': 'completed', 'measured_minutes': (last['seconds'] - first['seconds']) / 60, 'final_capture': final_metrics, 'rss_growth': last['rss'] - first['rss'], 'handle_growth': last['handles'] - first['handles']})
            if last['rss'] - first['rss'] > 50 * 1024 * 1024 or last['handles'] - first['handles'] > 50:
                raise RuntimeError('Synthetic UI resource growth exceeds planned threshold')
        report['passed'] = True
        if rpc.call('GetRecordStatus')['outputActive'] or rpc.call('GetStreamStatus')['outputActive']:
            raise RuntimeError('Unexpected recording or streaming activity at completion')
        return 0
    except Exception as error:
        report['passed'] = False
        report['errors'].append(str(error))
        print(f'OBS QA failed: {error}', flush=True)
        return 1
    finally:
        (args.output / 'result.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
        if rpc is not None:
            rpc.close()


if __name__ == '__main__':
    raise SystemExit(main())
