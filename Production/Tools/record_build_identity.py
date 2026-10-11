"""Record the exact local source and built Player identity; no repository mutation."""
import hashlib
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

production = Path(__file__).resolve().parents[1]
repository = production.parent
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', '-C', str(repository), *args], text=True).strip()
def inventory(roots):
    return [{'path': p.relative_to(production).as_posix(), 'bytes': p.stat().st_size, 'sha256': digest(p)}
            for root in roots for p in sorted((production / root).rglob('*')) if p.is_file()]

sources = inventory(['Content', 'SourceArt', 'Unity/Assets', 'Unity/Packages', 'Unity/ProjectSettings'])
variant_c = '--variant-c' in sys.argv
payload = inventory(['Validation/Builds/VariantC' if variant_c else 'Validation/Builds/Windows'])
def read_command(args):
    try:
        result = subprocess.run(args, capture_output=True, text=True, errors='replace', check=False)
        return result.stdout.strip() if result.returncode == 0 else 'Unavailable: ' + result.stderr.strip()
    except OSError as error:
        return 'Unavailable: ' + str(error)

identity = {'schemaVersion': 1, 'utc': datetime.now(timezone.utc).isoformat(), 'branch': git('branch', '--show-current'),
    'implementationCommit': git('rev-parse', 'HEAD'),
    'sourceStatusAtBuild': git('status', '--short', '--untracked-files=all', '--', 'Production/Content', 'Production/SourceArt', 'Production/Unity/Assets', 'Production/Unity/Packages', 'Production/Unity/ProjectSettings'),
    'sourceFingerprint': hashlib.sha256(json.dumps(sources, sort_keys=True).encode()).hexdigest(),
    'payloadFingerprint': hashlib.sha256(json.dumps(payload, sort_keys=True).encode()).hexdigest(),
    'sourceFiles': sources, 'playerFiles': payload, 'totalPayloadBytesIncludingDevelopmentSymbols': sum(p['bytes'] for p in payload),
    'hardwareSupplement': {'gpuNameDriverVRAMPowerLimit': read_command(['nvidia-smi', '--query-gpu=name,driver_version,memory.total,power.limit', '--format=csv,noheader']),
        'powerPlan': read_command(['powercfg', '/getactivescheme']),
        'evidence': 'Read-only nvidia-smi and powercfg at record time; storage model not measured'}}
destination = production / ('Validation/Manifests/build-identity-c.json' if variant_c else 'Validation/Manifests/build-identity.json')
destination.write_text(json.dumps(identity, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Build identity recorded:', identity['implementationCommit'], identity['sourceFingerprint'], len(payload), 'Player files')
