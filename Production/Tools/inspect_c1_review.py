"""Validate C1 independently without overwriting the historical C manifest."""
import json
import sys
from pathlib import Path
import inspect_c_review as base

def outcome_failures(run, expected, process, log):
    return base.outcome_failures(run, expected, process, log, 'C1')

def inspect(folder):
    result = base.inspect(folder, 'C1')
    result['ownerGate'] = {'HG01': 'APPROVED', 'HG02': 'APPROVED',
        'HG03': 'READY_FOR_OWNER_REVIEW' if result['result'] == 'PASS' else 'VALIDATION_FAILED'}
    result['ownerJudgment'] = 'HG01/HG02 APPROVED 2026-10-12; HG03 C1 PENDING'
    result['limits'][0] = 'Accepted C camera/spatial/visual direction; C1 polish awaiting HG03 owner judgment.'
    return result

if __name__ == '__main__':
    result = inspect(Path(sys.argv[1]).resolve())
    (base.ROOT / 'Validation/Manifests/captures-c1.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(result['result'], len(result['images']), 'native C1 captures;', result['failures'])
    sys.exit(0 if result['result'] == 'PASS' else 1)
