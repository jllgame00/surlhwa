"""Summarize opt-in Player telemetry; never certify hardware budgets from fixtures.

Raw JSONL remains authoritative. Nearest-rank percentiles retain stalls, including
GC/save frames; only explicit capture/warmup intervals and invalid rows are omitted.
"""
import argparse
import json
import math
from pathlib import Path

TIMING_METRICS = ('cpuFrameMs', 'timingMainMs', 'presentWaitMs', 'gpuMs')
METRICS = ('frameMs', 'unityDeltaMs', 'mainThreadMs', 'gcAllocatedBytes',
           'totalUsedBytes', 'systemUsedBytes', 'gfxUsedBytes', 'workingSetBytes',
           'probeMs') + TIMING_METRICS
POSITIVE = {'frameMs', 'unityDeltaMs', 'mainThreadMs', 'cpuFrameMs', 'timingMainMs', 'gpuMs'}
CONTEXT = ('scenario', 'phase', 'slot', 'memories', 'modal', 'method', 'paired', 'focused', 'saving',
           'largeText', 'highContrast', 'width', 'height', 'vSyncCount', 'targetFrameRate')


def finite(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def distribution(values, positive=False):
    valid, missing, invalid = [], 0, 0
    for value in values:
        if value is None or value == -1:
            missing += 1
        elif not finite(value) or value < 0 or (positive and value == 0):
            invalid += 1
        else:
            valid.append(value)
    valid.sort()
    result = dict(count=len(valid), missing=missing, invalid=invalid)
    for name, percentile in (('p50', .50), ('p95', .95), ('p99', .99), ('worst', 1)):
        result[name] = valid[max(0, math.ceil(len(valid) * percentile) - 1)] if valid else None
    return result


def metrics(rows, stable_only=False):
    return {name: distribution(
        [row.get(name) if name not in TIMING_METRICS or
         (row['_uniqueTiming'] and (not stable_only or row.get('contextStableFrames', 0) >= 8))
         else None for row in rows], positive=name in POSITIVE) for name in METRICS}


def steady_seconds(rows):
    longest = elapsed = 0.
    previous = None
    for row in rows:
        if previous is not None and row['frame'] == previous['frame'] + 1:
            elapsed += row['frameMs'] / 1000
        else:
            elapsed = row['frameMs'] / 1000
        longest = max(longest, elapsed)
        previous = row
    return longest


def summarize(metadata, frames, intervals, host=None):
    host = host or {}
    issues, excluded, valid = [], [], []
    guard = max(0, metadata.get('captureGuardFrames', 8))
    for interval in intervals:
        start = interval.get('startFrame')
        end = interval.get('endFrame')
        if not isinstance(start, int) or isinstance(start, bool) or start < 0:
            issues.append('invalid capture interval start; all frames excluded')
            excluded.append((0, math.inf))
            continue
        if not interval.get('finished') or not isinstance(end, int) or end < start:
            issues.append('unfinished capture interval: ' + str(interval.get('label', '')))
            end = math.inf
        excluded.append((start - 1, end + guard))
    continuous = metadata.get('continuousCapture', False)
    if continuous:
        issues.append('continuous capture is incompatible with normal-camera timing')
    if metadata.get('batchMode', False):
        issues.append('headless execution cannot establish normal-camera timing')
    dropped = metadata.get('droppedSamples', 0)
    if dropped:
        issues.append('sample buffer capacity dropped frames')
    if not metadata.get('finalized'):
        issues.append('probe did not finalize')
    if metadata.get('samples', len(frames)) != len(frames):
        issues.append('raw sample count differs from finalized metadata')
    invalid = duplicates = gaps = capture_count = warmup_count = duplicate_timing = unstable = 0
    previous_frame = previous_time = None
    timing_stamps = set()
    for source in frames:
        row = dict(source)
        frame, time = row.get('frame'), row.get('timeSeconds')
        if not isinstance(frame, int) or isinstance(frame, bool) or frame < 0:
            invalid += 1
            continue
        if previous_frame is not None and frame <= previous_frame:
            duplicates += 1
            continue
        if previous_frame is not None:
            gaps += max(0, frame - previous_frame - 1)
        previous_frame = frame
        if (not finite(time) or time < 0 or (previous_time is not None and time < previous_time)
                or not finite(row.get('frameMs')) or row['frameMs'] <= 0):
            invalid += 1
            continue
        previous_time = time
        if continuous or any(start <= frame <= end for start, end in excluded):
            capture_count += 1
            continue
        if row.get('warmup'):
            warmup_count += 1
            continue
        stamp = row.get('timingStamp')
        row['_uniqueTiming'] = finite(stamp) and stamp > 0 and stamp not in timing_stamps
        if finite(stamp) and stamp > 0:
            if stamp in timing_stamps:
                duplicate_timing += 1
            timing_stamps.add(stamp)
        if row['_uniqueTiming'] and row.get('contextStableFrames', 0) < 8:
            unstable += 1
        valid.append(row)
    if invalid or duplicates or gaps:
        issues.append('invalid, repeated, out-of-order or unobserved frame samples')
    if not valid:
        issues.append('no eligible normal-camera samples')
    foreground = [row for row in valid if row.get('focused') is True]
    if not foreground:
        issues.append('no focused eligible samples; foreground baseline unavailable')
    grouped = {}
    for row in valid:
        key = tuple(row.get(name, '') for name in CONTEXT)
        grouped.setdefault(key, []).append(row)
    workloads = [dict(zip(CONTEXT, key), samples=len(rows), steadySeconds=steady_seconds(rows),
                      metrics=metrics(rows, stable_only=True)) for key, rows in grouped.items()]
    all_metrics = metrics(valid)
    # Delayed timing immediately after focus changes can still describe background
    # work. Apply the same conservative context rule used for workload attribution.
    foreground_metrics = metrics(foreground, stable_only=True)
    unavailable = [name for name, value in all_metrics.items() if not value['count']]
    gpu = host.get('gpu', [])
    if isinstance(gpu, dict):
        gpu = [gpu]
    host_values = dict(driverVersion=host.get('driverVersion') or
                       [row.get('DriverVersion') for row in gpu if row.get('DriverVersion')],
                       powerPlan=host.get('powerPlan') or host.get('powerScheme'), storage=host.get('storage'))
    missing_host = [name for name, value in host_values.items() if not value]
    return dict(schemaVersion=1, status='INCOMPLETE' if issues else
                'PARTIAL_COUNTERS' if unavailable else 'MEASUREMENTS_RECORDED',
                hardwareAccepted=False, issues=issues, metadata=metadata, host=host,
                measurementTier='normal camera with probe overhead; capture intervals excluded; ' +
                                metadata.get('inputTier', 'unspecified input'),
                rawSamples=len(frames), eligibleSamples=len(valid), invalidSamples=invalid,
                duplicateOrOutOfOrderSamples=duplicates, unobservedFrameIds=gaps,
                sourceDroppedSamples=dropped, excludedCaptureSamples=capture_count,
                excludedWarmupSamples=warmup_count, duplicateTimingSamples=duplicate_timing,
                unstableTimingContextSamples=unstable, captureIntervals=intervals,
                missingHostMetadata=missing_host, unavailableMetrics=unavailable,
                percentileMethod='nearest rank; missing=-1/null; finite nonnegative values only',
                metrics=all_metrics, foregroundSamples=len(foreground), foregroundMetrics=foreground_metrics,
                foregroundUnavailableMetrics=[name for name, value in foreground_metrics.items() if not value['count']],
                foregroundHasSixtySecondSteadyWindow=any(row['focused'] is True and row['steadySeconds'] >= 60 for row in workloads),
                workloads=workloads,
                memoryCoverage={str(n): sum(row.get('memories') == n for row in valid) for n in (0, 1, 7)},
                hasSixtySecondSteadyWindow=any(row['steadySeconds'] >= 60 for row in workloads))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        def read(name):
            return json.loads((args.root / name).read_text(encoding='utf-8-sig'))
        metadata = read('performance-metadata.json')
        intervals = read('performance-captures.json')
        if isinstance(intervals, dict):
            intervals = intervals['intervals']
        frames = [json.loads(line) for line in (args.root / 'performance-frames.jsonl').read_text(
            encoding='utf-8-sig').splitlines() if line.strip()]
        host = read('performance-host.json') if (args.root / 'performance-host.json').exists() else None
        result = summarize(metadata, frames, intervals, host)
        output = json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + '\n'
        if args.output:
            raw_paths = {args.root / name for name in ('performance-metadata.json', 'performance-frames.jsonl',
                         'performance-captures.json', 'performance-host.json')}
            if args.output.resolve() in {path.resolve() for path in raw_paths}:
                parser.error('output must not replace raw telemetry')
            args.output.write_text(output, encoding='utf-8')
        else:
            print(output, end='')
        return 2 if result['status'] == 'INCOMPLETE' else 0
    except (OSError, ValueError, KeyError, TypeError) as error:
        parser.exit(2, 'telemetry summary failed: ' + str(error) + '\n')


if __name__ == '__main__':
    raise SystemExit(main())
