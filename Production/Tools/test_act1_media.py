import copy
import json
from pathlib import Path
import runpy
import shutil
import sys
import unittest
import uuid
from unittest.mock import patch
import wave


SCRIPT = Path(__file__).with_name('render_act1_media.py')


class MediaClockTests(unittest.TestCase):
    def setUp(self):
        # Python 3.13's Windows mode-0700 temporary directory ACL excludes the
        # restricted runner. Use an ordinary, unique test-owned workspace dir.
        evidence=(SCRIPT.parents[1]/'Validation/Evidence').resolve()
        self.folder=evidence/('media-clock-'+uuid.uuid4().hex)
        self.assertEqual(self.folder.parent,evidence)
        self.folder.mkdir()
        self.addCleanup(shutil.rmtree,self.folder)
        (self.folder / 'frames').mkdir()
        self.rows = [dict(frame=n, time=.5 + (n-1)*20, audioTime=n-1) for n in range(1, 5)]
        self.write_json('player-result.json', dict(status='PASS', tier='simulated test'))
        self.write_json('process.json', dict(exitCode=0, timedOut=False))
        (self.folder / 'frame-times.csv').write_text(''.join(f"{r['frame']},{r['time']}\n" for r in self.rows))
        (self.folder / 'capture-times.csv').write_text('1,earned-memory,30.5\n')
        for row in self.rows:
            (self.folder / f"frames/{row['frame']:05d}.png").write_bytes(b'recorded image fixture')
        with wave.open(str(self.folder / 'unity-mix.wav'), 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(8000)
            wav.writeframes(b'\xe8\x03\x18\xfc' * 16000)

    def write_json(self, name, value):
        (self.folder / name).write_text(json.dumps(value), encoding='utf-8')

    def motion(self, rows=None):
        (self.folder / 'motion-times.jsonl').write_text(''.join(json.dumps(r) + '\n' for r in (self.rows if rows is None else rows)))

    def run_packager(self, audio_clock=True):
        commands = self.commands = []
        def encode(command, **kwargs):
            commands.append(command)
            Path(command[-1]).write_bytes(b'mocked encoding; no encoder launched')
        def probe(command, **kwargs):
            # The concat terminal hold uses the last recorded image once more.
            count=sum(line.startswith('file ') for line in (self.folder/'player-frames.ffconcat').read_text().splitlines())
            return json.dumps(dict(streams=[dict(codec_name='h264',time_base='1/8000',nb_frames=str(count))]))
        args = [str(SCRIPT), str(self.folder)] + (['--audio-clock'] if audio_clock else [])
        with patch.object(sys, 'argv', args), patch('subprocess.run', side_effect=encode), patch('subprocess.check_output',side_effect=probe):
            runpy.run_path(str(SCRIPT), run_name='__main__')
        return commands, json.loads((self.folder / 'media.json').read_text(encoding='utf-8'))

    def test_audio_clock_preserves_recorded_frame_order_and_maps_capture_window(self):
        self.motion()
        commands, report = self.run_packager()
        sequence = (self.folder / 'player-frames.ffconcat').read_text()
        self.assertEqual([float(x.split()[1]) for x in sequence.splitlines() if x.startswith('duration ')], [1, 1, 1, 1])
        self.assertEqual([x for x in sequence.splitlines() if x.startswith('file ')],
                         [f"file 'frames/{n:05d}.png'" for n in [1, 2, 3, 4, 4]])
        self.assertEqual(report['clock'], 'AudioRenderer')
        self.assertEqual(report['videoSeconds'], 4)
        # Capture wall 30.5 maps to audio 1.5. Map both boundaries of the
        # original [29.5,55.5] wall-time window; never seek at wall 29.5 in audio.
        self.assertAlmostEqual(report['clips'][0]['startSeconds'], 1.45)
        self.assertAlmostEqual(report['clips'][0]['durationRequested'], 1.3)
        self.assertEqual(len(commands), 2)
        for command in commands:
            self.assertEqual(command[command.index('-fps_mode')+1], 'vfr')
            self.assertEqual(command[command.index('-enc_time_base:v')+1], '1:8000')

    def test_nonzero_first_audio_timestamp_is_an_offset_not_a_new_frame(self):
        for row in self.rows: row['audioTime'] += .25
        self.motion()
        commands, report = self.run_packager()
        self.assertEqual(commands[0][commands[0].index('-itsoffset')+1], '0.25')
        self.assertEqual(report['videoStartSeconds'], .25)
        sequence = (self.folder / 'player-frames.ffconcat').read_text()
        self.assertEqual([float(x.split()[1]) for x in sequence.splitlines() if x.startswith('duration ')], [1, 1, 1, .75])

    def test_default_wall_clock_needs_no_motion_file_and_keeps_existing_timing(self):
        _, report = self.run_packager(False)
        sequence = (self.folder / 'player-frames.ffconcat').read_text()
        self.assertEqual([float(x.split()[1]) for x in sequence.splitlines() if x.startswith('duration ')], [20, 20, 20, .2])
        self.assertNotIn('clock', report)
        self.assertEqual(report['clips'][0]['startSeconds'], 29.5)
        self.assertEqual(report['clips'][0]['durationRequested'], 26)

    def test_recorded_jpeg_and_legacy_png_names_are_used_without_frame_synthesis(self):
        self.rows[1]['image'] = 'frames/00002.jpg'
        (self.folder / self.rows[1]['image']).write_bytes(b'recorded jpeg fixture')
        self.motion()
        self.run_packager()
        sequence = (self.folder / 'player-frames.ffconcat').read_text()
        self.assertIn("file 'frames/00002.jpg'", sequence)
        self.assertNotIn("file 'frames/00002.png'", sequence)
        self.assertIn("file 'frames/00001.png'", sequence)

    def test_buffered_audio_plateaus_keep_last_real_frame_and_disclose_every_omission(self):
        self.rows=[dict(frame=i+1,time=.5+i*.05,audioTime=(i//2)*.005) for i in range(1171)]
        (self.folder/'frame-times.csv').write_text(''.join(f"{r['frame']},{r['time']}\n" for r in self.rows))
        (self.folder/'capture-times.csv').write_text('1,earned-memory,30.525\n')
        for row in self.rows:(self.folder/f"frames/{row['frame']:05d}.png").write_bytes(b'actual recorded fixture')
        self.motion()
        raw={name:(self.folder/name).read_bytes() for name in ('frame-times.csv','motion-times.jsonl','unity-mix.wav')}
        _,report=self.run_packager()
        sequence=(self.folder/'player-frames.ffconcat').read_text().splitlines()
        selected=list(range(2,1171,2))+[1171]
        self.assertEqual([line for line in sequence if line.startswith('file ')],
                         [f"file 'frames/{n:05d}.png'" for n in selected+[1171]])
        durations=[float(line.split()[1]) for line in sequence if line.startswith('duration ')]
        self.assertTrue(all(duration>0 for duration in durations))
        self.assertEqual(len(durations),586)
        self.assertEqual(report['rawFrames'],1171)
        self.assertEqual(report['selectedRecordedFrames'],586)
        self.assertEqual(report['encodedFrames'],587)
        self.assertEqual(report['omittedFrameIds'],list(range(1,1170,2)))
        self.assertIn('last recorded frame',report['audioClockLimitation'])
        self.assertIn('buffer',report['audioClockLimitation'])
        self.assertEqual(report['encodedVideo']['time_base'],'1/8000')
        # Use the complete raw clock for clip boundaries, including plateau
        # observations. Mapping only coalesced wall rows changes this result.
        self.assertAlmostEqual(report['clips'][0]['startSeconds'],1.45)
        self.assertAlmostEqual(report['clips'][0]['durationRequested'],1.3)
        for name,data in raw.items():self.assertEqual((self.folder/name).read_bytes(),data)
        self.assertTrue(all((self.folder/f'frames/{n:05d}.png').is_file() for n in range(1,1172)))

    def test_omitted_plateau_frame_is_still_validated_before_encoding(self):
        self.rows[1]['audioTime']=self.rows[0]['audioTime'];self.motion()
        (self.folder/'frames/00001.png').unlink()
        with self.assertRaisesRegex(SystemExit,'Invalid audio-clock evidence: missing image'):
            self.run_packager()
        self.assertEqual(self.commands,[])

    def test_matching_logs_with_a_missing_raw_frame_id_are_rejected(self):
        rows=[self.rows[i] for i in (0,2,3)];self.motion(rows)
        (self.folder/'frame-times.csv').write_text(''.join(f"{r['frame']},{r['time']}\n" for r in rows))
        with self.assertRaisesRegex(SystemExit,'Invalid audio-clock'):
            self.run_packager()
        self.assertEqual(self.commands,[])

    def test_final_sample_rounding_does_not_replace_a_recorded_timestamp(self):
        self.rows[-1]['audioTime']=4.00005;self.motion()
        _,report=self.run_packager()
        self.assertEqual(report['observedAudioEndSeconds'],4.00005)

    def test_invalid_frame_mapping_or_audio_clocks_are_rejected_before_encoding(self):
        for fault in ['missing', 'duplicate', 'reordered', 'mismatched_id', 'backward_audio', 'negative_audio', 'past_audio', 'nan_audio', 'infinite_audio', 'equal_wall']:
            with self.subTest(fault=fault):
                rows = copy.deepcopy(self.rows)
                if fault == 'missing': rows.pop()
                elif fault == 'duplicate': rows[1] = rows[0].copy()
                elif fault == 'reordered': rows[1], rows[2] = rows[2], rows[1]
                elif fault == 'mismatched_id': rows[1]['frame'] = 9
                elif fault == 'backward_audio': rows[2]['audioTime'] = rows[1]['audioTime']-.1
                elif fault == 'negative_audio': rows[0]['audioTime'] = -.1
                elif fault == 'past_audio': rows[-1]['audioTime'] = 4.1
                elif fault == 'nan_audio': rows[1]['audioTime'] = float('nan')
                elif fault == 'infinite_audio': rows[1]['audioTime'] = float('inf')
                elif fault == 'equal_wall': rows[1]['time'] = rows[0]['time']
                self.motion(rows)
                with self.assertRaisesRegex(SystemExit, 'Invalid audio-clock'):
                    self.run_packager()
                self.assertEqual(self.commands, [])

    def test_unsafe_mismatched_or_missing_image_names_are_rejected(self):
        for name in ['../00002.png', 'frames/../00002.png', '/frames/00002.png', 'frames/00002.webp', 'frames/00001.png', 'frames/00002.jpg', 'frames\\00002.png']:
            with self.subTest(name=name):
                rows = copy.deepcopy(self.rows); rows[1]['image'] = name; self.motion(rows)
                with self.assertRaisesRegex(SystemExit, 'Invalid audio-clock'):
                    self.run_packager()

    def test_capture_outside_observed_wall_clock_is_rejected_before_encoding(self):
        self.motion(); (self.folder / 'capture-times.csv').write_text('1,earned-memory,70\n')
        with self.assertRaisesRegex(SystemExit, 'Invalid audio-clock'):
            self.run_packager()
        self.assertEqual(self.commands, [])

    def test_failed_or_incomplete_player_remains_ineligible(self):
        self.motion()
        for status, code, timed_out in [('FAIL', 0, False), ('PASS', 2, False), ('PASS', 0, True)]:
            self.write_json('player-result.json', dict(status=status, tier='simulated test'))
            self.write_json('process.json', dict(exitCode=code, timedOut=timed_out))
            with self.assertRaisesRegex(SystemExit, 'Refusing failed/incomplete evidence'):
                self.run_packager()
            self.assertEqual(self.commands, [])


if __name__ == '__main__':
    unittest.main()
