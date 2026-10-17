"""Continuous public companion choreography. No character pixels are read."""
import math

WEAPONS = ['toy-gun', 'toy-sword', 'magic-wand', 'firearm', 'bow', 'one-hand', 'two-hand']


def build_actions(files, manifest, character_id, anchors):
    side = character_id == 'pelly'
    def position(name):
        item = anchors[name]
        if item.get('parent') not in (None, 'root'):
            x, y = position(item['parent'])
        else:
            x, y = 0, 0
        return item.get('x', 0) + x, item.get('y', 0) + y

    for reference in manifest['actions']:
        action = files[reference]
        if action['id'] not in WEAPONS:
            action['interpolateTransforms'] = True
            continue
        kind = action['id']
        action.update(duration=1200, interpolateTransforms=True, timeline=[])
        events = action['timeline']
        for arm in ['right', 'left']:
            if arm == 'right' or kind in ['firearm', 'bow', 'two-hand']:
                events.append(dict(time=0, sprite=dict(target='arm-'+arm, file='parts/arm-'+arm+'-grip.png')))
        knots = []
        def pose(time, r, l=None, angle=0, left_angle=0, body=0, shoulder=0, record=True):
            if kind=='two-hand' and record:
                knots.append((time, r, l or position('leftHand'), angle, left_angle, body, shoulder))
                return
            if kind=='firearm': shoulder *= 1.2
            events.append(dict(time=time, transform=dict(target='body', rotation=body)))
            for hand, target, rotation, turn in [('right', r, angle, shoulder), ('left', l or position('leftHand'), left_angle, -shoulder*.35)]:
                sx, sy = position(hand+'Shoulder')
                hx, hy = position(hand+'Hand')
                a = math.radians(turn)
                dx, dy = target[0]-sx, target[1]-sy
                local_x = dx*math.cos(a)+dy*math.sin(a)-(hx-sx)
                local_y = -dx*math.sin(a)+dy*math.cos(a)-(hy-sy)
                events.extend([dict(time=time, transform=dict(target=hand+'Shoulder', rotation=turn)),
                               dict(time=time, transform=dict(target=hand+'Hand', x=round(local_x, 5), y=round(local_y, 5), rotation=rotation-turn))])
        rest = position('rightHand')
        left_rest = position('leftHand')
        grip = rest if kind != 'bow' else left_rest
        # Every exported action includes the same prop and grip that the normal app uses.
        asset = kind if kind != 'firearm' else 'pistol'
        for hand in ['right', 'left']:
            if hand == 'right' and kind!='bow' or hand == 'left' and kind in ['firearm', 'bow', 'two-hand'] or hand=='right' and kind=='bow':
                hx, hy = position(hand+'Hand')
                events.append(dict(time=0, effect=dict(anchor=hand+'Hand', file='samples/grip-back-'+hand+'.png', duration=1200, zIndex=1990, transform=dict(x=-hx, y=-hy))))
        events.append(dict(time=0, effect=dict(anchor='leftHand' if kind=='bow' else 'rightHand', file='samples/'+asset+'.png', duration=1200, transform=dict(x=-grip[0], y=-grip[1]))))
        for hand in ['right', 'left']:
            if hand == 'right' or kind in ['firearm', 'bow', 'two-hand']:
                hx, hy = position(hand+'Hand')
                events.append(dict(time=0, effect=dict(anchor=hand+'Hand', file='samples/grip-'+hand+'.png', duration=1200, zIndex=2010, transform=dict(x=-hx, y=-hy))))
        pose(0, rest)
        if kind in ['toy-gun', 'firearm']:
            aim = (292, 353) if side else (286, 354)
            support = (aim[0]+7, aim[1]+13) if kind=='firearm' else left_rest
            pose(260, aim, support, shoulder=18, body=-2)
            pose(360, aim, support, shoulder=18, body=-2)
            pose(405, (aim[0]+9, aim[1]-7), (support[0]+9, support[1]-7) if kind=='firearm' else support, angle=9, left_angle=9 if kind=='firearm' else 0, body=1, shoulder=24)
            pose(510, aim, support, shoulder=18, body=-2)
            pose(720, aim, support, shoulder=18, body=-2)
            pose(765, (aim[0]+7, aim[1]-5), support, angle=7, body=1, shoulder=22)
            pose(870, aim, support, shoulder=18, body=-2)
            for t in [360, 720]:
                events.append(dict(time=t, effect=dict(anchor='rightHand', file='samples/muzzle-flash.png', duration=65, zIndex=2020, transform=dict(x=-rest[0], y=-rest[1]))))
        elif kind in ['toy-sword', 'one-hand', 'two-hand']:
            two = kind=='two-hand'
            # Long hilt: the second fist remains 23px below the first in blade space.
            def slash(t, point, angle, body, turn):
                rad = math.radians(angle)
                support = (point[0]-23*math.sin(rad), point[1]+23*math.cos(rad)) if two else left_rest
                pose(t, point, support, angle, angle if two else 0, body, turn)
            slash(220, (258, 338) if two else (350, 335) if side else (345, 338), 30 if two else 35 if kind=='toy-sword' else 45, 4, -18 if two else -8 if kind=='toy-sword' else -12)
            slash(420, (265, 330) if two else (360, 320) if side else (353, 324), 25 if two else 40, 7, -20)
            slash(590, (240, 365) if two else (275, 365), -72 if two else -90, -6, 40)
            slash(770, (250, 385) if two else (262, 386), -110, -8, 45)
            slash(940, (270, 384) if two else (294, 384), -55, -3, 24)
        elif kind=='magic-wand':
            pose(280, (322, 342), angle=15, body=3, shoulder=-8)
            pose(500, (285, 360), angle=-65, body=-3, shoulder=30)
            pose(760, (285, 360), angle=-65, body=-3, shoulder=30)
            events.append(dict(time=500, effect=dict(anchor='rightHand', file='samples/cast-tip.png', duration=280, zIndex=2020, transform=dict(x=-rest[0], y=-rest[1]))))
        else:
            # Left fist holds the bow; right fist draws the string toward the cheek.
            hold = (224, 360) if side else (212, 366)
            pose(260, (hold[0]+28, hold[1]), hold, shoulder=15)
            pose(590, (hold[0]+102, hold[1]-8), hold, body=3, shoulder=-10)
            pose(720, (hold[0]+102, hold[1]-8), hold, body=3, shoulder=-10)
            pose(760, (hold[0]+35, hold[1]), hold, body=-1, shoulder=18)
            pose(900, (hold[0]+30, hold[1]), hold, shoulder=15)
            # Two thin PNG segments stretch between the bow tips and the drawing fist.
            for dy in [-97, 97]:
                events.append(dict(time=0, effect=dict(anchor='leftHand', endAnchor='rightHand', file='samples/bow-string.png', duration=900, zIndex=2002, transform=dict(x=63, y=dy))))
            events.append(dict(time=260, effect=dict(anchor='leftHand', endAnchor='rightHand', file='samples/arrow.png', duration=460, zIndex=2003, transform=dict(x=-65, y=-3), endTransform=dict(y=-3))))
            for t, x in [(0, hold[0]-65), (720, hold[0]-65), (900, -210)]:
                events.append(dict(time=t, transform=dict(target='arrow', x=x-256, y=hold[1]-3-350)))
            events.append(dict(time=720, effect=dict(anchor='arrow', file='samples/arrow.png', duration=180, zIndex=2003, transform=dict(scaleX=.30, scaleY=.6))))
        pose(1200, rest, left_rest)
        if kind=='two-hand':
            # Keep the supporting fist on the rotating long hilt between keys too.
            # Dense keys preserve the relation through the general opt-in interpolator.
            for t in sorted(set(range(0,1201,10)) | {k[0] for k in knots}):
                a = max((k for k in knots if k[0]<=t), key=lambda k:k[0])
                b = min((k for k in knots if k[0]>=t), key=lambda k:k[0])
                f = 0 if a[0]==b[0] else (t-a[0])/(b[0]-a[0])
                f = f*f*(3-2*f)
                blend = lambda x,y:x*(1-f)+y*f
                r = tuple(blend(x,y) for x,y in zip(a[1],b[1]))
                l = tuple(blend(x,y) for x,y in zip(a[2],b[2]))
                angle = blend(a[3],b[3])
                if 220<=t<=940:
                    rad = math.radians(angle)
                    l = (r[0]-23*math.sin(rad),r[1]+23*math.cos(rad))
                pose(t,r,l,angle,blend(a[4],b[4]),blend(a[5],b[5]),blend(a[6],b[6]),record=False)
        events.sort(key=lambda e:e['time'])
