"""Focused repository checks; does not launch Unity or change chemistry."""
import json, pathlib, re, subprocess
root=pathlib.Path(__file__).resolve().parents[3]
errors=[]
json_count=0
for base in ('Assets/ChemistryLab','docs','Packages','BuildReports','SourceAssets/Original/ModernLab'):
    for path in (root/base).rglob('*.json'):
        try: json.loads(path.read_text(encoding='utf-8-sig'));json_count+=1
        except Exception as error: errors.append(f'{path.relative_to(root)}: {error}')
meta_count=0
guids={}
for path in (root/'Assets').rglob('*'):
    if path.name.endswith('.meta'):
        original=path.with_name(path.name[:-5])
        if not original.exists(): errors.append(f'Orphan meta: {path.relative_to(root)}')
        match=re.search(r'^guid: ([0-9a-f]{32})$',path.read_text(encoding='utf-8-sig'),re.M)
        if match:
            if match[1] in guids: errors.append(f'Duplicate GUID: {path} / {guids[match[1]]}')
            guids[match[1]]=path
        continue
    if not path.with_name(path.name+'.meta').exists(): errors.append(f'Missing meta: {path.relative_to(root)}')
    meta_count+=1
manifest=json.loads((root/'Packages/manifest.json').read_text())['dependencies']
locked=json.loads((root/'Packages/packages-lock.json').read_text())['dependencies']
for name,version in manifest.items():
    if name not in locked or locked[name]['version']!=version: errors.append(f'Package mismatch: {name}')
docs=['README.md','docs/README.md','docs/gameplay/lab-scene-production-plan.md',
      'docs/gameplay/staged-sample-reaction-presentation.md','docs/gameplay/procedural-reference-props.md',
      'docs/gameplay/modern-lab-visual-upgrade.md']
links=0
for name in docs:
    path=root/name
    if not path.exists(): continue
    for target in re.findall(r'\]\(([^)]+)\)',path.read_text(encoding='utf-8-sig')):
        if re.match(r'^(https?://|mailto:|#)',target): continue
        target=target.split('#')[0].strip('<>')
        if target and not (path.parent/target).exists(): errors.append(f'Broken Markdown link: {name} -> {target}')
        links+=1
result=subprocess.run(['git','diff','--check'],cwd=root,capture_output=True,text=True)
if result.returncode: errors.append(result.stdout+result.stderr)
print(json.dumps({'jsonFilesParsed':json_count,'assetsWithMetaChecked':meta_count,
    'packageDependenciesChecked':len(manifest),'markdownLinksChecked':links,'diffCheckPassed':result.returncode==0,'errors':errors},indent=2))
raise SystemExit(bool(errors))
