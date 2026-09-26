"""Original metre-scale lab kit. Run with Blender 5.2 --background --python.
No downloaded geometry or textures. Unity coordinates are mapped to Blender.
"""
import bpy, bmesh, math, json, os
import numpy as np
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'SourceAssets/Original/ModernLab'
RUNTIME = ROOT / 'Assets/ChemistryLab/Art/ModernLab'
SOURCE.mkdir(parents=True, exist_ok=True)
RUNTIME.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'ChemistryLab_ModernLab_Metres'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

palette = {
    'Ivory': (.79,.81,.79,1), 'Teal': (.10,.33,.34,1),
    'Steel': (.48,.53,.55,1), 'DarkSteel': (.16,.20,.22,1),
    'Worktop': (.065,.085,.09,1), 'Rubber': (.06,.075,.075,1),
    'Glass': (.88,.96,.98,.08), 'Amber': (.35,.17,.055,.28),
    'Diffuser': (.90,.96,1,1), 'Window': (.67,.82,.88,1),
    'Tile': (.67,.70,.69,1), 'Ceramic': (.88,.90,.86,1),
    'Label': (.93,.93,.88,1),
}
materials = {}
for name, rgba in palette.items():
    material = bpy.data.materials.new(name)
    material.diffuse_color = rgba
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = .34 if name in ('Steel','DarkSteel') else .62
    bsdf.inputs['Metallic'].default_value = .75 if name in ('Steel','DarkSteel') else 0
    materials[name] = material

modules = {}
parts = {}
def coord(p): return (p[0], -p[2], p[1])
def module(name):
    empty = bpy.data.objects.new(name, None)
    scene.collection.objects.link(empty)
    modules[name] = empty
    parts[name] = []
    return name

def finish(obj, mod, mat, bevel=0):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Light catching edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        normal = obj.modifiers.new('Face weighted normals', 'WEIGHTED_NORMAL')
        normal.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=normal.name)
    obj.data.materials.append(materials[mat])
    obj.parent = modules[mod]
    parts[mod].append(obj)
    return obj

def box(mod, name, pos, size, mat='Ivory', bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=coord(pos))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (size[0],size[2],size[1])
    return finish(obj, mod, mat, min(bevel,min(size)*.22))

def cylinder(mod,name,pos,radius,height,mat='Steel',vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=height, location=coord(pos))
    obj = bpy.context.object
    obj.name = name
    for poly in obj.data.polygons: poly.use_smooth = len(poly.vertices)==4
    return finish(obj,mod,mat,.001)

def pipe(mod, name, a, b, radius=.012, mat='Steel'):
    direction = Vector(coord(b)) - Vector(coord(a))
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=direction.length,location=(Vector(coord(a))+Vector(coord(b)))*.5)
    obj=bpy.context.object
    obj.name=name
    obj.rotation_euler=direction.to_track_quat('Z','Y').to_euler()
    for poly in obj.data.polygons: poly.use_smooth=True
    return finish(obj,mod,mat,0)

def lathe(mod,name,pos,profile,mat='Glass',segments=48):
    vertices=[]
    faces=[]
    for y,r in profile:
        for i in range(segments):
            angle=2*math.pi*i/segments
            vertices.append(coord((pos[0]+r*math.cos(angle),pos[1]+y,pos[2]+r*math.sin(angle))))
    for j in range(len(profile)-1):
        for i in range(segments):
            a=j*segments+i; b=j*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj
    obj.select_set(True)
    bm=bmesh.new(); bm.from_mesh(mesh); bmesh.ops.recalc_face_normals(bm,faces=bm.faces); bm.to_mesh(mesh); bm.free()
    for poly in mesh.polygons: poly.use_smooth=True
    return finish(obj,mod,mat)

def cabinet(mod,x,z,width=.86,depth=.77,side=1):
    box(mod,'Cabinet carcass',(x,.46,z),(width,.77,depth),'Ivory')
    front=z+side*(depth*.5+.012)
    box(mod,'Recessed toe kick',(x,.075,z+side*.025),(width-.05,.12,depth-.04),'DarkSteel')
    for y,h in ((.74,.16),(.50,.28),(.22,.25)):
        box(mod,'Drawer reveal',(x,y,front),(width-.03,h,.018),'DarkSteel',.002)
        box(mod,'Drawer face',(x,y,front+side*.012),(width-.052,h-.018,.021),'Ivory',.003)
        pipe(mod,'Drawer handle',(x-.13,y+.03,front+side*.045),(x+.13,y+.03,front+side*.045),.009)

m=module('Workbench')
box(m,'Resin slab',(0,.97,0),(5.4,.08,2.2),'Worktop',.014)
box(m,'Undertop support',(0,.90,0),(5.28,.065,2.08),'DarkSteel')
for x in (-2.35,2.35):
    for z in (-.87,.87):
        box(m,'Square steel leg',(x,.445,z),(.095,.86,.095),'DarkSteel')
        cylinder(m,'Levelling foot',(x,.035,z),.058,.034,'Rubber')
    box(m,'Leg cross rail',(x,.19,0),(.06,.055,1.83),'Steel')
for x in (-1.67,1.67):
    for side in (-1,1): cabinet(m,x,side*.62,.92,.74,side)
for side in (-1,1):
    box(m,'Teal bench fascia',(0,.867,side*1.032),(5.18,.032,.018),'Teal',.003)
    box(m,'Recessed service rail',(0,.77,side*1.015),(1.42,.09,.025),'DarkSteel')
    for x in (-.48,0,.48):
        box(m,'Power socket',(x,.77,side*1.04),(.075,.065,.012),'Ivory',.002)
        for dx in (-.013,.013): box(m,'Socket pin',(x+dx,.77,side*1.047),(.005,.02,.002),'Rubber',0)

m=module('Hood')
box(m,'Work surface',(0,.97,0),(4.5,.08,1.75),'Worktop',.012)
for x in (-1.59,0,1.59): cabinet(m,x,0,1.35,1.55,1)
box(m,'Rear ceramic liner',(0,1.94,-.74),(4.02,1.85,.04),'Ceramic')
for x in (-2.10,2.10):
    box(m,'Side column',(x,1.99,0),(.22,2.16,1.75),'Ivory',.01)
    box(m,'Sash track',(x-math.copysign(.12,x),2.07,.51),(.036,1.88,.03),'Steel',.002)
box(m,'Header housing',(0,3.08,-.13),(4.5,.46,1.5),'Ivory',.016)
box(m,'Header teal band',(0,2.925,.635),(4.25,.055,.014),'Teal',.003)
box(m,'Sash glass',(0,2.08,.50),(3.95,1.12,.004),'Glass',0)
for y in (1.52,2.64): box(m,'Sash edge frame',(0,y,.51),(4.04,.045,.04),'Steel',.004)
pipe(m,'Sash handle',(-.38,1.57,.545),(.38,1.57,.545),.015)
box(m,'Interior lamp',(0,2.74,-.25),(3.62,.022,.24),'Diffuser')
for x in (-1.5,-.75,0,.75,1.5):
    for y in (1.16,2.50): box(m,'Rear extraction slot',(x,y,-.709),(.55,.027,.006),'DarkSteel',.002)
box(m,'Baffle divider',(0,1.83,-.711),(.018,1.5,.006),'Steel',.001)
box(m,'Exhaust duct',(0,3.40,-.4),(.72,.23,.58),'Steel')
for x in (-.30,.30): box(m,'Duct seam',(x,3.40,-.4),(.015,.25,.6),'DarkSteel',.002)

for side,name in ((-1,'StorageLeft'),(1,'StorageRight')):
    m=module(name)
    x=side*6.42
    box(m,'Cabinet rear',(x,1.55,-.9),(.38,3.1,9.2),'Ivory')
    for row in range(4):
        y=.42+row*.72
        box(m,'Folded shelf',(x-side*.38,y,-.9),(.72,.08,9),'Steel',.004)
        box(m,'Teal shelf edge',(x-side*.75,y,-.9),(.015,.065,8.95),'Teal',.002)
        for z in (-4.7,-3.35,-1.75,-.15,1.45,2.9):
            box(m,'Shelf bracket',(x-side*.35,y-.09,z),(.50,.10,.025),'Steel',.002)
    for z in (-5.1,3.3): box(m,'Shelf end',(x-side*.38,1.55,z),(.72,3.1,.12),'Ivory')
    for z in (-4.0,-.9,2.2):
        box(m,'Back panel seam',(x-side*.194,1.6,z),(.008,2.95,.018),'Steel',.001)

m=module('Sink')
for x in (-.90,.90): cabinet(m,x,0,1.05,1.95,1)
# Four pieces leave a real basin opening rather than a solid slab under water.
box(m,'Left worktop',(-1.13,.985,0),(.64,.09,2.2),'Worktop')
box(m,'Right worktop',(1.13,.985,0),(.64,.09,2.2),'Worktop')
box(m,'Front worktop',(0,.985,.84),(1.66,.09,.52),'Worktop')
box(m,'Rear worktop',(0,.985,-.84),(1.66,.09,.52),'Worktop')
box(m,'Basin bottom',(0,.76,0),(1.55,.025,1.12),'Steel')
for x in (-.79,.79): box(m,'Basin side',(x,.87,0),(.026,.23,1.16),'Steel')
for z in (-.57,.57): box(m,'Basin side',(0,.87,z),(1.6,.23,.026),'Steel')
for x in (-.80,.80): box(m,'Basin rolled lip',(x,1.035,0),(.026,.014,1.19),'Steel',.004)
for z in (-.59,.59): box(m,'Basin rolled lip',(0,1.035,z),(1.64,.014,.026),'Steel',.004)
cylinder(m,'Drain',(0,.778,0),.055,.005,'DarkSteel')
pipe(m,'Faucet riser',(0,1.03,-.74),(0,1.42,-.74),.022)
pipe(m,'Faucet top',(0,1.42,-.74),(0,1.42,-.22),.022)
pipe(m,'Faucet outlet',(0,1.42,-.22),(0,1.36,-.22),.022)
pipe(m,'Tap lever',(.04,1.08,-.74),(.18,1.13,-.74),.012)
box(m,'Soap bottle',(.98,1.13,-.66),(.10,.20,.10),'Ceramic')
pipe(m,'Soap pump',(.98,1.24,-.66),(.98,1.24,-.52),.008,'DarkSteel')

m=module('RoomDetail')
for x in (-6.82,6.82):
    box(m,'Wall skirting',(x,.08,0),(.035,.16,11.75),'Steel',.003)
    box(m,'Service trunking',(x,3.27,0),(.09,.11,11.75),'Ivory',.004)
box(m,'Back skirting',(0,.08,-5.85),(13.7,.16,.035),'Steel',.003)
box(m,'Back service rail',(0,3.35,-5.83),(13.65,.085,.09),'Ivory')
# Close the former invisible front boundary with framed daylight windows.
box(m,'Front lower wall',(0,.48,5.87),(13.7,.96,.075),'Ivory')
box(m,'Front upper wall',(0,3.35,5.87),(13.7,.3,.075),'Ivory')
for x in (-5,-2.5,0,2.5,5):
    box(m,'Window daylight panel',(x,2.03,5.89),(2.24,2.10,.015),'Window',0)
    for dx in (-1.15,1.15): box(m,'Window jamb',(x+dx,2.03,5.82),(.055,2.18,.09),'Steel',.004)
    for y in (.95,2.05,3.11): box(m,'Window transom',(x,y,5.82),(2.36,.045,.09),'Steel',.003)
    box(m,'Window sill',(x,.94,5.78),(2.4,.045,.24),'Ceramic')
for x in (-4.68,4.68):
    box(m,'Back window daylight',(x,2.22,-5.835),(2.2,1.86,.022),'Window',0)
    for dx in (-1.12,1.12): box(m,'Back window jamb',(x+dx,2.22,-5.80),(.045,1.98,.06),'Steel',.004)
    for y in (1.25,2.22,3.2): box(m,'Back window rail',(x,y,-5.80),(2.27,.045,.06),'Steel',.003)
# A flush door stays behind the existing safety/periodic-table sightline.
box(m,'Door leaf',(-4.50,1.075,-5.77),(1.18,2.15,.035),'Teal')
for x in (-5.12,-3.88): box(m,'Door frame',(x,1.10,-5.73),(.055,2.2,.085),'Steel')
box(m,'Door lintel',(-4.5,2.2,-5.73),(1.30,.06,.085),'Steel')
box(m,'Door vision panel',(-4.5,1.63,-5.735),(.36,.60,.014),'Window',0)
pipe(m,'Door handle',(-4.03,.97,-5.69),(-4.21,.97,-5.69),.012)
for x in range(-6,7,2): box(m,'Ceiling channel',(x,3.487,0),(.016,.01,11.7),'Steel',0)
for z in range(-5,6,2): box(m,'Ceiling channel',(0,3.487,z),(13.7,.01,.016),'Steel',0)
for x in (-3.3,3.3):
    box(m,'Vent surround',(x,3.477,-3.1),(.8,.02,.42),'Ivory')
    for i in range(9): box(m,'Vent louvre',(x-.31+i*.078,3.461,-3.1),(.017,.012,.33),'DarkSteel',.002)

m=module('PreparationTray')
box(m,'Teal silicone liner',(0,.016,0),(.76,.025,.48),'Teal',.009)
for x in (-.39,.39): box(m,'Tray rim',(x,.032,0),(.022,.048,.50),'Steel',.005)
for z in (-.25,.25): box(m,'Tray rim',(0,.032,z),(.80,.048,.022),'Steel',.005)
for x in (-.25,0,.25): box(m,'Grip ribs',(x,.031,0),(.005,.003,.31),'Rubber',.001)

m=module('BenchTools')
# Small glassware away from vessel/tray raycasts; reviewed flask/tubes stay untouched.
lathe(m,'Beaker',(2.03,1.01,-.47),[(0,.001),(0,.045),(.005,.048),(.13,.048),(.135,.049),(.135,.046),(.008,.045),(.008,.001)])
lathe(m,'Graduated cylinder',(2.40,1.01,-.44),[(0,.04),(.006,.04),(.006,.022),(.22,.022),(.225,.023),(.225,.020),(.009,.020)],segments=40)
for i in range(1,9):
    box(m,'Cylinder graduation',(2.40,1.025+i*.021,-.417),(.020 if i%2==0 else .012,.0015,.0008),'Label',0)
for i in range(1,5): box(m,'Beaker graduation',(2.03,1.025+i*.022,-.421),(.028,.0015,.0008),'Label',0)
box(m,'Clamp base',(.8,1.03,.70),(.30,.025,.24),'DarkSteel')
pipe(m,'Clamp stand',(.8,1.035,.70),(.8,1.68,.70),.012)
pipe(m,'Clamp arm',(.8,1.52,.70),(.50,1.52,.70),.009)
cylinder(m,'Clamp knob',(.8,1.52,.70),.025,.033,'Rubber')
for x in (-2.20,-2.08):
    box(m,'Goggle lens',(x,1.045,-.57),(.09,.018,.060),'Glass',.008)
    box(m,'Goggle frame',(x,1.037,-.57),(.10,.018,.068),'Teal',.006)
pipe(m,'Goggle bridge',(-2.16,1.049,-.57),(-2.12,1.049,-.57),.006,'Teal')
for x in (-1.96,-1.82):
    box(m,'Folded nitrile glove',(x,1.027,-.66),(.11,.009,.12),'Teal',.009)
    for i in range(4): box(m,'Glove finger',(x-.042+i*.028,1.027,-.76),(.023,.008,.10),'Teal',.004)

m=module('ReagentBottle')
# Separate shell/cap: chemical contents and formula labels remain owned by gameplay.
lathe(m,'Bottle shell',(0,0,0),[(.009,.001),(.009,.042),(.015,.044),(.204,.044),(.230,.024),(.277,.023),(.282,.024),(.282,.020),(.235,.020),(.207,.041),(.015,.039),(.015,.001)])
cylinder(m,'Screw cap',(0,.298,0),.025,.030,'Rubber',32)
for i in range(24):
    a=2*math.pi*i/24
    box(m,'Cap knurl',(.025*math.cos(a),.298,.025*math.sin(a)),(.002,.023,.002),'DarkSteel',.0003)
lathe(m,'Cap tamper ring',(0,0,0),[(.278,.024),(.282,.024),(.282,.023),(.278,.023)],'DarkSteel',32)

# Join by module/material: finite renderer count rather than one draw per bevel part.
audits=[]
for name,empty in modules.items():
    groups={}
    for obj in parts[name]: groups.setdefault(obj.data.materials[0].name,[]).append(obj)
    for material,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join()
        obj=bpy.context.object
        obj.name=name+'_'+material
        scene.cursor.location=(0,0,0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.016)
        bpy.ops.object.mode_set(mode='OBJECT')
        obj.data.uv_layers.new(name='Lightmap')
        bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(obj.data);bm.free()
        audits.append({'module':name,'material':material,'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons),'uvSets':len(obj.data.uv_layers)})

# Original 1K subtle material textures; fixed seed, no external image inputs.
rng=np.random.default_rng(7364)
for name,base in [('Worktop',(0.90,0.92,0.92)),('Paint',(.97,.98,.97)),('Floor',(.94,.95,.94))]:
    size=1024
    noise=rng.random((size,size),dtype=np.float32)
    amp=.055 if name=='Worktop' else .028
    pixels=np.empty((size,size,4),dtype=np.float32)
    for c in range(3): pixels[:,:,c]=base[c]+(noise-.5)*amp
    pixels[:,:,3]=1
    img=bpy.data.images.new(name+'_Albedo',width=size,height=size)
    img.pixels.foreach_set(pixels.ravel())
    img.filepath_raw=str(RUNTIME/(name+'_Albedo.png'));img.file_format='PNG';img.save()
    normal=np.empty((size,size,4),dtype=np.float32)
    normal[:,:,0]=.5+np.gradient(noise,axis=1)*.06
    normal[:,:,1]=.5+np.gradient(noise,axis=0)*.06
    normal[:,:,2]=1;normal[:,:,3]=1
    img=bpy.data.images.new(name+'_Normal',width=size,height=size)
    img.colorspace_settings.name='Non-Color'
    img.pixels.foreach_set(normal.ravel())
    img.filepath_raw=str(RUNTIME/(name+'_Normal.png'));img.file_format='PNG';img.save()

bpy.ops.object.select_all(action='SELECT')
bpy.context.preferences.filepaths.save_version=0
for material_name,texture in [('Worktop','Worktop'),('Ivory','Paint'),('Tile','Floor')]:
    material=materials[material_name]
    nodes=material.node_tree.nodes;links=material.node_tree.links
    bsdf=nodes.get('Principled BSDF')
    colour=nodes.new('ShaderNodeTexImage');colour.image=bpy.data.images.get(texture+'_Albedo')
    tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1
    tint.inputs[1].default_value=material.diffuse_color
    links.new(colour.outputs['Color'],tint.inputs[2]);links.new(tint.outputs['Color'],bsdf.inputs['Base Color'])
    normal_image=nodes.new('ShaderNodeTexImage');normal_image.image=bpy.data.images.get(texture+'_Normal')
    normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.2
    links.new(normal_image.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
for img in bpy.data.images:
    if img.filepath_raw: img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ModernLab.blend'))
bpy.ops.export_scene.fbx(filepath=str(RUNTIME/'ModernLabKit.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
report={'version':bpy.app.version_string,'scene':scene.name,'units':'metres','unityUnitMetres':1,'author':'Chemistry Lab project original procedural artwork','license':'Project-owned original work; no third-party geometry or textures','modules':list(modules),'meshAudit':audits,'rendererCount':len(audits),'blenderFile':'SourceAssets/Original/ModernLab/ModernLab.blend','runtimeFbx':'Assets/ChemistryLab/Art/ModernLab/ModernLabKit.fbx'}
(SOURCE/'provenance.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MODERN_LAB_BLENDER_PASS '+json.dumps({'scene':scene.name,'modules':list(modules),'meshes':len(audits),'vertices':sum(a['vertices'] for a in audits)}))
