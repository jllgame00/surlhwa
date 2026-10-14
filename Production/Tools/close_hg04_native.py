"""Requirement B only; never calls into Unity or writes game state.

Windows SendInput scan codes and absolute cursor motion are the only tested inputs.
The existing Player probe and durable save are read-only observations. Low-level
hooks audit tagged injected events and abort on unrelated input. No refocusing.
"""
import ctypes as C
from ctypes import wintypes as W
import datetime as dt
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import threading
import time
import uuid

ROOT = Path(__file__).resolve().parents[2]
NAME = sys.argv[1]
OUT = ROOT / 'Production/Validation/Evidence' / NAME
EXE = ROOT / 'Production/Validation/Builds/Slice/SurlhwaSlice.exe'
SAVE = Path('C:/Users/surlw/.codex/visualizations/2026/10/10/01a1278f-2182-70e1-b942-d2436ca59b10/SliceValidation') / NAME
OUT.mkdir(exist_ok=False)
if SAVE.exists():
    raise RuntimeError('Fresh isolated save required')
U = C.WinDLL('user32', use_last_error=True)
K = C.WinDLL('kernel32', use_last_error=True)
ULONG_PTR = C.c_size_t
LRESULT = C.c_ssize_t
TAG = 0x5355524C

class MI(C.Structure):
    _fields_ = [('dx', W.LONG), ('dy', W.LONG), ('mouseData', W.DWORD), ('dwFlags', W.DWORD), ('time', W.DWORD), ('dwExtraInfo', ULONG_PTR)]
class KI(C.Structure):
    _fields_ = [('wVk', W.WORD), ('wScan', W.WORD), ('dwFlags', W.DWORD), ('time', W.DWORD), ('dwExtraInfo', ULONG_PTR)]
class IU(C.Union):
    _fields_ = [('mi', MI), ('ki', KI)]
class INPUT(C.Structure):
    _anonymous_ = ('u',)
    _fields_ = [('type', W.DWORD), ('u', IU)]
class KH(C.Structure):
    _fields_ = [('vkCode', W.DWORD), ('scanCode', W.DWORD), ('flags', W.DWORD), ('time', W.DWORD), ('dwExtraInfo', ULONG_PTR)]
class MH(C.Structure):
    _fields_ = [('pt', W.POINT), ('mouseData', W.DWORD), ('flags', W.DWORD), ('time', W.DWORD), ('dwExtraInfo', ULONG_PTR)]
assert C.sizeof(INPUT) == 40, C.sizeof(INPUT)
HOOK = C.WINFUNCTYPE(LRESULT, C.c_int, W.WPARAM, W.LPARAM)
ENUM = C.WINFUNCTYPE(W.BOOL, W.HWND, W.LPARAM)
U.SendInput.argtypes = [W.UINT, C.POINTER(INPUT), C.c_int]
U.SendInput.restype = W.UINT
U.GetForegroundWindow.restype = W.HWND
U.GetWindowThreadProcessId.argtypes = [W.HWND, C.POINTER(W.DWORD)]
U.GetClientRect.argtypes = [W.HWND, C.POINTER(W.RECT)]
U.ClientToScreen.argtypes = [W.HWND, C.POINTER(W.POINT)]
U.WindowFromPoint.argtypes = [W.POINT]
U.WindowFromPoint.restype = W.HWND
U.GetAncestor.argtypes = [W.HWND, W.UINT]
U.GetAncestor.restype = W.HWND
U.IsWindowVisible.argtypes = [W.HWND]
U.EnumWindows.argtypes = [ENUM, W.LPARAM]
U.SetWindowsHookExW.argtypes = [C.c_int, HOOK, W.HINSTANCE, W.DWORD]
U.SetWindowsHookExW.restype = W.HHOOK
U.CallNextHookEx.argtypes = [W.HHOOK, C.c_int, W.WPARAM, W.LPARAM]
U.CallNextHookEx.restype = LRESULT
U.UnhookWindowsHookEx.argtypes = [W.HHOOK]
U.PostThreadMessageW.argtypes = [W.DWORD, W.UINT, W.WPARAM, W.LPARAM]
U.SetProcessDpiAwarenessContext.argtypes = [C.c_void_p]
U.SetProcessDpiAwarenessContext(C.c_void_p(-4))
K.GetModuleHandleW.argtypes = [W.LPCWSTR]
K.GetModuleHandleW.restype = W.HMODULE
K.OpenProcess.argtypes = [W.DWORD, W.BOOL, W.DWORD]
K.OpenProcess.restype = W.HANDLE
K.QueryFullProcessImageNameW.argtypes = [W.HANDLE, W.DWORD, W.LPWSTR, C.POINTER(W.DWORD)]
K.CloseHandle.argtypes = [W.HANDLE]
K.CreateFileW.argtypes = [W.LPCWSTR, W.DWORD, W.DWORD, C.c_void_p, W.DWORD, W.DWORD, W.HANDLE]
K.CreateFileW.restype = W.HANDLE
K.ReadFile.argtypes = [W.HANDLE, C.c_void_p, W.DWORD, C.POINTER(W.DWORD), C.c_void_p]

lock = threading.Lock()
events = []
armed = False
interrupted = None
hook_ready = threading.Event()
hook_tid = 0
hook_error = None
held = set()
mouse_held = False
pid = hwnd = 0
rect0 = None
gesture_rect = None
gesture_name = None
gesture_point = None
last_observed = None
last_guard = 0
requirement = 'B complete native Stir service'
boundary = 'Harness prepared; no Player input issued'
player = None
result = {'status': 'IN_PROGRESS', 'requirement': requirement, 'evidence': str(OUT),
          'acceptedEnterEvidence': 'hg04-sendinput-02/enter-result.json; not rerun'}

def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()

def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def log(kind, **values):
    with lock:
        events.append(dict(utc=utc(), monotonic=time.perf_counter(), kind=kind, **values))

def flush_events():
    with lock:
        text = '\n'.join(json.dumps(e, ensure_ascii=False) for e in events) + '\n'
    (OUT / 'native-events.jsonl').write_text(text, encoding='utf-8')

def shared_read(path):
    # FILE_SHARE_READ | WRITE | DELETE permits the Player's atomic File.Replace.
    handle = K.CreateFileW(str(path), 0x80000000, 7, None, 3, 0x80, None)
    if handle == C.c_void_p(-1).value:
        raise FileNotFoundError(path)
    try:
        data, count = C.create_string_buffer(65536), W.DWORD()
        if not K.ReadFile(handle, data, 65536, C.byref(count), None):
            raise OSError(C.get_last_error(), 'Read-only observation failed')
        return data.raw[:count.value].decode('utf-8-sig')
    finally:
        K.CloseHandle(handle)

def owner(window):
    n = W.DWORD()
    U.GetWindowThreadProcessId(window, C.byref(n))
    return n.value

def region():
    r, p = W.RECT(), W.POINT()
    if not U.GetClientRect(hwnd, C.byref(r)) or not U.ClientToScreen(hwnd, C.byref(p)):
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: target window disappeared')
    return p.x, p.y, r.right, r.bottom

def guard(point=None, record=False):
    global last_guard, rect0
    if interrupted:
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: ' + str(interrupted))
    fg = U.GetForegroundWindow()
    r = region()
    client = point or gesture_point or (r[2] // 2, r[3] // 2)
    x, y = r[0] + round(client[0]), r[1] + round(client[1])
    hit = U.GetAncestor(U.WindowFromPoint(W.POINT(x, y)), 2)
    values = dict(pid=pid, hwnd=hwnd, foregroundHwnd=fg, foregroundPid=owner(fg), region=r,
                  clientPoint=client, screenPoint=[x, y], hitRoot=hit, activeGesture=gesture_name,
                  gestureStartRegion=gesture_rect)
    if fg != hwnd or owner(fg) != pid or owner(hwnd) != pid or hit != hwnd or r[2:] != (1280, 720):
        log('ownership_lost', **values)
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: process/window ownership, target coverage, or client size changed: ' + json.dumps(values))
    if gesture_rect is not None and r != gesture_rect:
        log('gesture_region_changed', **values)
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: window moved during active ' + str(gesture_name) + ': ' + json.dumps(values))
    if r != rect0:
        log('idle_reanchor', previousRegion=rect0, currentRegion=r, hwnd=hwnd, pid=pid)
        rect0 = r
    if record or time.perf_counter() - last_guard > .1:
        log('ownership', **values)
        last_guard = time.perf_counter()
    return values

def begin_segment(name, point=None):
    global gesture_rect, gesture_name, gesture_point
    if gesture_rect is not None:
        raise RuntimeError('Harness nested native segment')
    current = guard(point, record=True)
    gesture_rect, gesture_name, gesture_point = tuple(current['region']), name, point
    log('segment_begin', name=name, clientPoint=point, clientRegion=gesture_rect)

def end_segment():
    global gesture_rect, gesture_name, gesture_point
    guard(record=True)
    log('segment_end', name=gesture_name, clientRegion=gesture_rect)
    gesture_rect = gesture_name = gesture_point = None

def check_control(label, phase, point):
    o = wait_for(lambda o: o['phase'] == phase and not o['modal'] and label in o['visibleText'],
                 'Expected production control: ' + label)
    current = guard(point, record=True)
    log('control_verified', label=label, phase=phase, clientPoint=point,
        clientRegion=current['region'], observedUtc=o['observedUtc'],
        layout='SliceUi fixed 1280x720 authored control rectangle; active production label and phase verified')

def key_control(vk):
    controls = {0x51: ('따르기  Q', (294,605)), 0x45: ('지거 이동  E', (422,605)),
                0x52: ('거르기  R', (550,605)), 0x49: ('얼음  I', (808,605)),
                0x4A: ('대상 변경  J', (307,661)), 0x53: ('젓기  S + 원', (681,605)),
                0x31: ('1  위스키', (116,186.5)), 0x32: ('2  버무스', (116,226.5)),
                0x33: ('3  물', (116,266.5))}
    label, point = controls[vk]
    check_control(label, 'Brewing', point)
    return point

def hook_thread():
    global interrupted, hook_tid, hook_error
    hook_tid = K.GetCurrentThreadId()
    @HOOK
    def callback(code, message, data):
        global interrupted
        if code >= 0 and armed:
            keyboard = message in (0x100, 0x101, 0x104, 0x105)
            v = C.cast(data, C.POINTER(KH if keyboard else MH)).contents
            own = v.dwExtraInfo == TAG
            fields = dict(message=int(message), flags=int(v.flags), tag=int(v.dwExtraInfo), own=own)
            if keyboard:
                fields.update(vk=int(v.vkCode), scan=int(v.scanCode))
            else:
                fields.update(x=v.pt.x, y=v.pt.y)
            log('keyboard_hook' if keyboard else 'mouse_hook', **fields)
            if not own and interrupted is None:
                interrupted = dict(reason='Unrelated physical or external input detected', **fields)
        return U.CallNextHookEx(None, code, message, data)
    hooks = [U.SetWindowsHookExW(t, callback, K.GetModuleHandleW(None), 0) for t in (13, 14)]
    if not all(hooks):
        hook_error = C.get_last_error()
    hook_ready.set()
    msg = W.MSG()
    while all(hooks) and U.GetMessageW(C.byref(msg), None, 0, 0) > 0:
        U.TranslateMessage(C.byref(msg))
        U.DispatchMessageW(C.byref(msg))
    for h in hooks:
        if h:
            U.UnhookWindowsHookEx(h)

def observe():
    global last_observed
    try:
        o = json.loads(shared_read(OUT / 'observation.json'))
    except (FileNotFoundError, json.JSONDecodeError, PermissionError):
        return None
    if o['observedUtc'] != last_observed:
        last_observed = o['observedUtc']
        log('production_observation', observation=o)
    if o.get('nativeInterrupted'):
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: production probe observed focus loss')
    return o

def pause(seconds):
    end = time.perf_counter() + seconds
    while time.perf_counter() < end:
        guard()
        observe()
        time.sleep(min(.01, max(0, end - time.perf_counter())))

def wait_for(predicate, description, timeout=8):
    global boundary
    end = time.perf_counter() + timeout
    while time.perf_counter() < end:
        guard()
        o = observe()
        if o and predicate(o) and not o['saving']:
            log('boundary', description=description, observation=o)
            boundary = description
            return o
        time.sleep(.02)
    raise RuntimeError('Missing boundary: ' + description)

def emit(i, description, cleanup=False):
    before = guard(record=True) if not cleanup else {'foregroundPid': owner(U.GetForegroundWindow())}
    C.set_last_error(0)
    count = U.SendInput(1, C.byref(i), C.sizeof(INPUT))
    error = C.get_last_error()
    log('SendInput', description=description, issued=count, winError=error, before=before, foregroundPidAfter=owner(U.GetForegroundWindow()), cleanup=cleanup)
    if count != 1:
        raise RuntimeError('OS SendInput rejected: ' + description + ' error=' + str(error))

def key(vk, down, cleanup=False):
    i = INPUT(type=1)
    i.ki = KI(0, U.MapVirtualKeyW(vk, 0), 8 | (0 if down else 2), 0, TAG)
    emit(i, f'key {vk:02X} ' + ('down' if down else 'up'), cleanup)
    if down:
        held.add(vk)
    else:
        held.discard(vk)

def tap(vk, duration=.12):
    point = key_control(vk)
    begin_segment('key tap ' + hex(vk), point)
    key(vk, True)
    pause(duration)
    key(vk, False)
    end_segment()
    pause(.15)

def hold(vk, duration, fine=False):
    point = key_control(vk)
    begin_segment('held tool ' + hex(vk), point)
    if fine:
        key(0xA0, True)
    key(vk, True)
    pause(duration)
    key(vk, False)
    if fine:
        key(0xA0, False)
    end_segment()
    pause(.7)

def move(x, y):
    current = guard((x, y))
    r = current['region']
    sx, sy = r[0] + round(x), r[1] + round(y)
    vx, vy, vw, vh = (U.GetSystemMetrics(n) for n in (76, 77, 78, 79))
    i = INPUT(type=0)
    i.mi = MI(round((sx-vx)*65535/(vw-1)), round((sy-vy)*65535/(vh-1)), 0, 0xC001, 0, TAG)
    emit(i, 'absolute cursor')
    p = W.POINT()
    U.GetCursorPos(C.byref(p))
    log('cursor', requestedClient=[x,y], actualClient=[p.x-r[0],p.y-r[1]],
        clientRegion=r, requestedScreen=[sx,sy], actualScreen=[p.x,p.y])

def click(x, y, label, phase):
    global mouse_held
    check_control(label, phase, (x,y))
    begin_segment('click ' + label, (x,y))
    move(x, y)
    pause(.08)
    for flags in (2, 4):
        i = INPUT(type=0)
        i.mi = MI(0, 0, 0, flags, 0, TAG)
        emit(i, 'left mouse ' + ('down' if flags == 2 else 'up'))
        mouse_held = flags == 2
        pause(.12)
    end_segment()

def capture(label):
    from PIL import ImageGrab
    guard(record=True)
    x, y, w, h = rect0
    ImageGrab.grab(bbox=(x, y, x+w, y+h), all_screens=True).save(OUT / (label + '.png'))
    write(label + '.json', observe())
    guard()

def snapshot():
    return json.loads((SAVE / 'slot1/current.json').read_text(encoding='utf-8-sig'))

try:
    live = subprocess.check_output(['powershell', '-NoProfile', '-Command', '(Get-Process SurlhwaSlice -ErrorAction SilentlyContinue).Id'], text=True).strip()
    if live:
        raise RuntimeError('Existing Player: ' + live)
    run = uuid.uuid4().hex
    args = [str(EXE), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-sliceEvidenceRoot', str(OUT), '-sliceSaveRoot', str(SAVE), '-sliceRunId', run, '-logFile', str(OUT / 'Player.log')]
    player = subprocess.Popen(args)
    pid = player.pid
    end = time.perf_counter() + 40
    while time.perf_counter() < end:
        windows = []
        @ENUM
        def enum(w, unused):
            if owner(w) == pid and U.IsWindowVisible(w):
                r = W.RECT()
                U.GetClientRect(w, C.byref(r))
                if r.right == 1280 and r.bottom == 720:
                    windows.append(w)
            return True
        U.EnumWindows(enum, 0)
        if len(windows) == 1:
            hwnd = windows[0]
            if (OUT / 'observation.json').exists():
                break
        if player.poll() is not None:
            raise RuntimeError('Player exited before native check')
        time.sleep(.1)
    if not hwnd:
        raise RuntimeError('No unique expected Player window')
    handle = K.OpenProcess(0x1000, False, pid)
    buf, size = C.create_unicode_buffer(32768), W.DWORD(32768)
    if not K.QueryFullProcessImageNameW(handle, 0, buf, C.byref(size)):
        raise RuntimeError('Cannot verify actual executable')
    K.CloseHandle(handle)
    if Path(buf.value).resolve() != EXE.resolve():
        raise RuntimeError('Executable mismatch')
    rect0 = region()
    write('identity.json', dict(utc=utc(), runId=run, pid=pid, hwnd=hwnd, executable=buf.value, executableSha256=hashlib.sha256(EXE.read_bytes()).hexdigest(), arguments=args, clientRegion=rect0, saveRoot=str(SAVE), harnessSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()))
    thread = threading.Thread(target=hook_thread, daemon=True)
    thread.start()
    if not hook_ready.wait(3) or hook_error:
        raise RuntimeError('Cannot audit native input hooks: ' + str(hook_error))
    if any(U.GetAsyncKeyState(vk) & 0x8000 for vk in range(1, 255)):
        raise RuntimeError('ENVIRONMENT_INTERRUPTED: key/button already held before arming')
    guard(record=True)
    armed = True
    wait_for(lambda o: o['phase'] == 'menu' and o['focused'] and o['nativeOwnershipEstablished'] and o['processId'] == pid, 'Fresh foreground menu')
    capture('01-menu')
    click(430, 482, '새로 시작', 'menu')
    wait_for(lambda o: o['phase'] == 'Welcome', 'Native New Game pointer activation for B setup')
    click(969, 669, '계속  Enter', 'Welcome')
    wait_for(lambda o: o['phase'] == 'Intro' and o['sequence'] == 3, 'Ordinary guest arrived')
    for phase, seq in [('Intro', 4), ('Request', 5), ('Brewing', 6)]:
        current_phase = observe()['phase']
        click(969, 669, 'Stir 준비' if current_phase == 'Request' else '계속  Enter', current_phase)
        wait_for(lambda o: o['phase'] == phase and o['sequence'] == seq, 'Normal reading progression ' + phase)
    capture('03-brewing')
    print('B_ONLY: normal native progression reached Brewing', flush=True)
    boundary = 'Normal production controls reached Brewing for first postman service'
    for digit, ingredient, target in [(0x31, 'whiskey', 59.14706), (0x32, 'vermouth', 29.57353), (0x33, 'water', 29.57353)]:
        tap(digit)
        wait_for(lambda o: o['ingredient'] == ingredient and o['target'] == 'Jigger', 'Selected ' + ingredient)
        hold(0x51, (target - 1.2) / 30)
        for fine_step in range(4):
            current = observe()['jigger']
            delta = target-current
            if abs(delta) <= .3:
                break
            if delta <= 0:
                raise RuntimeError('Measured pour exceeded intended amount: ' + str(current))
            hold(0x51, max(.025, (delta-.10)/5), fine=True)
        measured = observe()['jigger']
        if abs(measured-target) > .3:
            raise RuntimeError('Native measurement outside target tolerance')
        log('measured_ingredient', ingredient=ingredient, target=target, actual=measured)
        hold(0x45, 1.75)
        wait_for(lambda o: o['jigger'] < .001, 'Jigger emptied through native E transfer')
    tap(0x4A)
    wait_for(lambda o: o['target'] == 'Mixing', 'Mixing target selected')
    tap(0x49)
    pause(.6)
    capture('04-measured-ice')
    boundary = 'Three measured ingredients transferred; native ice command delivered'
    # Read-only projection from locked C1 scene and production workbench geometry.
    theta = math.radians(28)
    world_y, world_z = 1.055+.4*math.cos(theta), -.65+.4*math.sin(theta)
    cx = 640 + 1.55*720/7.4
    cy = 360 - ((world_y-10.089431)*math.cos(theta)+(world_z+17.20895)*math.sin(theta))*720/7.4
    radius = 720/7.4*.25
    key_control(0x53)
    begin_segment('continuous native Stir orbit', (cx+radius,cy))
    move(cx+radius, cy)
    pause(.15)
    key(0x53, True)
    start = time.perf_counter()
    previous, angle = start, 0.0
    while time.perf_counter()-start < 30:
        now = time.perf_counter()
        # A scheduling pause pauses the hand instead of jumping across the glass.
        angle += min(now-previous, .04)*math.radians(150)
        previous = now
        move(cx+radius*math.cos(angle), cy-radius*math.sin(angle))
        o = observe()
        if o and o['stirDegrees'] >= 1440:
            break
        time.sleep(.012)
    key(0x53, False)
    end_segment()
    pause(.7)
    stirred = observe()
    if stirred['stirDegrees'] < 360:
        raise RuntimeError('Missing boundary: recipe preparation Stir >=360 degrees; actual=' + str(stirred['stirDegrees']))
    capture('05-native-stirred')
    boundary = 'Real OS cursor trajectory accumulated production Stir degrees=' + str(stirred['stirDegrees'])
    hold(0x52, 3.5)
    wait_for(lambda o: o['mixing'] < .001 and o['serving'] > 117, 'Native strain transferred authoritative liquid')
    capture('06-strained')
    if '손님께 내기' not in observe()['visibleText']:
        raise RuntimeError('Expected Serve control not present')
    click(1157, 605, '손님께 내기', 'Brewing')
    reaction = wait_for(lambda o: o['phase'] == 'Reaction' and o['services'] == 1, 'One native Serve committed and guest reacted')
    write('service-durable.json', snapshot())
    capture('07-reaction')
    if reaction['recipe'] != 'recipe_test_stir' or reaction['preparation'] != 'Good' or not reaction['fits']:
        raise RuntimeError('Incorrect production recognition/evaluation: ' + json.dumps(reaction))
    boundary = 'One native Seolya service recognized/evaluated with guest reaction'
    for phase, seq in [('Result', 8), ('Farewell', 9), ('Farewell', 10)]:
        click(969, 669, '계속  Enter', observe()['phase'])
        wait_for(lambda o: o['phase'] == phase and o['sequence'] == seq, 'Normal service reading ' + phase)
    capture('08-farewell')
    click(969, 669, '계속  Enter', 'Farewell')
    completed = wait_for(lambda o: o['encounter'] == 1 and o['phase'] == 'Intro' and o['services'] == 1, 'Supported departure completed; next guest arrived')
    final = snapshot()
    write('departure-durable.json', final)
    capture('09-departure-completed')
    if len(final['payload']['completedOccurrences']) != 1 or final['payload']['actComplete'] or final['payload']['memoryIds']:
        raise RuntimeError('Unexpected completion boundaries')
    result.update(status='PASS', service=reaction, departure=completed, lastConfirmedBoundary='One complete native Stir service with supported departure', finishedUtc=utc())
    print('HG04_READY_FOR_OWNER_REVIEW', flush=True)
except Exception as e:
    classification = 'OS/environment' if 'ENVIRONMENT_INTERRUPTED' in str(e) or 'OS SendInput' in str(e) else 'unresolved production/harness boundary'
    result.update(status='ENVIRONMENT_INTERRUPTED' if 'ENVIRONMENT_INTERRUPTED' in str(e) else 'BLOCKED', requirement=requirement, lastConfirmedBoundary=boundary, firstMissingOrIncorrectBoundary=str(e), classification=classification, finishedUtc=utc())
    print(json.dumps(result, ensure_ascii=False), flush=True)
finally:
    if mouse_held:
        try:
            release = INPUT(type=0)
            release.mi = MI(0, 0, 0, 4, 0, TAG)
            emit(release, 'release owned mouse button', cleanup=True)
        except Exception:
            pass
    for vk in list(held):
        try:
            key(vk, False, cleanup=True)
        except Exception:
            pass
    armed = False
    if hook_tid:
        U.PostThreadMessageW(hook_tid, 0x12, 0, 0)
    flush_events()
    (OUT / 'harness-used.py').write_bytes(Path(__file__).read_bytes())
    write('native-result.json', result)
    # End only this harness-owned fresh process; cleanup is outside gameplay proof.
    if player and player.poll() is None:
        log('cleanup', operation='terminate owned fresh Player', pid=pid)
        player.terminate()
        player.wait(timeout=10)
    if player:
        write('process-ended.json', dict(pid=pid, endedUtc=utc(), exitCode=player.returncode, reason='Harness cleanup after retained bounded evidence'))
sys.exit(0 if result['status'] == 'PASS' else 2)
