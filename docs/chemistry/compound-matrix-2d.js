const $ = id => document.getElementById(id);
let language = 'vi';
try { language = localStorage.getItem('chemistrylab-docs-language') === 'en' ? 'en' : 'vi'; } catch {}
let data, selected = 'copper-two|hydroxide';
const text = (vi, en) => language === 'en' ? en : vi;
const statusLabel = status => ({
  formalComposition: text('Thành phần hình thức', 'Formal composition'),
  literatureSupported: text('Có nguồn trong phạm vi ghi chú', 'Evidence within stated scope'),
  excluded: text('Tổ hợp bị loại', 'Excluded combination'),
  unsupported: text('Chưa hỗ trợ', 'Unsupported')
})[status] ?? status;
const marker = status => ({ literatureSupported: '●', formalComposition: '○', excluded: '×', unsupported: '?' })[status] ?? '?';
const node = (tag, content, className) => { const n = document.createElement(tag); if (content !== undefined) n.textContent = content; if (className) n.className = className; return n; };
const ionLabel = ion => `${ion.formula} (${ion.charge > 0 ? '+' : '−'}${Math.abs(ion.charge)})`;
function applyLanguage() {
  document.documentElement.lang = language;
  for (const element of document.querySelectorAll('[data-vi][data-en]')) element.textContent = element.dataset[language];
  $('language').textContent = language === 'en' ? 'Tiếng Việt' : 'English';
  if (data) { populateAxes(); render(); }
}
function populateAxes() {
  for (const [id, ions] of [['anion', data.anions], ['cation', data.cations]]) {
    const select = $(id), previous = select.value;
    select.replaceChildren(new Option(text('Tất cả', 'All'), ''));
    for (const ion of ions) select.add(new Option(`${ionLabel(ion)} · ${ion.id}`, ion.id));
    select.value = previous;
  }
}
function showDetail(cell) {
  selected = cell.coordinate;
  const panel = $('detail');
  const heading = node('h3', cell.formula ?? '—'); heading.id = 'detail-title';
  panel.replaceChildren(node('span', `${marker(cell.status)} ${statusLabel(cell.status)}`, 'status'), heading, node('p', `${cell.anionId} × ${cell.cationId}`));
  const cation = data.cations.find(i => i.id === cell.cationId), anion = data.anions.find(i => i.id === cell.anionId);
  const pairs = [
    ['Cation', ionLabel(cation)], ['Anion', ionLabel(anion)],
    [text('Tỉ lệ cation : anion', 'Cation : anion ratio'), `${cell.cationCount} : ${cell.anionCount}`],
    [text('Tổng điện tích', 'Total charge'), `${cell.cationCount}×(${cation.charge}) + ${cell.anionCount}×(${anion.charge}) = 0`],
    [text('Khóa tương thích', 'Legacy key'), cell.coordinate]
  ];
  const dl = node('dl'); for (const [k, v] of pairs) dl.append(node('dt', k), node('dd', v)); panel.append(dl);
  if (cell.exceptionReason) panel.append(node('p', cell.exceptionReason, 'excluded'));
  panel.append(node('p', cell.notes));
  panel.append(node('h4', text('Bối cảnh & điều kiện', 'Context & conditions')));
  const conditions = node('ul');
  for (const id of cell.conditionIds) { const condition = data.conditions.find(c => c.id === id); conditions.append(node('li', condition ? `${condition.label ?? condition.title ?? id}: ${condition.description ?? ''}` : id)); }
  panel.append(conditions);
  if (cell.propertyRecord) {
    panel.append(node('h4', text('Tính chất từ danh mục cũ', 'Legacy property record')));
    panel.append(node('p', `${cell.propertyRecord.phase ?? '—'} · ${cell.propertyRecord.solubility ?? '—'} · ${cell.propertyRecord.appearance ?? ''}`));
    panel.append(node('p', text('Ghi đè tính chất trong dự án; không phải chứng nhận đầy đủ về tính bền hoặc khả năng điều chế.', 'Project property override; not a full certification of stability or synthesis feasibility.')));
  }
  panel.append(node('h4', text('Nguồn & phạm vi', 'Evidence & scope')));
  const sources = node('ul');
  for (const id of cell.evidenceIds) {
    const evidence = data.evidence.find(e => e.id === id); if (!evidence) continue;
    const li = node('li'); const link = node('a', evidence.title ?? id);
    if (/^https:\/\//.test(evidence.url)) { link.href = evidence.url; link.target = '_blank'; link.rel = 'noopener noreferrer'; }
    li.append(link, node('p', evidence.scope ?? '')); sources.append(li);
  }
  panel.append(sources);
  if (!sources.children.length) panel.append(node('p', text('Chưa có nguồn riêng cho ô này.', 'No cell-specific evidence recorded.')));
  for (const button of $('matrix').querySelectorAll('button')) { const active = button.dataset.coordinate === selected; button.setAttribute('aria-pressed', String(active)); button.tabIndex = active ? 0 : -1; }
  if (!$('matrix').querySelector('button[tabindex="0"]')) { const first = $('matrix').querySelector('button'); if (first) first.tabIndex = 0; }
}
function render() {
  const search = $('search').value.trim().toLowerCase().replace(/[₀-₉]/g, d => '₀₁₂₃₄₅₆₇₈₉'.indexOf(d));
  const matches = cell => !search || `${cell.formula ?? ''} ${cell.anionId} ${cell.cationId}`.toLowerCase().includes(search);
  const columns = data.cations.filter(i => !$('cation').value || i.id === $('cation').value);
  const rows = data.anions.filter(i => (!$('anion').value || i.id === $('anion').value) && data.cells.some(c => c.anionId === i.id && columns.some(col => col.id === c.cationId) && matches(c)));
  const head = node('tr'); const corner = node('th', 'Anion ↓ / Cation →'); corner.scope = 'col'; head.append(corner);
  for (const cation of columns) { const th = node('th', ionLabel(cation)); th.scope = 'col'; th.title = cation.id; head.append(th); }
  $('matrix').tHead.replaceChildren(head);
  const body = $('matrix').tBodies[0]; body.replaceChildren();
  let found = 0;
  for (const anion of rows) {
    const row = node('tr'), heading = node('th', ionLabel(anion)); heading.scope = 'row'; heading.title = anion.id; row.append(heading);
    for (const cation of columns) {
      const cell = data.cells.find(c => c.cationId === cation.id && c.anionId === anion.id);
      const td = node('td'), button = node('button', cell.formula ?? '—', cell.status === 'excluded' ? 'excluded' : cell.status === 'literatureSupported' ? 'supported' : '');
      button.type = 'button'; button.dataset.coordinate = cell.coordinate;
      button.setAttribute('aria-label', `${cell.formula ?? cell.coordinate}; ${statusLabel(cell.status)}; ${cation.id}, ${anion.id}`);
      button.append(node('span', `${marker(cell.status)} ${cell.cationCount}:${cell.anionCount}`, 'mark'));
      if (matches(cell)) found++; else button.classList.add('unmatched');
      button.addEventListener('click', () => showDetail(cell)); td.append(button); row.append(td);
    }
    body.append(row);
  }
  $('counts').textContent = `${data.anions.length} anion × ${data.cations.length} cation · ${found} ${text('ô khớp', 'matching cells')}`;
  showDetail(data.cells.find(c => c.coordinate === selected) ?? data.cells[0]);
}
$('matrix').addEventListener('keydown', event => {
  const button = event.target.closest('button'); if (!button) return;
  const rows = Array.from($('matrix').tBodies[0].rows), r = rows.indexOf(button.closest('tr')), c = button.closest('td').cellIndex - 1;
  let nextR = r, nextC = c;
  switch (event.key) { case 'ArrowLeft': nextC--; break; case 'ArrowRight': nextC++; break; case 'ArrowUp': nextR--; break; case 'ArrowDown': nextR++; break; case 'Home': nextC = 0; break; case 'End': nextC = rows[r].cells.length - 2; break; default: return; }
  const next = rows[Math.max(0, Math.min(rows.length - 1, nextR))]?.cells[Math.max(1, Math.min(rows[r].cells.length - 1, nextC + 1))]?.querySelector('button');
  if (next) { event.preventDefault(); next.focus(); showDetail(data.cells.find(cell => cell.coordinate === next.dataset.coordinate)); }
});
$('language').addEventListener('click', () => { language = language === 'vi' ? 'en' : 'vi'; try { localStorage.setItem('chemistrylab-docs-language', language); } catch {} applyLanguage(); });
for (const id of ['anion', 'cation']) $(id).addEventListener('change', render);
$('search').addEventListener('input', render);
$('reset').addEventListener('click', () => { for (const id of ['search', 'anion', 'cation']) $(id).value = ''; selected = 'copper-two|hydroxide'; render(); });
applyLanguage();
try {
  const response = await fetch('./compound-matrix-2d.json'); if (!response.ok) throw new Error(`HTTP ${response.status}`);
  data = await response.json(); if (data.rowAxis !== 'anionId' || data.columnAxis !== 'cationId' || !data.cells?.length) throw new Error('Unexpected matrix schema');
  populateAxes(); render(); $('provenance').textContent = `${data.source} · SHA-256 ${data.sourceSha256}`;
} catch (error) { $('error').hidden = false; $('error').textContent = `${text('Không tải được dữ liệu. Mở trang qua HTTP và kiểm tra file JSON.', 'Unable to load data. Serve this page over HTTP and check the JSON file.')} ${error.message}`; $('counts').textContent = text('Dữ liệu chưa tải', 'Data unavailable'); for (const id of ['search','anion','cation','reset']) $(id).disabled = true; }
