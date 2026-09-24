# Builds game/Assets/ThirdParty/TestBAChar/Aris_rigged.fbx from the ripped Aris_Original_Body.glb
# and the game's own bone tree (Aris_Original_Mesh.glb). Run headless from the repo root:
#   "C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe" -b --python tools/blender/build_aris_rig.py
# Weights are nearest-bone (Blender's bone-heat solver fails on this mesh); good enough for a top-down camera.

import bpy, math, mathutils
D = r"C:\my-contents\git-repos\workshop-unity-endlessmob\game\Assets\ThirdParty\TestBAChar\Assets\_MX\Characters\Aris_Original\Model\\"
OUT = r"C:\my-contents\git-repos\workshop-unity-endlessmob\game\Assets\ThirdParty\TestBAChar\Aris_rigged.fbx"

def fix(o):  # same stand-up + life-size fix as before
    o.rotation_mode = 'XYZ'; o.rotation_euler = (math.radians(-90), 0, 0); o.scale = (2, 2, 2)

# ---- 1. bone tree from the game's rig -----------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=D + "Aris_Original_Mesh.glb")
for r in [o for o in bpy.data.objects if o.parent is None]: fix(r)
bpy.context.view_layer.update()

# Unity-friendly names for the main chain; everything else keeps its game name.
rename = {"Bip001 Pelvis":"Hips","Bip001 Spine":"Spine","Bip001 Spine1":"Chest","Bip001 Neck":"Neck","Bip001 Head":"Head",
          "Bip001 L Clavicle":"LeftShoulder","Bip001 L UpperArm":"LeftUpperArm","Bip001 L Forearm":"LeftLowerArm","Bip001 L Hand":"LeftHand",
          "Bip001 R Clavicle":"RightShoulder","Bip001 R UpperArm":"RightUpperArm","Bip001 R Forearm":"RightLowerArm","Bip001 R Hand":"RightHand",
          "Bip001 L Thigh":"LeftUpperLeg","Bip001 L Calf":"LeftLowerLeg","Bip001 L Foot":"LeftFoot","Bip001 L Toe0":"LeftToes",
          "Bip001 R Thigh":"RightUpperLeg","Bip001 R Calf":"RightLowerLeg","Bip001 R Foot":"RightFoot","Bip001 R Toe0":"RightToes"}
def wanted(n):
    if n in rename: return True
    l = n.lower()
    return ("hair" in l or "skirt" in l or "ribbon" in l) and "xtra" not in l
nodes = {o.name: o for o in bpy.data.objects}
chosen = [n for n in nodes if wanted(n)]
skipped = sorted(set(n for n in nodes if not wanted(n)))
print("CHOSEN", len(chosen), "SKIPPED", len(skipped), "e.g.", skipped[:12])
info = {}
YUP_TO_ZUP = mathutils.Matrix.Rotation(math.radians(90), 4, 'X')  # the bone tree came out Y-up; the mesh stands Z-up
for n in chosen:
    o = nodes[n]
    p = o.parent
    while p is not None and p.name not in chosen: p = p.parent
    info[n] = (YUP_TO_ZUP @ o.matrix_world.translation, p.name if p else None, [c.name for c in o.children if c.name in chosen])

# ---- 2. the body, one mesh with four materials --------------------------------
for o in list(bpy.data.objects): bpy.data.objects.remove(o)
bpy.ops.import_scene.gltf(filepath=D + "Aris_Original_Body.glb")
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
for e in [o for o in bpy.data.objects if o.type == 'EMPTY']: bpy.data.objects.remove(e)
part_tex = {"SubMesh_0":"Body","SubMesh_1":"Face","SubMesh_2":"EyeMouth","SubMesh_3":"EyeMouth","SubMesh_4":"Hair"}
mats = {}
for t in set(part_tex.values()):
    m = bpy.data.materials.new("Aris_" + t); m.use_nodes = True
    img = m.node_tree.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(D + r"Texture\Aris_Original_" + t + ".png")
    m.node_tree.links.new(img.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    mats[t] = m
for o in meshes:
    fix(o); o.data.materials.clear(); o.data.materials.append(mats[part_tex[o.name]])
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.ops.object.join()
body = bpy.context.view_layer.objects.active; body.name = "Aris"; body.data.name = "Aris"
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.0005)  # bone heat fails on duplicate vertices
bpy.ops.object.mode_set(mode='OBJECT')
print("CLEANED VERTS", len(body.data.vertices))

# ---- 3. armature ---------------------------------------------------------------
arm_data = bpy.data.armatures.new("ArisRig"); arm = bpy.data.objects.new("ArisRig", arm_data)
bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = {}
for n in chosen:
    pos, parent, kids = info[n]
    b = arm_data.edit_bones.new(rename.get(n, n)); b.head = pos
    if kids: b.tail = sum((info[k][0] for k in kids), mathutils.Vector()) / len(kids)
    elif parent: b.tail = pos + (pos - info[parent][0]).normalized() * 0.06
    else: b.tail = pos + mathutils.Vector((0, 0, 0.1))
    if (b.tail - b.head).length < 0.01: b.tail = b.head + mathutils.Vector((0, 0, 0.03))
    eb[n] = b
for n in chosen:
    parent = info[n][1]
    if parent: eb[n].parent = eb[parent]
bpy.ops.object.mode_set(mode='OBJECT')
print("BONES", len(arm_data.bones))
for chk in ("Hips","Head","LeftHand","RightToes"):
    b = arm_data.bones[chk]; print("BONE", chk, tuple(round(x,2) for x in b.head_local), "->", tuple(round(x,2) for x in b.tail_local))

# ---- 4. automatic weights ------------------------------------------------------
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type='ARMATURE_NAME')  # empty groups only; heat weights were garbage on this mesh
unweighted = sum(1 for v in body.data.vertices if not any(g.weight > 0.001 for g in v.groups))
print("HEAT LEFT UNWEIGHTED", unweighted)
def seg_dist(p, a, b):
    ab = b - a; t = 0.0 if ab.length_squared == 0 else max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length
segs = [(b.name, b.head_local.copy(), b.tail_local.copy()) for b in arm_data.bones]
fixed = 0
for v in body.data.vertices:
    best = sorted(segs, key=lambda sgm: seg_dist(v.co, sgm[1], sgm[2]))[:2]
    d0 = seg_dist(v.co, best[0][1], best[0][2]); d1 = seg_dist(v.co, best[1][1], best[1][2])
    w0 = 1.0 if (d1 == 0 or d1 > 1.6 * d0) else d1 / (d0 + d1)  # clearly closer bone takes all; near-ties blend
    body.vertex_groups[best[0][0]].add([v.index], w0, 'REPLACE')
    body.vertex_groups[best[1][0]].add([v.index], 1.0 - w0, 'REPLACE')
    fixed += 1
print("FILLED BY NEAREST BONE", fixed)
# Nearest-bone is all-or-nothing, so a strand kinks where one bone's territory meets the next.
# Smooth each vertex's weights with its neighbours' a few times, like blurring the boundaries.
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); bpy.context.view_layer.objects.active = body
bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
bpy.ops.object.vertex_group_smooth(group_select_mode='ALL', factor=0.5, repeat=6, expand=0.0)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
bpy.ops.object.mode_set(mode='OBJECT')
multi = sum(1 for v in body.data.vertices if sum(1 for g in v.groups if g.weight > 0.01) >= 3)
print("SMOOTHED: verts following 3+ bones", multi, "of", len(body.data.vertices))
unweighted = sum(1 for v in body.data.vertices if not any(g.weight > 0.001 for g in v.groups))
print("VERTS", len(body.data.vertices), "UNWEIGHTED", unweighted, "GROUPS", len(body.vertex_groups))
# Which bones carry the most vertices? (sanity: Head and Hips should be big)
from collections import Counter
c = Counter()
for v in body.data.vertices:
    if v.groups:
        g = max(v.groups, key=lambda g: g.weight); c[body.vertex_groups[g.group].name] += 1
print("TOP BONES", c.most_common(10))

# ---- 5. export ------------------------------------------------------------------
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, path_mode='STRIP', add_leaf_bones=False, bake_anim=False, apply_scale_options='FBX_SCALE_ALL')  # so Unity sees scale 1, not 100
print("EXPORTED", OUT)
