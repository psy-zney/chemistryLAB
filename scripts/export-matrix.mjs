import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

export const root = fileURLToPath(new URL('../', import.meta.url));
export const sourcePath = 'Assets/ChemistryLab/Resources/Chemistry/compound-generation-matrix.json';
export const outputPath = 'docs/chemistry/compound-matrix-2d.json';
export const ascii = value => value.replace(/[₀-₉]/g, digit => '₀₁₂₃₄₅₆₇₈₉'.indexOf(digit));
export function gcd(a, b) { return b ? gcd(b, a % b) : a; }
const part = (ion, count) => `${ion.polyatomic && count > 1 ? `(${ascii(ion.formula)})` : ascii(ion.formula)}${count > 1 ? count : ''}`;

export function exportMatrix(raw) {
  const source = JSON.parse(raw);
  const model = source.matrix2D;
  if (!model || model.rowAxis !== 'anionId' || model.columnAxis !== 'cationId') throw new Error('Missing canonical anion × cation 2D contract');
  const cations = source.ions.filter(ion => ion.charge > 0);
  const anions = source.ions.filter(ion => ion.charge < 0);
  const annotations = new Map(model.cells.map(cell => [`${cell.cationId}|${cell.anionId}`, cell]));
  const overrides = new Map(source.overrides.map(item => [item.coordinate, item]));
  const exclusions = new Map(source.exclusions.map(item => [item.coordinate, item]));
  const cells = anions.flatMap(anion => cations.map(cation => {
    const coordinate = `${cation.id}|${anion.id}`;
    const divisor = gcd(cation.charge, -anion.charge);
    const cationCount = -anion.charge / divisor;
    const anionCount = cation.charge / divisor;
    const annotation = annotations.get(coordinate);
    const propertyRecord = overrides.get(coordinate) ?? null;
    const exception = exclusions.get(coordinate);
    return {
      anionId: anion.id, cationId: cation.id, coordinate,
      formula: exception ? null : propertyRecord?.formula ? ascii(propertyRecord.formula) : part(cation, cationCount) + part(anion, anionCount),
      cationCount, anionCount,
      status: exception ? 'excluded' : annotation?.status ?? model.defaultStatus,
      conditionIds: annotation?.conditionIds ?? model.defaultConditionIds,
      evidenceIds: annotation?.evidenceIds ?? model.defaultEvidenceIds,
      notes: annotation?.notes ?? 'Formal charge-balanced composition; existence, stability and reaction feasibility have not been established for this cell.',
      exceptionReason: exception?.reason ?? null,
      propertyReviewStatus: propertyRecord ? 'propertyReviewed' : 'heuristic',
      authorizesReaction: false,
      propertyRecord
    };
  }));
  return {
    schemaVersion: '2.0', rowAxis: model.rowAxis, columnAxis: model.columnAxis,
    source: sourcePath, sourceSha256: createHash('sha256').update(raw).digest('hex'),
    scope: 'Formal ionic composition with explicitly scoped evidence. Selecting a cell never authorizes synthesis.',
    cations, anions, conditions: model.conditions, evidence: model.evidence, cells,
    separateOxideRecords: source.overrides.filter(item => item.coordinate.startsWith('oxide:')),
    separateOxideExclusions: source.exclusions.filter(item => item.coordinate.startsWith('oxide:'))
  };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const result = `${JSON.stringify(exportMatrix(readFileSync(resolve(root, sourcePath), 'utf8')), null, 2)}\n`;
  const destination = resolve(root, outputPath);
  if (process.argv.includes('--check')) {
    if (readFileSync(destination, 'utf8') !== result) throw new Error('Matrix export stale. Run node scripts/export-matrix.mjs');
    console.log('Matrix export matches canonical source and source hash.');
  } else { writeFileSync(destination, result); console.log(`Wrote ${outputPath}`); }
}
