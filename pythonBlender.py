import bpy
import bmesh
#from pythonnet import load #needs pythonnet install if uncommented.
import numpy as np
#load("mono")
from itertools import chain, islice, accumulate
import time
import sys
#import win32pipe, win32file, pywintypes #needs pywin32 install if uncommented.
import mmap
import subprocess
import struct
import threading
from io import BufferedReader
import io
import traceback
import csv
import mathutils
import random
#from bitstring import BitArray #needs install if uncommented.

from bpy.types import Scene, PropertyGroup, Object
from bpy.props import BoolProperty, EnumProperty, FloatProperty, IntProperty, CollectionProperty, StringProperty, FloatVectorProperty

import os
path = "D:/SteamLibrary/steamapps/common/Resonite"

#fname = []
#for root,d_names,f_names in os.walk(path+"/Resonite_Data"):
#    for f in f_names:
#        sys.path.append(os.path.join(root, f))


#import clr
#from System.Reflection import *
#from System.Threading.Tasks import *
#from System.Threading import *
#from System.IO import *
#from System import *

blender_status_slot = None
status = ""
FrooxiousBrain = None
BlenderRunner = None
PIPE: mmap.mmap = None

BoneMap: dict[str, str] = {}

def ReadBinary_Array(resp, type, bytesize):
    length = struct.unpack("<i", resp.read(4))[0]
    #print(f"length of array is {length}")
    return struct.unpack("<"+(type*length), resp.read(bytesize*length))



class BytesIOWrapper():
    
    read_amount: int 
    data: io.BytesIO
    
    def __init__(self,data: io.BytesIO):
        self.read_amount = 0
        self.data = data
        
    def read(self,amount: int) -> bytes:
        self.read_amount += amount
        return self.data.read(amount)
        


def InitFrooc():
    global FrooxiousBrain
    global BlenderRunner
    global PIPE
    
    
    #Directory.SetCurrentDirectory("D:/SteamLibrary/steamapps/common/Resonite/")
    print("loading engine pipe")
    created = False
    
    
    #res = win32pipe.SetNamedPipeHandleState(PIPE, win32pipe.PIPE_READMODE_BYTE, None, None)
    #if res == 0:
    #    print(f"SetNamedPipeHandleState return code: {res}")

class MeshUpdater():
    vertices = []
    bone_groups = []
    bone_weights = []
    triangles = []
    has_bones = False
    bones = []
    bone_matrices = []
    vert_count = 0
    tri_count = 0
    
    file = None
    
    def BINARY_MESH(self, resp, obj_name_cross_refs, slotrefid, refid):
        global BoneMap
        #vert_count = 0
        
        
        binary = format(int(struct.unpack('<B', resp.read(1))[0]),'#010b')
        mesh_data = None
        mesh_obj = None
        if str(refid) in bpy.data.meshes:
            mesh_data = bpy.data.meshes[str(refid)]
        else:
            mesh_data = bpy.data.meshes.new(str(refid))
        
        if str(refid) in obj_name_cross_refs:
            mesh_obj = bpy.data.objects[obj_name_cross_refs[str(refid)]]
        else:
            mesh_obj = bpy.data.objects.new("MESH",mesh_data)
            bpy.data.collections["RESONITE"].objects.link(mesh_obj)
            obj_name_cross_refs[str(refid)] = mesh_obj.name
            mesh_obj.refid = str(refid)
            if str(slotrefid) in obj_name_cross_refs:
                obparent = bpy.data.objects[obj_name_cross_refs[str(slotrefid)]]
                mesh_obj.parent = obparent
                mesh_obj.parent.hide_viewport = mesh_obj.parent.hide_viewport
                mesh_obj.parent.hide_render = mesh_obj.parent.hide_render
        
        if(binary[-3] == '1'):
            return #we already got the data, according to mod code where if we write "NewObj" flag after uploading means that the mesh has been uploaded.

        #bone_names = []
        if(binary[-1] == '1'):
            #print("vertices")
            self.vertices = ReadBinary_Array(resp,"f",4)
            self.vert_count = int(len(self.vertices)/3)
            self.triangles = ReadBinary_Array(resp,"i",4)
            self.tri_count = int(len(self.triangles)/3)
            #we have gotten vert and tri count, generate mesh.
            mesh_data.clear_geometry()
            
            me = mesh_data

            # Get a BMesh representation
            bm = bmesh.new()   # create an empty BMesh
            
            bmesh_verts = []
            # Modify the BMesh, can do anything here...
            for newvert in range(0,self.vert_count):
                bmesh_verts.append(bm.verts.new([self.vertices[(newvert*3)+0],self.vertices[(newvert*3)+2],self.vertices[(newvert*3)+1]]))
            for newface in range(0,self.tri_count):
                try:
                    bm.faces.new([bmesh_verts[self.triangles[(newface*3)+0]],bmesh_verts[self.triangles[(newface*3)+1]],bmesh_verts[self.triangles[(newface*3)+2]]])
                except:
                    pass


            # Finish up, write the bmesh back to the mesh
            bm.to_mesh(me)
            bm.free()  # free and prevent further access
            
        if(binary[-2] == '1'):
            #print("bones")
            print("bones")
            #self.file = "C:/Users/Onan/source/repos/Krysalis/Thundagun/bone_CSV/"+str(refid)+".csv"
            
            self.bones = ReadBinary_Array(resp,"Q",8)
            if(len(mesh_obj.vertex_groups) == 0):
                for idx,bone in enumerate(self.bones):
                    mesh_obj.vertex_groups.new(name=obj_name_cross_refs[str(bone)])
            self.bone_groups = ReadBinary_Array(resp,"i",4) 
            self.bone_weights = ReadBinary_Array(resp,"f",4) 
            
            for idx in range(0,self.vert_count):
                try:
                    mesh_obj.vertex_groups[obj_name_cross_refs[str(self.bones[self.bone_groups[(idx*4)]])]].add(index=[idx],weight=self.bone_weights[idx*4],type='REPLACE')
                    mesh_obj.vertex_groups[obj_name_cross_refs[str(self.bones[self.bone_groups[(idx*4)+1]])]].add(index=[idx],weight=self.bone_weights[(idx*4)+1],type='REPLACE')
                    mesh_obj.vertex_groups[obj_name_cross_refs[str(self.bones[self.bone_groups[(idx*4)+2]])]].add(index=[idx],weight=self.bone_weights[(idx*4)+2],type='REPLACE')
                    mesh_obj.vertex_groups[obj_name_cross_refs[str(self.bones[self.bone_groups[(idx*4)+3]])]].add(index=[idx],weight=self.bone_weights[(idx*4)+3],type='REPLACE')
                except:
                    print(f"error doing bone: boneindex: {bone}, vert_index: {idx}")
                    pass
            matrice_elements = ReadBinary_Array(resp,"f",4)
            self.bone_matrices = []
            for i in range(0,int(len(matrice_elements)/16)):
                mat:mathutils.Matrix = mathutils.Matrix()
                mat.resize_4x4()
                mat[0][0] = matrice_elements[(i*16)+0]
                mat[0][1] = matrice_elements[(i*16)+1]
                mat[0][2] = matrice_elements[(i*16)+2]
                mat[0][3] = matrice_elements[(i*16)+3]
                
                mat[1][0] = matrice_elements[(i*16)+4]
                mat[1][1] = matrice_elements[(i*16)+5]
                mat[1][2] = matrice_elements[(i*16)+6]
                mat[1][3] = matrice_elements[(i*16)+7]
                
                mat[2][0] = matrice_elements[(i*16)+8]
                mat[2][1] = matrice_elements[(i*16)+9]
                mat[2][2] = matrice_elements[(i*16)+10]
                mat[2][3] = matrice_elements[(i*16)+11]
                
                mat[3][0] = matrice_elements[(i*16)+12]
                mat[3][1] = matrice_elements[(i*16)+13]
                mat[3][2] = matrice_elements[(i*16)+14]
                mat[3][3] = matrice_elements[(i*16)+15]
                self.bone_matrices.append(mat)
            
            


            mesh_obj.parent = None
            
            
            
            skelly = None
            if "SCENE_BLENDER_ARMATURE" in bpy.data.objects:
                skelly = bpy.data.objects["SCENE_BLENDER_ARMATURE"]
            else:
                armdata = bpy.data.armatures.new("SCENE_BLENDER_ARMATURE")
                skelly = bpy.data.objects.new("SCENE_BLENDER_ARMATURE", armdata)
                bpy.data.collections["RESONITE"].objects.link(skelly)
                skelly.location = [0,0,0]
                skelly.rotation_mode = 'QUATERNION'
                skelly.rotation_quaternion = [1,0,0,0]
                skelly.scale = [1,1,1]
            
            
            bpy.context.view_layer.objects.active = skelly
            skelly.select_set(True)
            bpy.ops.object.mode_set(mode='EDIT')
            print(self.bones)
            #with open(self.file, "w") as csvfile:
            for idx,bone in enumerate(self.bones):
                
                
                #skelly.  
                print(obj_name_cross_refs[str(bone)])
                arm_data: bpy.types.Armature = skelly.data
                newbone: bpy.types.PoseBone = None
                if str(bone) in BoneMap: 
                    newbone = arm_data.edit_bones[BoneMap[str(bone)]]
                    print(obj_name_cross_refs[str(bone)] + " 1")
                else:
                    print(obj_name_cross_refs[str(bone)] + " 2")
                    newbone = arm_data.edit_bones.new(obj_name_cross_refs[str(bone)])
                    BoneMap[str(bone)] = newbone.name
                    #newbone = arm_data.edit_bones.new(obj_name_cross_refs[str(bone)])
                
                newbone.head = [random.random()*100, random.random()*100,random.random()*100]
                newbone.tail = [.1,0,0]
                newbone.tail = newbone.tail+newbone.head
                newbone.matrix = self.bone_matrices[idx]
            bpy.ops.object.mode_set(mode='OBJECT')
            for idx,bone in enumerate(self.bones):
                rotation: bpy.types.CopyRotationConstraint = None
                location: bpy.types.CopyLocationConstraint  = None
                scale: bpy.types.CopyScaleConstraint  = None
                constraints: bpy.types.PoseBoneConstraints = skelly.pose.bones[BoneMap[str(bone)]].constraints
                if 'Copy Location' in constraints:
                    location = constraints['Copy Location']
                else:
                    location = constraints.new('COPY_LOCATION')
                if 'Copy Rotation' in constraints:
                    rotation = constraints['Copy Rotation']
                else:
                    rotation = constraints.new('COPY_ROTATION')
                if 'Copy Scale' in constraints:
                    scale = constraints['Copy Scale']
                else:
                    scale = constraints.new('COPY_SCALE')
                rotation.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                location.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                scale.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                
                #rotation.invert_x = True
            skelly.select_set(False)
            
            mod: bpy.types.ArmatureModifier = mesh_obj.modifiers.get("Armature")
            if mod is None:
                # otherwise add a modifier to selected object
                mod = mesh_obj.modifiers.new("Armature", 'ARMATURE')
                mod.object = skelly
            bpy.ops.object.mode_set(mode='OBJECT')
        
    

lasttime: float = 0
class ModalOperator(bpy.types.Operator):
    """Move an object with the mouse, example"""
    bl_idname = "object.modal_operator"
    bl_label = "Simple Modal Operator"
    
    
    process: subprocess.Popen = None
    
    messagebin = b""
    connected = False
    
    obj_name_cross_refs = {}
    
    meshes: dict[str, MeshUpdater] = {}
    
    _timer = None
    quit = False
    
    def modal(self, context, event):
        global FrooxiousBrain
        global BlenderRunner
        global status
        global lasttime
        global blender_status_slot
        global mesh_queue
        global mesh_finishes
        global PIPE
        
        #if FrooxiousBrain == None:
        #    InitFrooc()
        
        
        
        #if FrooxiousBrain.WorldManager.FocusedWorld != None:
        #    FrooxiousBrain.WorldManager.FocusedWorld.RunSynchronously(Action(SyncStatus))
        
        #for mesh_finish in mesh_finishes:
        #    mesh_finish(True)
        #    FrooxiousBrain.MeshUpdated()
            
        #mesh_finishes = []
        
        #FrooxiousBrain.RunUpdateLoop()
        if not self.connected:
            try:
                PIPE = mmap.mmap(0, 100000000, "FrooxEnginePipe")
                self.connected = True
            except Exception as e:
                if(time.time()-lasttime < 1):
                    return {'PASS_THROUGH'}
                lasttime = time.time()
                print("no pipe, trying again in a sec")
                print(traceback.format_exc())
                return {'PASS_THROUGH'}
                
            print("engine pipe connected.")
            

        try:
            if PIPE[0:8] != bytes([0,0,0,0,0,0,0,0]):
                length = struct.unpack("<Q", PIPE[0:8])[0]
                self.messagebin = PIPE[8:100000000-20]
                PIPE[0:8] = bytes([0,0,0,0,0,0,0,0])
                try:
                    self.Binary_Update(self.messagebin,length)
                except Exception:
                    print("exception reading binary")
                    print(traceback.format_exc())
            else:
                if self.process.poll() != None:
                    print("broken pipe, bye bye")
                    quit = True
                    skelly = None
                    objects = [obj for obj in bpy.data.objects if obj.name in self.obj_name_cross_refs.values()]
                    if "SCENE_BLENDER_ARMATURE" in bpy.data.objects:
                        skelly = bpy.data.objects["SCENE_BLENDER_ARMATURE"]
                    objects.append(skelly)
                    try:
                        with bpy.context.temp_override(selected_objects = objects): 
                            bpy.ops.object.delete()
                        #pass
                    except Exception:
                        print("Deleting objects failed:")
                        print(traceback.format_exc())
                    self.obj_name_cross_refs = {}
                    global BoneMap
                    BoneMap = {}
                    bpy.ops.outliner.orphans_purge()
                    return {'FINISHED'}
                return {'PASS_THROUGH'}
                
            
        except Exception as e:
            print("exception reading pipe!")
            print(traceback.format_exc())
            return {'PASS_THROUGH'}
            
        
        #for mesh in mesh_queue:
        #    MeshUpdateQueued(mesh)
        #mesh_queue = []
        
        #curtime = time.time()
        #status = "Full frametime: " + str(curtime-lasttime) + "\nBlender FPS: " + str(1/max(0.001,curtime-lasttime))
        
        #lasttime = curtime
            #FrooxiousBrain.WorldManager.FocusedWorld.ConnectorManager.DataModelUnlock()
        #bside = bpy.context.active_object
        #if bside == None:
        #    return {'PASS_THROUGH'}
        #fside = bside["_FROOC"]
        #fside.LocalPosition[0] = bside.location[0]
        #fside.LocalPosition[1] = bside.location[1]
        #fside.LocalPosition[2] = bside.location[2]
        #print("Update")
        return {'PASS_THROUGH'}

    def invoke(self, context, event):
        global BoneMap
        self.process = subprocess.Popen(["resonite.exe", "-Screen", "-LoadAssembly", "Libraries/ResoniteModLoader.dll", "-DoNotAutoLoadHome"], shell=True, cwd=path)
        self.obj_name_cross_refs = {}
        BoneMap = {}
        InitFrooc()
        self._timer = context.window_manager.event_timer_add(0.01, window=context.window)
        context.window_manager.modal_handler_add(self)
        
        return {'RUNNING_MODAL'}
    def Binary_Update(self, bin, length: int):
        print(f"binary: {length}")
        #print(BitArray(bytes=bin).b)
        resp = BytesIOWrapper(io.BytesIO(bin))
        

        
        while(resp.read_amount < length):
            #print(resp.read_amount)
            Type = struct.unpack("<B", resp.read(1))[0]
            
            if Type == 1:
                #print("reading slot")
                try:
                    self.BINARY_SLOT(resp)
                except Exception:
                    print("Error decoding slot!")
                    raise Exception
            elif Type == 2:
                #raise Exception("NO MESHES ALLOWED RIGHT NOW - @989onan")
                try:
                    print("reading mesh")
                    slotrefid = struct.unpack("<Q", resp.read(8))[0]
                    refid = struct.unpack("<Q", resp.read(8))[0]
                    if str(refid) in self.meshes:
                        self.meshes[str(refid)].BINARY_MESH(resp,self.obj_name_cross_refs,slotrefid,refid)
                    else:
                        self.meshes[str(refid)] = MeshUpdater()
                        self.meshes[str(refid)].BINARY_MESH(resp,self.obj_name_cross_refs,slotrefid,refid)
                    print("finished mesh")
                except Exception:
                    print("Error decoding mesh:")
                    raise Exception
            else:
                raise Exception(f"Error decoding stream, unrecognized type \"{Type}\"")

            


            
        
        #indices = np.reshape(np.array(submesh.RawIndicies)[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))
        #for i in range(v[1].SubmeshCount):
        #    submesh = v[1].GetSubmesh(i)
        #    indices = np.concatenate((indices, np.reshape(np.array(submesh.RawIndicies)[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))), axis = 0)
        #
        #mesh_finishes.append(v[2])

    def BINARY_SLOT(self, resp):
        refid,create,destroy,active,position,rotation,scale,parentID,name = (None,None,None,None,None,None,None,0,None)
        
        try:
            binary = format(int(struct.unpack('<i', resp.read(4))[0]),'#034b')
            
            
            
            #print("start!")
            #print(binary)
            ob = None
            if(binary[-1] == '1'): #just in case
                #print("refid!")
                refid = struct.unpack("<Q", resp.read(8))[0]
                #print(str(refid))
            
            
        
                #[obj for obj in bpy.data.objects if obj.refid == str(refid)]
                if str(refid) in self.obj_name_cross_refs:
                    ob = bpy.data.objects[self.obj_name_cross_refs[str(refid)]]
                else:
                    ob = bpy.data.objects.new("UNNAMED",None)
                    bpy.data.collections["RESONITE"].objects.link(ob)
                    self.obj_name_cross_refs[str(refid)] = ob.name
                    ob.refid = str(refid)
            
            
            
            if(binary[-2] == '1'):
                #print("destroy")
                #TODO: Record mode destroy insert keyframe
                list = [t for t in ob.children_recursive]
                list.append(ob)
                for deleted in list:
                    del self.obj_name_cross_refs[str(deleted.refid)]
                with bpy.context.temp_override(selected_objects = list): 
                    bpy.ops.object.delete()
            if(binary[-3] == '1'):
                #print("create!") #TODO: Record mode create insert keyframe
                pass
            if(binary[-4] == '1'):
                name:str = resp.read(struct.unpack('<i', resp.read(4))[0]).decode('utf-8')
                #if("UNNAMED" in ob.name):
                #    print(f"\"{ob.name}\" is an object being renamed to: \"{name}\"")
                if name != None:
                    ob.name = name
                    
                    self.obj_name_cross_refs[str(refid)] = ob.name
                    ob.refid = str(refid)
            if(binary[-5] == '1'):
                #print("Active!")
                active = struct.unpack("?", resp.read(1))[0]
                list = [t for t in ob.children_recursive]
                list.append(ob)
                for hidden in list:
                    hidden.hide_viewport = not active
                    hidden.hide_render = not active
            if(binary[-6] == '1'):
                #print("position!")
                position = [struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0]]
                ob.location = [position[0],position[2],position[1]]
            if(binary[-7] == '1'):
                #print("rotation!")
                rotation = [struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0]]
                ob.rotation_mode = 'QUATERNION'
                ob.rotation_quaternion = [-rotation[3],rotation[0],rotation[2],rotation[1]]
            if(binary[-8] == '1'):
                #print("scale!")
                scale = [struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0],struct.unpack("<f", resp.read(4))[0]]
                ob.scale = [scale[0],scale[2],scale[1]]
            if(binary[-9] == '1'):
                #print("ParentID!")
                parentID = struct.unpack("<Q", resp.read(8))[0]
                #print(parentID)
                if str(parentID) in self.obj_name_cross_refs:
                    obparent: bpy.types.Object = bpy.data.objects[self.obj_name_cross_refs[str(parentID)]]
                    ob.parent = obparent
                    ob.hide_viewport = obparent.hide_viewport
                    ob.hide_render = obparent.hide_render
                    
            #print("FINISH!")
            
            
            
        except Exception:
            print("Error when decoding slot across the IPC pipe:")
            print(traceback.format_exc())
            return
            
        
                
        #print(f"refid: {refid}, destroy: {destroy}, create: {create}, active: {active}, position: {position}, rotation: {rotation}, scale: {scale}, parentID: {parentID}")





def menu_func(self, context):
    self.layout.operator(ModalOperator.bl_idname, text=ModalOperator.bl_label)


# Register and add to the "view" menu (required to also use F3 search "Simple Modal Operator" for quick access).
def register():
    bpy.utils.register_class(ModalOperator)
    bpy.types.VIEW3D_MT_object.append(menu_func)
    Object.refid = StringProperty(
        name='FrooxEngineID',
        description="frooxEngineID for tracking froox engine objects via IPC connector",
        default=""
    )


def unregister():
    bpy.utils.unregister_class(ModalOperator)
    bpy.types.VIEW3D_MT_object.remove(menu_func)


if __name__ == "__main__":
    register()
    
    



"""
FrooxBrainBackup = Activator.CreateInstance(thefrooc.GetType("FrooxEngine.StandaloneFrooxEngineRunner",True),[blenderinfo])
Options = Activator.CreateInstance(thefrooc.GetType("FrooxEngine.LaunchOptions",True))
SystemInfo = Activator.CreateInstance(thefrooc.GetType("FrooxEngine.StandaloneSystemInfo",True))
InitProgress = Activator.CreateInstance(thefrooc.GetType("FrooxEngine.ConsoleEngineInitProgress",True))
Options.DataDirectory = "Q:/Fortnite/Headless2/Data/"
Options.CacheDirectory = "Q:/Fortnite/Headless2/Cache/"
Options.VerboseInit = True

print(FrooxBrainBackup.Initialize("Q:/Fortnite/Headless2/Data/","Q:/Fortnite/Headless2/Cache/",None,True).Result)
FrooxBrainBackup.InteractiveLogin()
"""