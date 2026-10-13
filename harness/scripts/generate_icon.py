"""Convert the user-supplied artwork to app PNG and seven Windows ICO sizes.

Requires Pillow for regeneration only. Preserve the source canvas, colors and
background; never redraw the artwork or remove its margins.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2] / 'src/VoxPet.App/Assets'
SIZES = (16, 24, 32, 48, 64, 128, 256)


def main():
    with Image.open(ROOT / 'VoxPet-source.png') as source:
        if source.width != source.height:
            raise ValueError('Icon artwork must have a square canvas')
        image = source.convert('RGBA').resize((256, 256), Image.Resampling.LANCZOS)
    image.save(ROOT / 'VoxPet.png')
    image.save(ROOT / 'VoxPet.ico', sizes=[(size, size) for size in SIZES])
    print('Generated source-preserving PNG and seven-size ICO')


if __name__ == '__main__':
    main()
