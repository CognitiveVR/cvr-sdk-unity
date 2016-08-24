import bpy
import mathutils
import math
import bmesh
import sys

#cogntiveVR

#variables
scene = bpy.context.scene
ops = bpy.ops
args = sys.argv
exportPath = args[3]
minFaces = int(args[4])
maxFaces = int(args[5])
if (len(args))<7:
 args.append('')
fileName = args[6]

#print(exportPath)
#print(minFaces)
#print(maxFaces)
#print(fileName)



#select all
for ob in scene.objects:
 ob.select = True
ops.object.delete()

#import
ops.import_scene.obj(filepath=exportPath+fileName+".obj", use_edges=True, use_smooth_groups=True, use_split_objects=True, use_split_groups=True, use_groups_as_vgroups=False, use_image_search=True, split_mode='ON', global_clamp_size=0, axis_forward='-Z', axis_up='Y')
#print("=============================================import complete")

#decimate
for obj in scene.objects:
 if obj.type == 'MESH':
  scene.objects.active = obj
  mod = bpy.context.object.modifiers.new('Decimate','DECIMATE')
  
  faceCount = len(bpy.context.object.data.polygons)
  ratio = 1.0
  
  ratio = (faceCount-minFaces)/maxFaces
  
  ratio = 1-ratio
  
  if ratio >= 1.0:
   ratio = 1.0
  if ratio <= 0.1:
   ratio = 0.1
  
  mod.ratio = ratio
  ops.object.modifier_apply(apply_as='DATA')
#print("=============================================decimate complete")
  
#export
#bpy.ops.object.join()
ops.export_scene.obj(filepath=exportPath+"/"+fileName+"_decimated.obj", use_edges=False, path_mode='RELATIVE')
#print("=============================================export complete")
exit()