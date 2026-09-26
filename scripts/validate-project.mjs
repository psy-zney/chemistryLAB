import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve, dirname } from 'node:path';
import { exportMatrix, root, sourcePath, outputPath, ascii, gcd } from './export-matrix.mjs';

const read = file => readFileSync(resolve(root, file), 'utf8').replace(/^\uFEFF/, '');
const files = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z'], { cwd: root, encoding: 'utf8' }).split('\0').filter(Boolean);
const unique = (items, key, name) => {
  const ids = items.map(key);
  assert(ids.every(id => typeof id === 'string' && id.length > 0), `${name}: empty ID`);
  assert.equal(new Set(ids).size, ids.length, `${name}: duplicate IDs`);
};
let jsonCount = 0, assetCount = 0, linkCount = 0;
for (const file of files) {
  if (file.endsWith('.json')) { JSON.parse(read(file)); jsonCount++; }
  if (file.startsWith('Assets/')) {
    if (file.endsWith('.meta')) assert(existsSync(resolve(root, file.slice(0, -5))), `Orphan metadata: ${file}`);
    else { assetCount++; assert(existsSync(resolve(root, `${file}.meta`)), `Missing metadata: ${file}`); }
    let parent = dirname(file);
    while (parent !== 'Assets' && parent !== '.') {
      assert(existsSync(resolve(root, `${parent}.meta`)), `Missing folder metadata: ${parent}`);
      parent = dirname(parent);
    }
  }
  if (file.endsWith('.md')) {
    for (const match of read(file).matchAll(/\[[^\]\n]*\]\(([^)\s]+)\)/g)) {
      const target = match[1].split('#')[0];
      if (!target || /^(https?:|app:|mailto:|\\)/.test(target)) continue;
      assert(existsSync(resolve(root, dirname(file), decodeURIComponent(target))), `Broken Markdown link: ${file} -> ${target}`);
      linkCount++;
    }
  }
}
const manifest = JSON.parse(read('Packages/manifest.json'));
const lock = JSON.parse(read('Packages/packages-lock.json'));
for (const [name, version] of Object.entries(manifest.dependencies)) assert.equal(lock.dependencies[name]?.version, version, `Package mismatch: ${name}`);

const raw = readFileSync(resolve(root, sourcePath), 'utf8');
const source = JSON.parse(raw);
const matrix = exportMatrix(raw);
unique(source.ions, i => i.id, 'ions');
unique(source.overrides, i => i.coordinate, 'overrides');
unique(source.exclusions, i => i.coordinate, 'exclusions');
unique(matrix.conditions, i => i.id, 'conditions');
unique(matrix.evidence, i => i.id, 'evidence');
unique(source.matrix2D.cells, i => `${i.cationId}|${i.anionId}`, 'annotations');
const ions = new Map(source.ions.map(i => [i.id, i]));
const conditions = new Set(matrix.conditions.map(i => i.id));
const evidence = new Set(matrix.evidence.map(i => i.id));
const excluded = new Map(source.exclusions.map(i => [i.coordinate, i.reason]));
// Independently compare atom inventories; conventional formulas may reorder atoms.
function atomCounts(formula) {
  const value = ascii(formula); let position = 0;
  const number = () => { const start = position; while (/\d/.test(value[position] ?? '')) position++; const n = position === start ? 1 : Number(value.slice(start, position)); assert(Number.isSafeInteger(n) && n > 0, `Invalid subscript: ${formula}`); return n; };
  const group = nested => {
    const result = {};
    while (position < value.length && value[position] !== ')') {
      let part;
      if (value[position] === '(') { position++; part = group(true); assert.equal(value[position++], ')', `Missing closing parenthesis: ${formula}`); }
      else { const match = /^[A-Z][a-z]?/.exec(value.slice(position)); assert(match, `Invalid formula: ${formula}`); position += match[0].length; part = { [match[0]]: 1 }; }
      const count = number(); for (const [element, amount] of Object.entries(part)) result[element] = (result[element] ?? 0) + amount * count;
    }
    assert(Object.keys(result).length > 0, `Empty formula group: ${formula}`);
    if (!nested) assert.equal(position, value.length, `Unexpected parenthesis: ${formula}`);
    return result;
  };
  return group(false);
}
for (const ion of ions.values()) {
  assert(Number.isInteger(ion.charge) && ion.charge !== 0, `Invalid charge: ${ion.id}`);
  assert(Number.isFinite(ion.molarMass) && ion.molarMass > 0, `Invalid mass: ${ion.id}`);
  assert.equal(atomCounts(ion.formula).O ?? 0, ion.oxygenCount, `Oxygen count: ${ion.id}`);
}
for (const annotation of source.matrix2D.cells) {
  assert(ions.get(annotation.cationId)?.charge > 0 && ions.get(annotation.anionId)?.charge < 0, 'Invalid annotation ion IDs');
}
assert.equal(matrix.cells.length, matrix.cations.length * matrix.anions.length);
unique(matrix.cells, c => c.coordinate, 'cells');
for (const cell of matrix.cells) {
  const c = ions.get(cell.cationId), a = ions.get(cell.anionId);
  assert.equal(cell.cationCount * c.charge + cell.anionCount * a.charge, 0, `Charge: ${cell.coordinate}`);
  assert.equal(gcd(cell.cationCount, cell.anionCount), 1, `Ratio: ${cell.coordinate}`);
  assert(cell.cationCount > 0 && cell.anionCount > 0);
  assert(['formalComposition', 'literatureSupported', 'excluded', 'unsupported'].includes(cell.status), `Unknown status: ${cell.status}`);
  assert(cell.conditionIds.every(id => conditions.has(id)), `Condition reference: ${cell.coordinate}`);
  assert(cell.evidenceIds.every(id => evidence.has(id)), `Evidence reference: ${cell.coordinate}`);
  if (cell.status === 'literatureSupported') assert(cell.evidenceIds.length > 0, `Missing evidence: ${cell.coordinate}`);
  if (excluded.has(cell.coordinate)) {
    assert.equal(cell.status, 'excluded'); assert.equal(cell.formula, null); assert.equal(cell.exceptionReason, excluded.get(cell.coordinate));
  } else if (cell.propertyRecord?.formula) assert.equal(cell.formula, ascii(cell.propertyRecord.formula), 'Reviewed formula must prevail');
  if (cell.formula) {
    const expected = {};
    for (const [ion, count] of [[c, cell.cationCount], [a, cell.anionCount]]) for (const [element, amount] of Object.entries(atomCounts(ion.formula))) expected[element] = (expected[element] ?? 0) + count * amount;
    assert.deepEqual(atomCounts(cell.formula), expected, `Atom conservation: ${cell.coordinate}`);
  }
}
for (const [coordinate, formula, cationCount, anionCount] of [
  ['sodium|sulfate', 'Na2SO4', 2, 1], ['calcium|phosphate', 'Ca3(PO4)2', 3, 2],
  ['copper-two|hydroxide', 'Cu(OH)2', 1, 2], ['hydrogen|acetate', 'CH3COOH', 1, 1]
]) {
  const cell = matrix.cells.find(c => c.coordinate === coordinate);
  assert.deepEqual([cell.formula, cell.cationCount, cell.anionCount], [formula, cationCount, anionCount]);
}
assert.equal(matrix.cells.find(c => c.coordinate === 'ammonium|hydroxide').status, 'excluded');
assert.equal(matrix.cells.find(c => c.coordinate === 'silver|hydroxide').status, 'excluded');
assert(matrix.cells.every(c => !c.coordinate.startsWith('oxide:')));
assert.equal(read(outputPath), `${JSON.stringify(matrix, null, 2)}\n`, 'Stale matrix export');

const parityIndex = process.argv.indexOf('--unity-parity');
if (parityIndex >= 0) {
  const unity = JSON.parse(read(process.argv[parityIndex + 1]));
  const projection = cells => cells.map(c => ({ anionId: c.anionId, cationId: c.cationId, formula: c.formula || null, cationCount: c.cationCount, anionCount: c.anionCount, status: c.status })).sort((a, b) => `${a.anionId}|${a.cationId}`.localeCompare(`${b.anionId}|${b.cationId}`));
  assert.deepEqual(projection(unity.cells), projection(matrix.cells), 'Unity/Pages 2D cell drift');
}
console.log(JSON.stringify({ result: 'passed', jsonCount, assetCount, relativeMarkdownLinks: linkCount, directPackages: Object.keys(manifest.dependencies).length, cells: matrix.cells.length, exclusions: matrix.cells.filter(c => c.status === 'excluded').length, unityParity: parityIndex >= 0 }, null, 2));
