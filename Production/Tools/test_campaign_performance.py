"""Synthetic telemetry tests only; these are not Player benchmarks."""
import unittest

from summarize_campaign_performance import distribution, summarize


def metadata():
    return dict(schemaVersion=1, finalized=True, captureGuardFrames=8, droppedSamples=0,
                inputTier='simulated production InputSystem', renderPath='normal camera',
                continuousCapture=False, deviceName='test fixture', operatingSystem='test OS',
                gpuName='test GPU', graphicsApi='test API', cpu='test CPU',
                width=1280, height=720, vSyncCount=0, targetFrameRate=60,
                counters=[dict(name='Main Thread', available=True, unit='TimeNanoseconds')])


def frame(n, **changes):
    row=dict(frame=n, timeSeconds=n/60, frameMs=1000/60, unityDeltaMs=1000/60,
             mainThreadMs=4., gcAllocatedBytes=0, totalUsedBytes=500_000_000,
             systemUsedBytes=600_000_000, gfxUsedBytes=40_000_000, workingSetBytes=650_000_000,
             probeMs=.01, phase='Brewing', slot=3, line=0, memories=0, modal='',
             method='stir', tool='stir', paired=False, focused=True, saving=False,
             warmup=False, timingStamp=n, cpuFrameMs=1000/60, timingMainMs=5.,
             presentWaitMs=1., gpuMs=3., contextStableFrames=8)
    row.update(changes)
    return row


class PerformanceSummaryTests(unittest.TestCase):
    def test_headless_execution_cannot_claim_normal_camera_timing(self):
        meta=metadata(); meta['batchMode']=True
        result=summarize(meta,[frame(1)],[])
        self.assertEqual(result['status'],'INCOMPLETE')
        self.assertTrue(any('headless' in issue for issue in result['issues']))

    def test_nearest_rank_distribution_retains_real_stalls(self):
        result=distribution(list(range(1,101)),positive=True)
        self.assertEqual((result['p50'],result['p95'],result['p99'],result['worst']),(50,95,99,100))
        self.assertEqual(distribution([16,17,1800],positive=True)['worst'],1800)

    def test_zero_allocation_is_valid_missing_is_not_zero(self):
        result=distribution([0,0,12,None,-1,float('nan'),float('inf'),-2])
        self.assertEqual(result['count'],3)
        self.assertEqual(result['missing'],2)
        self.assertEqual(result['invalid'],3)
        self.assertEqual(result['p50'],0)
        self.assertIsNone(distribution([None,-1],positive=True)['p95'])

    def test_capture_and_lag_guard_are_excluded_without_erasing_raw_counts(self):
        frames=[frame(i,frameMs=900 if 9<=i<=20 else 16) for i in range(1,31)]
        interval=dict(kind='capture',label='still',startFrame=10,endFrame=12,startSeconds=.1,endSeconds=.2,finished=True)
        result=summarize(metadata(),frames,[interval])
        self.assertEqual(result['rawSamples'],30)
        self.assertEqual(result['excludedCaptureSamples'],12)
        self.assertEqual(result['eligibleSamples'],18)
        self.assertEqual(result['metrics']['frameMs']['worst'],16)
        self.assertEqual(result['captureIntervals'],[interval])

    def test_missing_gpu_stays_unavailable_and_does_not_poison_cpu(self):
        result=summarize(metadata(),[frame(i,gpuMs=-1) for i in range(1,20)],[])
        self.assertEqual(result['metrics']['mainThreadMs']['count'],19)
        self.assertEqual(result['metrics']['gpuMs']['count'],0)
        self.assertIsNone(result['metrics']['gpuMs']['p50'])
        self.assertIn('gpuMs',result['unavailableMetrics'])
        self.assertNotEqual(result['status'],'PASS')

    def test_frame_duplicates_invalid_times_and_gaps_are_reported(self):
        rows=[frame(1),frame(2),frame(2),frame(5),frame(6,frameMs=float('nan')),frame(7,timeSeconds=-1)]
        result=summarize(metadata(),rows,[])
        self.assertEqual(result['duplicateOrOutOfOrderSamples'],1)
        self.assertEqual(result['unobservedFrameIds'],2)
        self.assertEqual(result['invalidSamples'],2)
        self.assertEqual(result['status'],'INCOMPLETE')

    def test_capacity_drops_unfinished_capture_and_continuous_media_cannot_look_ready(self):
        meta=metadata(); meta['droppedSamples']=12; meta['continuousCapture']=True
        result=summarize(meta,[frame(1)],[dict(startFrame=1,endFrame=-1,finished=False,label='interrupted',kind='capture')])
        self.assertEqual(result['status'],'INCOMPLETE')
        self.assertEqual(result['sourceDroppedSamples'],12)
        self.assertEqual(result['eligibleSamples'],0)
        self.assertTrue(any('continuous' in issue for issue in result['issues']))
        self.assertFalse(result['hardwareAccepted'])

    def test_repeated_gpu_timestamp_is_not_counted_as_another_gpu_frame(self):
        result=summarize(metadata(),[frame(1,timingStamp=99),frame(2,timingStamp=99),frame(3,timingStamp=100)],[])
        self.assertEqual(result['metrics']['gpuMs']['count'],2)
        self.assertEqual(result['duplicateTimingSamples'],1)

    def test_workload_groups_preserve_memories_method_modal_and_delayed_context_limit(self):
        rows=[frame(1,memories=0),frame(2,memories=1,method='shake'),
              frame(3,memories=7,modal='codex',method='',contextStableFrames=2)]
        result=summarize(metadata(),rows,[])
        self.assertEqual(result['memoryCoverage'],{'0':1,'1':1,'7':1})
        self.assertEqual(len(result['workloads']),3)
        codex=next(x for x in result['workloads'] if x['modal']=='codex')
        self.assertEqual(codex['metrics']['gpuMs']['count'],0)
        self.assertEqual(result['metrics']['gpuMs']['count'],3)
        self.assertEqual(result['unstableTimingContextSamples'],1)

    def test_probe_overhead_and_driver_gap_remain_explicit(self):
        result=summarize(metadata(),[frame(1,probeMs=.25)],[])
        self.assertEqual(result['metrics']['probeMs']['worst'],.25)
        self.assertIn('driverVersion',result['missingHostMetadata'])
        self.assertIn('probe',result['measurementTier'])
        self.assertFalse(result['hardwareAccepted'])

    def test_warmup_and_insufficient_steady_window_do_not_certify_the_protocol(self):
        result=summarize(metadata(),[frame(1,warmup=True),frame(2),frame(3)],[],
                         dict(driverVersion='fixture',powerPlan='fixture',storage='fixture'))
        self.assertEqual(result['excludedWarmupSamples'],1)
        self.assertFalse(result['hasSixtySecondSteadyWindow'])
        self.assertEqual(result['missingHostMetadata'],[])

    def test_actual_powershell_host_shape_keeps_driver_and_power_distinct_from_graphics_api(self):
        host=dict(gpu=[dict(Name='fixture GPU',DriverVersion='fixture driver')],
                  powerScheme='fixture scheme',storage=[dict(Model='fixture disk')],errors=[])
        result=summarize(metadata(),[frame(1)],[],host)
        self.assertEqual(result['missingHostMetadata'],[])
        self.assertEqual(result['host'],host)
        host['gpu'][0]['DriverVersion']=''
        self.assertIn('driverVersion',summarize(metadata(),[frame(1)],[],host)['missingHostMetadata'])

    def test_missing_tail_cannot_match_finalized_count(self):
        meta=metadata(); meta['samples']=100
        result=summarize(meta,[frame(1)],[])
        self.assertEqual(result['status'],'INCOMPLETE')
        self.assertTrue(any('count' in issue for issue in result['issues']))

    def test_background_only_rows_cannot_establish_foreground_baseline(self):
        result=summarize(metadata(),[frame(1,focused=False),frame(2,focused=False)],[])
        self.assertEqual(result['eligibleSamples'],2)
        self.assertEqual(result['foregroundSamples'],0)
        self.assertEqual(result['foregroundMetrics']['frameMs']['count'],0)
        self.assertEqual(result['status'],'INCOMPLETE')
        self.assertTrue(any('focused' in issue for issue in result['issues']))

    def test_foreground_aggregate_keeps_background_stalls_only_in_all_eligible_metrics(self):
        result=summarize(metadata(),[frame(1,focused=False,frameMs=3000),
                                   frame(2,frameMs=20),frame(3,frameMs=22)],[])
        self.assertEqual(result['metrics']['frameMs']['worst'],3000)
        self.assertEqual(result['foregroundSamples'],2)
        self.assertEqual(result['foregroundMetrics']['frameMs']['worst'],22)
        self.assertFalse(result['foregroundHasSixtySecondSteadyWindow'])

    def test_delayed_gpu_after_focus_regain_is_not_called_foreground_work(self):
        result=summarize(metadata(),[frame(1,focused=False,gpuMs=900),
                                   frame(2,gpuMs=800,contextStableFrames=2),
                                   frame(3,gpuMs=3,contextStableFrames=8)],[])
        self.assertEqual(result['metrics']['gpuMs']['worst'],900)
        self.assertEqual(result['foregroundMetrics']['gpuMs']['worst'],3)
        self.assertEqual(result['foregroundMetrics']['gpuMs']['count'],1)


if __name__=='__main__':
    unittest.main()
