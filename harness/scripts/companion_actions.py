"""Continuous public companion choreography. No character pixels are read."""
import math

def sample(knots, time, column):
    """C1 shape-preserving curve. Deliberate holds/reversals have zero velocity."""
    i = max(i for i, key in enumerate(knots) if key[0] <= time)
    if i == len(knots)-1:
        return knots[i][column]
    a, b = knots[i], knots[i+1]
    h = b[0]-a[0]; t = (time-a[0])/h
    d = (b[column]-a[column])/h
    def tangent(d0, d1, h0, h1):
        if d0*d1 <= 0:
            return 0
        w0, w1 = 2*h1+h0, h1+2*h0
        return (w0+w1)/(w0/d0+w1/d1)
    m0 = 0 if i == 0 else tangent((a[column]-knots[i-1][column])/(a[0]-knots[i-1][0]), d, a[0]-knots[i-1][0], h)
    m1 = 0 if i+2 == len(knots) else tangent(d, (knots[i+2][column]-b[column])/(knots[i+2][0]-b[0]), h, knots[i+2][0]-b[0])
    ease = t*t*(3-2*t)
    return a[column]*(1-ease)+b[column]*ease+(t*t*t-2*t*t+t)*h*m0+(t*t*t-t*t)*h*m1


def add_secondary(events, duration, side):
    # Low-amplitude overlapping follow-through, with value/velocity zero at both ends.
    # The body compresses around its lower pivot; compensate the feet to keep the baseline.
    body = {e['time']:e['transform'] for e in events if e.get('transform',{}).get('target')=='body'}
    for time in range(0, duration+1, 10):
        u=time/duration; envelope=math.sin(math.pi*u)**2
        phase=2*math.pi*u
        squash=(.009 if side else .014)*envelope*math.sin(phase-.25)
        turn=(.65 if side else .55)*body.get(time,{}).get('rotation',0)
        transform=body.get(time)
        if transform is None:
            transform=dict(target='body');events.append(dict(time=time,transform=transform))
        transform.update(rotation=round(turn,6), x=round((2.3 if side else 1.4)*envelope*math.sin(phase),6),
                         y=round((112 if side else 115)*squash,6),scaleX=round(1+squash*.55,7),scaleY=round(1-squash,7))
        events.append(dict(time=time,transform=dict(target='head',rotation=round(-turn*.35+(2.0 if side else 2.8)*envelope*math.sin(phase-.6),6),
                                                    y=round((1.1 if side else 1.7)*envelope*math.sin(phase-.4),6))))


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
            kind = action['id']
            duration = 1000 if kind=='claw' else 1200
            action.update(duration=duration, interpolateTransforms=True, transformInterpolation='monotoneCubic', timeline=[])
            events = action['timeline']
            rest = position('rightHand')
            aim = (310,355) if side else (300,350)
            turn = 52 if kind=='claw' else 24
            if side:
                sx, sy = position('rightShoulder'); hx, hy = position('rightHand')
                turn = math.degrees(math.atan2(aim[1]-sy, aim[0]-sx)-math.atan2(hy-sy,hx-sx))
            arrive = (190 if side else 240) if kind=='claw' else (270 if side else 320)
            gesture = [(0,*rest,0),(80,rest[0]+(3 if side else -3),rest[1]+3,-3),
                       (arrive,*aim,turn),(arrive+90,aim[0]-2,aim[1]-2,turn+2),
                       (700 if kind=='claw' else 780,*aim,turn),
                       (duration-110,rest[0]-2,rest[1]+1,-1),(duration,*rest,0)]
            for time in range(0,duration+1,10):
                target=(sample(gesture,time,1),sample(gesture,time,2));angle=sample(gesture,time,3)
                if side:
                    sx,sy=position('rightShoulder');hx,hy=rest
                    angle=math.degrees(math.atan2(target[1]-sy,target[0]-sx)-math.atan2(hy-sy,hx-sx))
                sx,sy = position('rightShoulder');hx,hy = rest;a = math.radians(angle)
                dx,dy = target[0]-sx,target[1]-sy
                events.extend([dict(time=time,transform=dict(target='rightShoulder',rotation=angle)),
                               dict(time=time,transform=dict(target='rightHand',x=round(dx*math.cos(a)+dy*math.sin(a)-(hx-sx),5),y=round(-dx*math.sin(a)+dy*math.cos(a)-(hy-sy),5),rotation=-angle))])
            add_secondary(events,duration,side)
            events.append(dict(time=0,sprite=dict(target='arm-right',file='parts/arm-right-grip.png')))
            events.append(dict(time=0,effect=dict(anchor='rightHand',file='samples/grip-back-right.png',duration=duration,zIndex=1990,transform=dict(x=-rest[0],y=-rest[1]))))
            events.append(dict(time=160 if kind=='claw' else 0,effect=dict(anchor='rightHand',file='samples/'+kind+'.png',duration=620 if kind=='claw' else duration,zIndex=2010,transform=dict(x=-rest[0],y=-rest[1]))))
            events.sort(key=lambda e:e['time'])
            continue
        kind = action['id']
        action.update(duration=1200, interpolateTransforms=True, transformInterpolation='monotoneCubic', timeline=[])
        events = action['timeline']
        for arm in ['right', 'left']:
            if arm == 'right' or kind in ['firearm', 'bow', 'two-hand']:
                events.append(dict(time=0, sprite=dict(target='arm-'+arm, file='parts/arm-'+arm+'-grip.png')))
        knots = []
        def pose(time, r, l=None, angle=0, left_angle=0, body=0, shoulder=0, record=True):
            if side and kind=='two-hand' and 220 <= time <= 940:
                rad = math.radians(angle)
                l = (r[0]-23*math.sin(rad), r[1]+23*math.cos(rad))
            if record:
                knots.append((time, *r, *(l or position('leftHand')), angle, left_angle, body, shoulder))
                return
            if kind=='firearm': shoulder *= 1.2
            events.append(dict(time=time, transform=dict(target='body', rotation=body)))
            for hand, target, rotation, turn in [('right', r, angle, shoulder), ('left', l or position('leftHand'), left_angle, -shoulder*.35)]:
                sx, sy = position(hand+'Shoulder')
                hx, hy = position(hand+'Hand')
                if side:
                    turn = math.degrees(math.atan2(target[1]-sy,target[0]-sx)-math.atan2(hy-sy,hx-sx))
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
        pose(90 if side else 110, (rest[0]+(3 if side else -4),rest[1]+3), body=1 if side else -1, shoulder=-2)
        if kind in ['toy-gun', 'firearm']:
            aim = (292, 353) if side else (286, 354)
            support = (aim[0]+7, aim[1]+13) if kind=='firearm' else left_rest
            pose(260, aim, support, shoulder=18, body=-2)
            pose(360, aim, support, shoulder=18, body=-2)
            pose(405, (aim[0]+9, aim[1]-7), (support[0]+9, support[1]-7) if kind=='firearm' else support, angle=9, left_angle=9 if kind=='firearm' else 0, body=1, shoulder=24)
            pose(510, aim, support, shoulder=18, body=-2)
            pose(720, aim, support, shoulder=18, body=-2)
            pose(765, (aim[0]+7, aim[1]-5), (support[0]+7, support[1]-5) if kind=='firearm' else support, angle=7, left_angle=7 if kind=='firearm' else 0, body=1, shoulder=22)
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
        # Character-specific settling before the exact rest key; no snap back to idle.
        # Pelly settles quickly with a smaller counter-movement; Neulbo has a softer tail.
        settle=1080 if side else 1090
        rest_key=(settle,rest[0]-(1.2 if side else 2),rest[1]+.8,left_rest[0],left_rest[1],-1,0,-.3,0)
        knots.insert(-1,rest_key)
        for t in sorted(set(range(0,1201,10)) | {k[0] for k in knots}):
            values=[sample(knots,t,i) for i in range(1,9)]
            r,l=tuple(values[:2]),tuple(values[2:4]);angle=values[4]
            if kind=='two-hand' and 220<=t<=940:
                rad=math.radians(angle);l=(r[0]-23*math.sin(rad),r[1]+23*math.cos(rad))
            pose(t,r,l,*values[4:],record=False)
        add_secondary(events,1200,side)
        events.sort(key=lambda e:e['time'])
