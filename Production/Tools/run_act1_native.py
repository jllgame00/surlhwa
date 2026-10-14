"""ACT 1 four-method native integration; never calls into Unity or writes game state.

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
EXE = ROOT / 'Production/Validation/Builds/Act1/SurlhwaAct1.exe'
SAVE = OUT / 'save'
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
requirement = 'ACT 1 native production integration'
boundary = 'Harness prepared; no Player input issued'
player = None
result = {'status': 'IN_PROGRESS', 'requirement': requirement, 'evidence': str(OUT),
          'tier': 'Windows SendInput, real Player; no callback or state injection'}

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
        data, count = C.create_string_buffer(1048576), W.DWORD()
        if not K.ReadFile(handle, data, 1048576, C.byref(count), None):
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

def control(label):
    o=observe()
    found=[c for c in o['controls'] if c['label']==label and c['enabled']]
    if len(found)!=1:
        raise RuntimeError('Expected one active production control: '+label+'; found='+str(len(found)))
    c=found[0]
    point=(c['x']+c['width']/2,c['y']+c['height']/2)
    guard(point,record=True)
    log('control_verified',label=label,phase=o['phase'],clientPoint=point,control=c,observedUtc=o['observedUtc'])
    return point

def check_control(label,phase,point):
    o=wait_for(lambda o:o['phase']==phase and label in o['visibleText'],'Expected control '+label)
    actual=control(label)
    if max(abs(actual[i]-point[i]) for i in (0,1))>.1:
        raise RuntimeError('Control changed before action')

def key_control(vk):
    suffix={0x51:' Q',0x45:' E',0x52:' R',0x49:' I',0x4A:' J',0x53:' S',0x4B:' K',0x46:' F',0x54:' T',0x58:' X',0x4C:' L',0x47:' G',0x4F:' O',0x43:' C',0x44:' D'}
    labels=[c['label'] for c in observe()['controls'] if c['enabled'] and c['label'].endswith(suffix[vk])]
    if len(labels)!=1: raise RuntimeError('Missing key control '+str(vk)+': '+str(labels))
    return control(labels[0])

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

def click_label(label):
    phase=observe()['phase']; x,y=control(label); click(x,y,label,phase); pause(.24)

def target(name):
    for _ in range(5):
        if observe()['target']==name: return
        tap(0x4A)
    raise RuntimeError('Cannot select target '+name)

def ingredient(identity):
    label=names[identity]
    options=[c['label'] for c in observe()['controls'] if c['enabled'] and (c['label']==label or c['label'].endswith('  '+label))]
    if len(options)!=1: raise RuntimeError('Ingredient control missing '+identity)
    click_label(options[0]); wait_for(lambda o:o['ingredient']==identity,'Selected '+identity)

def measured(identity,amount,container,keycode=0x51):
    ingredient(identity); before=observe()[container]
    hold(keycode,max(.03,(amount-1.5)/30))
    for _ in range(8):
        actual=observe()[container]-before; remaining=amount-actual
        if abs(remaining)<=.35: break
        if remaining<0: raise RuntimeError('Native pour exceeded tolerance '+str(actual))
        hold(keycode,max(.035,(remaining-.12)/5),fine=True)
    actual=observe()[container]-before
    if abs(actual-amount)>.35: raise RuntimeError('Native measured amount outside tolerance '+str(actual))
    log('actual_ingredient',ingredient=identity,intendedMl=amount,actualMl=actual,container=container)

def stir():
    o=observe(); cx,cy,r=o['stirX'],o['stirY'],o['stirRadius']
    key_control(0x53); begin_segment('native Stir orbit',(cx+r,cy)); move(cx+r,cy); pause(.15); key(0x53,True)
    start=previous=time.perf_counter(); angle=0
    while time.perf_counter()-start<25:
        now=time.perf_counter(); angle+=min(now-previous,.04)*math.radians(150); previous=now
        move(cx+r*math.cos(angle),cy-r*math.sin(angle)); o=observe()
        if o and o['stirDegrees']>=1080: break
        time.sleep(.012)
    key(0x53,False); end_segment(); pause(.3)
    if observe()['stirDegrees']<720: raise RuntimeError('Missing production angular Stir evidence')

def shake():
    key_control(0x4B); begin_segment('native Shake trajectory',(512,360)); move(512,360); pause(.15); key(0x4B,True)
    start=time.perf_counter()
    while time.perf_counter()-start<7:
        t=time.perf_counter()-start; move(640-128*math.cos(2*math.pi*t),360); observe(); time.sleep(.015)
    key(0x4B,False); end_segment(); pause(.3)
    if observe()['shakeReversals']<8: raise RuntimeError('Missing production Shake reversal evidence')

def float_layer():
    key_control(0x46); begin_segment('native Float pour height and rate',(576,446)); move(576,446); pause(.15); key(0x46,True)
    start=time.perf_counter()
    while time.perf_counter()-start<14:
        pause(.05)
        if observe()['jigger']<.001: break
    key(0x46,False); end_segment(); pause(.3)
    if observe()['jigger']>.001: raise RuntimeError('Float did not finish actual measured transfer')

def prepare(identity):
    recipe=recipes[identity]; method=recipe['method']; click_label(method.capitalize())
    if method=='stir':
        for a in recipe['amounts']:
            if a['ingredientId']==recipe['dashIngredient']: continue
            target('Jigger'); measured(a['ingredientId'],a['ml'],'jigger'); hold(0x45,2.2)
        target('Mixing')
        if recipe['dashIngredient']: ingredient(recipe['dashIngredient']); tap(0x44)
        tap(0x49); stir(); hold(0x52,4)
        if recipe['requiredGarnish']=='cherry': tap(0x43)
    elif method=='build':
        target('Serving'); tap(0x49)
        for a in recipe['amounts']: measured(a['ingredientId'],a['ml'],'serving')
        tap(0x47); tap(0x4F)
    elif method=='shake':
        target('Shaker')
        for a in recipe['amounts']:
            if a['ingredientId']!=recipe['lateIngredient']: measured(a['ingredientId'],a['ml'],'shaker')
        tap(0x49); tap(0x4C); shake(); tap(0x4C); hold(0x54,4); target('Serving')
        late=next(a for a in recipe['amounts'] if a['ingredientId']==recipe['lateIngredient']); measured(late['ingredientId'],late['ml'],'serving',0x58)
    else:
        for identity in recipe['layerOrder']:
            a=next(a for a in recipe['amounts'] if a['ingredientId']==identity)
            target('Jigger'); measured(identity,a['ml'],'jigger'); float_layer()
    capture('brew-'+str(observe()['encounter'])+'-'+method)
    click_label('손님께 내기')
    o=wait_for(lambda o:o['phase']=='Reaction','Production service committed')
    if o['recipe']!=recipe['id'] or o['preparation']!='Good' or not o['quality']:
        raise RuntimeError('Production service result differs: '+json.dumps(o,ensure_ascii=False))
    write('service-'+str(o['encounter'])+'.json',snapshot()); capture('reaction-'+str(o['encounter']))
    print('NATIVE_SERVICE_PASS '+str(o['encounter'])+' '+recipe['id'],flush=True)

try:
    subprocess.run([sys.executable,str(ROOT/'Production/Tools/act1_inventory.py'),'write',str(OUT/'inventory.json')],check=True)
    content=json.loads((ROOT/'Production/Content/Campaign/act1.json').read_text(encoding='utf-8'))
    names={x['id']:x['label'] for x in content['ingredients']}; recipes={x['id']:x for x in content['recipes']}
    live=subprocess.check_output(['powershell','-NoProfile','-Command','(Get-Process SurlhwaAct1 -ErrorAction SilentlyContinue).Id'],text=True).strip()
    if live: raise RuntimeError('Existing ACT 1 Player: '+live)
    run=uuid.uuid4().hex
    args=[str(EXE),'-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-campaignEvidenceRoot',str(OUT),'-campaignSaveRoot',str(SAVE),'-logFile',str(OUT/'Player.log')]
    startup=subprocess.STARTUPINFO(); startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow=1
    player=subprocess.Popen(args,startupinfo=startup); pid=player.pid; end=time.perf_counter()+45
    while time.perf_counter()<end:
        windows=[]
        @ENUM
        def enum(w,unused):
            if owner(w)==pid and U.IsWindowVisible(w):
                r=W.RECT(); U.GetClientRect(w,C.byref(r))
                if r.right==1280 and r.bottom==720: windows.append(w)
            return True
        U.EnumWindows(enum,0)
        if len(windows)==1:
            hwnd=windows[0]
            if (OUT/'observation.json').exists(): break
        if player.poll() is not None: raise RuntimeError('Player exited before native check')
        time.sleep(.1)
    if not hwnd: raise RuntimeError('No unique expected Player window')
    handle=K.OpenProcess(0x1000,False,pid); buf,size=C.create_unicode_buffer(32768),W.DWORD(32768)
    if not K.QueryFullProcessImageNameW(handle,0,buf,C.byref(size)): raise RuntimeError('Cannot verify executable')
    K.CloseHandle(handle)
    if Path(buf.value).resolve()!=EXE.resolve(): raise RuntimeError('Executable mismatch')
    # Initial acquisition only, before arming or any tested input. Never refocus an active attempt.
    U.ShowWindow.argtypes=[W.HWND,C.c_int]; U.SetForegroundWindow.argtypes=[W.HWND]
    U.ShowWindow(hwnd,9); U.SetForegroundWindow(hwnd); time.sleep(.4)
    rect0=region(); log('initial_window_acquisition',pid=pid,hwnd=hwnd,region=rect0,foregroundPid=owner(U.GetForegroundWindow()))
    write('identity.json',dict(utc=utc(),pid=pid,hwnd=hwnd,executable=buf.value,sha256=hashlib.sha256(EXE.read_bytes()).hexdigest(),arguments=args,clientRegion=rect0))
    thread=threading.Thread(target=hook_thread,daemon=True); thread.start()
    if not hook_ready.wait(3) or hook_error: raise RuntimeError('Cannot audit input hooks: '+str(hook_error))
    if any(U.GetAsyncKeyState(vk)&0x8000 for vk in range(1,255)): raise RuntimeError('ENVIRONMENT_INTERRUPTED: input already held')
    guard(record=True); armed=True
    wait_for(lambda o:o['phase']=='menu' and o['focused'],'Fresh foreground menu'); capture('00-menu')
    click_label('도감  Tab'); pause(.3); capture('01-codex-start'); click_label('돌아가기  Esc')
    click_label('새로 시작'); wait_for(lambda o:o['phase']=='Welcome','New campaign')
    plan=['recipe_test_stir']*3+['recipe_dry_martini','recipe_dry_martini','recipe_manwol','recipe_negroni','recipe_test_stir','recipe_negroni','recipe_manhattan','recipe_manhattan','recipe_test_stir','recipe_negroni','recipe_gin_fizz','recipe_dry_martini','recipe_gin_fizz','recipe_dry_martini']
    visited=set()
    while True:
        o=observe()
        if o['saving']: pause(.1); continue
        if o['saveError']: raise RuntimeError('Production error: '+o['saveError'])
        if o['overflowText']: write('overflow-'+str(o['sequence'])+'.json',o)
        if o['phase']=='Complete': break
        if o['phase'] in ('Arriving','Departing'): pause(.2); continue
        if o['phase']=='Brewing':
            if o['encounter'] in visited: raise RuntimeError('Unexpected retry')
            visited.add(o['encounter']); prepare(plan[o['encounter']]); continue
        if o['phase']=='Choice':
            choice=next(t['value'] for t in content['texts'] if t['id']==content['prince']['choices']['keep'])
            click_label(choice); continue
        if o['phase']=='Request': click_label('한 잔 준비하기')
        else: click_label('계속  Enter')
        pause(.2)
    final=snapshot(); write('completed-campaign.json',final); capture('99-complete'); pause(3)
    if not o['actComplete'] or o['memories']!=1 or o['services']!=17 or not o['memoryGlowing']: raise RuntimeError('Missing ACT 1 memory completion boundary')
    result.update(status='PASS',services=17,lastConfirmedBoundary='Complete native ACT 1, all four methods, earned glowing star',finishedUtc=utc())
    print('ACT1_NATIVE_COMPLETE',flush=True)
except Exception as e:
    result.update(status='ENVIRONMENT_INTERRUPTED' if 'ENVIRONMENT_INTERRUPTED' in str(e) else 'BLOCKED',lastConfirmedBoundary=boundary,firstMissingOrIncorrectBoundary=str(e),finishedUtc=utc())
    print(json.dumps(result,ensure_ascii=False),flush=True)
finally:
    if mouse_held:
        try:
            i=INPUT(type=0); i.mi=MI(0,0,0,4,0,TAG); emit(i,'release owned mouse',cleanup=True)
        except Exception: pass
    for vk in list(held):
        try: key(vk,False,cleanup=True)
        except Exception: pass
    armed=False
    if hook_tid: U.PostThreadMessageW(hook_tid,0x12,0,0)
    flush_events(); (OUT/'harness-used.py').write_bytes(Path(__file__).read_bytes()); write('native-result.json',result)
    if player and player.poll() is None: player.terminate(); player.wait(timeout=10)
    if player: write('process-ended.json',dict(pid=pid,endedUtc=utc(),exitCode=player.returncode,reason='Owned harness cleanup'))
    if (OUT/'inventory.json').exists():
        audit=subprocess.run([sys.executable,str(ROOT/'Production/Tools/act1_inventory.py'),'verify',str(OUT/'inventory.json')],capture_output=True,text=True)
        write('inventory-verification.json',dict(exitCode=audit.returncode,stdout=audit.stdout,stderr=audit.stderr))
        if audit.returncode:
            result.update(status='BLOCKED',firstMissingOrIncorrectBoundary='Source/Player inventory changed during native attempt')
            write('native-result.json',result)
sys.exit(0 if result['status']=='PASS' else 2)
