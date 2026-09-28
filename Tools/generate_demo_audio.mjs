// Original temporary demo sounds; no external recordings or sampled music.
// Run: node Tools/generate_demo_audio.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
const out = path.resolve('Assets/_SURLHWA/Audio/Drafts');
await fs.mkdir(out, { recursive: true });
const rate = 22050;
let seed = 190928;
function noise() { seed = (1664525 * seed + 1013904223) >>> 0; return seed / 2147483648 - 1; }
function tone(hz, t) { return Math.sin(2 * Math.PI * hz * t); }
async function wav(name, seconds, sample) {
  const count = Math.round(seconds * rate), bytes = Buffer.alloc(44 + count * 2);
  bytes.write('RIFF'); bytes.writeUInt32LE(bytes.length - 8, 4); bytes.write('WAVEfmt ', 8);
  bytes.writeUInt32LE(16, 16); bytes.writeUInt16LE(1, 20); bytes.writeUInt16LE(1, 22);
  bytes.writeUInt32LE(rate, 24); bytes.writeUInt32LE(rate * 2, 28);
  bytes.writeUInt16LE(2, 32); bytes.writeUInt16LE(16, 34); bytes.write('data', 36);
  bytes.writeUInt32LE(count * 2, 40);
  let peak = 0, power = 0;
  for (let i = 0; i < count; i++) {
    const t = i / rate;
    const edge = Math.min(1, t / .02, (seconds - t) / .03);
    const v = Math.max(-.95, Math.min(.95, sample(t, i) * edge));
    peak = Math.max(peak, Math.abs(v)); power += v * v;
    bytes.writeInt16LE(Math.round(v * 32767), 44 + i * 2);
  }
  await fs.writeFile(path.join(out, name + '.wav'), bytes);
  console.log(name, { seconds, peak, rms: Math.sqrt(power / count) });
}
// Sparse original sustained harmony, no melody borrowed from an existing work.
const chords = [[146.832,220,277.183],[130.813,195.998,261.626],
                [164.814,220,329.628],[146.832,220,293.665]];
await wav('WinterRoom', 48, t => {
  const section = Math.floor(t / 12), u = t % 12;
  const envelope = Math.sin(Math.PI * u / 12) ** 2;
  return chords[section].reduce((v,hz,k) => v + .09 * envelope *
    (tone(hz,t) + .15 * tone(hz*2,t)) * (1 + .04 * tone(.13 + k*.017,t)), 0);
});
let wind = 0;
await wav('DistantWind', 16, t => {
  wind = wind * .992 + noise() * .008;
  return wind * .55 * (.65 + .35 * tone(1/16,t)) * Math.sin(Math.PI*t/16) ** 2;
});
let water = 0;
await wav('Pour', 2, t => {
  water = water*.7 + noise()*.3;
  return .12*water + .025*tone(330+70*tone(3,t),t);
});
await wav('Ice', .55, t => .17*Math.exp(-t*12)*(tone(2600,t)+.4*tone(3900,t)) +
  .06*noise()*Math.exp(-t*30));
await wav('Glass', .4, t => .16*Math.exp(-t*16)*(tone(880,t)+.3*tone(1413,t)));
await wav('Door', .75, t => .08*Math.exp(-t*7)*tone(523.251,t)+
  .05*Math.exp(-t*5)*tone(783.991,t));
await wav('Page', .12, t => .08*noise()*Math.sin(Math.PI*t/.12)**2);
