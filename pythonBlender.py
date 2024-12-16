import bpy
import bmesh
#from pythonnet import load #needs pythonnet install if uncommented.
import numpy as np
#load("mono")
from itertools import chain, islice, accumulate
import time
import sys
import win32pipe, win32file, pywintypes #needs pywin32 install if uncommented.
import subprocess
import struct
import threading
from io import BufferedReader
import io
import traceback
import csv
import mathutils
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
PIPE = None

def ReadBinary_Array(resp, type, bytesize):
    length = struct.unpack("i", resp.read(4))[0]
    #print(f"length of array is {length}")
    return struct.unpack(""+(type*length), resp.read(bytesize*length))

def deleteObjectRecursive(obj):
    try:
        if len(obj.children) > 0:
            for ob in obj.children[:]:
                deleteObjectRecursive(ob)
    except:
        return
    if "_FROOC" in obj:
        slotref = BlenderRunner.GetSlotReference(FrooxiousBrain.WorldManager.FocusedWorld.ReferenceController,obj["_FROOC"])
        if slotref != None:
            BlenderRunner.ToSlotCon(slotref).PObj = None
    bpy.data.objects.remove(obj, do_unlink=True)
    

def hideObjectRecursive(obj,hide):
    try:
        if len(obj.children) > 0:
            for ob in obj.children[:]:
                hideObjectRecursive(ob,hide)
    except:
        return
    obj.hide_viewport = hide
    obj.hide_render = hide

def SlotInit(j,slot,par,parentpy):
    return

def SlotCreateEmpty(slot,parentpy):
    global blender_status_slot
    global FrooxiousBrain
    global BlenderRunner
    #print(slot,parentpy)
    nam = slot.Name
    if nam == None:
        nam = "NoName..."
    ob = BlenderRunner.ToSlotCon(slot).PObj
    if ob == None:
        ob = bpy.data.objects.new(nam, None)
    
    locpos = slot.LocalPosition
    locrot = slot.LocalRotation
    locsca = slot.LocalScale
    
    if (parentpy == None) and ((slot.RawParent) != None):
        parentparent = slot.RawParent.RawParent
        parentpy = SlotCreateEmpty((slot.RawParent),None if parentparent == None else BlenderRunner.ToSlotCon(parentparent).PObj)
    if parentpy != None:
        ob.matrix_world = parentpy.matrix_world
    ob.parent = parentpy
        
    ob.location = (locpos[0],locpos[2],locpos[1])
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = (-locrot.w,locrot.x,locrot.z,locrot.y)
    ob.scale = (locsca[0],locsca[2],locsca[1])
    if slot.IsActive == ob.hide_viewport:
        hideObjectRecursive(ob,not slot.IsActive)
    
    bpy.data.collections["RESONITE"].objects.link(ob)
    ob["_FROOC"] = BlenderRunner.RefIDToUlong(slot.ReferenceID)
    BlenderRunner.ToSlotCon(slot).PObj = ob

    return ob
            

def SlotUpdate(j,parentpy):
    global blender_status_slot
    global status
    if j.PObj == None:
        return
    
    
    
    locpos = j.Owner.LocalPosition
    locrot = j.Owner.LocalRotation
    locsca = j.Owner.LocalScale
    try:
        j.PObj.location = (locpos[0],locpos[2],locpos[1])
    except:
        SlotCreateEmpty(j.Owner,parentpy)
        j.PObj.location = (locpos[0],locpos[2],locpos[1])
    j.PObj.rotation_quaternion = (-locrot[3],locrot[0],locrot[2],locrot[1])
    j.PObj.scale = (locsca[0],locsca[2],locsca[1])
    if j.Owner.IsActive == j.PObj.hide_viewport:
        hideObjectRecursive(j.PObj,not j.Owner.IsActive)
    
    parentpy = BlenderRunner.ToSlotCon(j.Owner.RawParent).PObj
    if j.PObj.parent != parentpy:
        if (parentpy == None) and (j.Owner.RawParent != None):
            parentparent = j.Owner.RawParent.RawParent
            parentpy = SlotCreateEmpty((j.Owner.RawParent),None if parentparent == None else BlenderRunner.ToSlotCon(parentparent).PObj)
        if parentpy != None:
            j.PObj.matrix_world = parentpy.matrix_world
        j.PObj.parent = parentpy
        j.PObj.location = (locpos[0],locpos[2],locpos[1])
        j.PObj.rotation_quaternion = (-locrot[3],locrot[0],locrot[2],locrot[1])
        j.PObj.scale = (locsca[0],locsca[2],locsca[1])
    
    
    
    
    

def SlotDestroy(j):
    if j.PObj == None:
        return
    deleteObjectRecursive(j.PObj)

def MeshInit(m):
    meshname = m.mesh.AssetURL
    if meshname == None:
        meshname = "Unnamed mesh"
    else:
        meshname = meshname.ToString()
    mesh_data = bpy.data.meshes.new(meshname)
    mesh_data.use_fake_user = True
    m.PObj = mesh_data
    

def MeshUnload(m):
    bpy.data.meshes.remove(m.PObj, do_unlink=True)
    m.PObj = None


def MeshUpdateQueued(v):
    mesh_data = v[0]
    mesh_data.clear_geometry()
    if v[1].SubmeshCount == 0:
        mesh_finishes.append(v[2])
        return
    submesh = v[1].GetSubmesh(0)
    indices = np.reshape(np.array(submesh.RawIndicies)[:submesh.Count*submesh.IndiciesPerElement],(submesh.Count,submesh.IndiciesPerElement))
    for i in range(v[1].SubmeshCount):
        submesh = v[1].GetSubmesh(i)
        indices = np.concatenate((indices,np.reshape(np.array(submesh.RawIndicies)[:submesh.Count*submesh.IndiciesPerElement],(submesh.Count,submesh.IndiciesPerElement))),axis=0)
    mesh_data.from_pydata(np.reshape(np.array(v[3]),(-1,3)),[],indices)
    mesh_finishes.append(v[2])

mesh_queue = []
mesh_finishes = []
def MeshUpdateData(m,meshx,uploadHint,bounds,onUpdated,rawPositions):
    global mesh_queue
    mesh_data = m.PObj
    mesh_queue.append((mesh_data,meshx,onUpdated,rawPositions))
    #MeshUpdateQueued((mesh_data,meshx,onUpdated,rawPositions))



def MeshRendInit(mr,slotcon,meshcon,parentpy):
    slot = mr.Owner.Slot
    old = slotcon.PObj
    locpos = slot.LocalPosition
    locrot = slot.LocalRotation
    locsca = slot.LocalScale
    if old != None:
        bpy.data.objects.remove(old, do_unlink=True)
    ob = bpy.data.objects.new(slotcon.Owner.Name, meshcon.PObj)
    if (parentpy == None) and ((slot.RawParent) != None):
        parentparent = slot.RawParent.RawParent
        parentpy = SlotCreateEmpty((slot.RawParent),None if parentparent == None else BlenderRunner.ToSlotCon(parentparent).PObj)
    if parentpy != None:
        ob.matrix_world = parentpy.matrix_world
    
    ob.parent = parentpy
    
    ob.location = (locpos[0],locpos[2],locpos[1])
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = (-locrot.w,locrot.x,locrot.z,locrot.y)
    ob.scale = (locsca[0],locsca[2],locsca[1])
    if slot.IsActive == ob.hide_viewport:
        hideObjectRecursive(ob,not slot.IsActive)
    
    bpy.data.collections["RESONITE"].objects.link(ob)
    ob["_FROOC"] = BlenderRunner.RefIDToUlong(slot.ReferenceID)
    
    #bpy.data.collections["RESONITE"].objects.link(ob)
    mr.PObj = ob
    slotcon.PObj = ob
    #slotcon.PObj["_FROOC"] = BlenderRunner.RefIDToUlong(slotcon.Owner.ReferenceID)
    #

def MeshRendUpdate(mr,slotcon,meshcon,parentpy):
    if mr.PObj == None or meshcon.mesh != mr.Owner.Mesh.Asset:
        MeshRendInit(mr,slotcon,meshcon,parentpy)
    #mr.PObj.data = meshcon.PObj
    pass

def MeshRendDestroy(mr):
    deleteObjectRecursive(mr.PObj)
    mr.PObj = None
    pass



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
    bone_position = []
    bone_vector = []
    vert_count = 0
    tri_count = 0
    
    file = None
    
    def BINARY_MESH(self, resp, obj_name_cross_refs, slotrefid, refid):
        
        #vert_count = 0
        
        
        binary = format(int(struct.unpack('i', resp.read(4))[0]),'#034b')
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
        
        #bone_names = []
        if(binary[-1] == '1'):
            #print("vertices")
            self.vertices = ReadBinary_Array(resp,"f",4)
            self.vert_count = int(len(self.vertices)/3)
            
        if(binary[-2] == '1'):
            #print("triangles")
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
                bmesh_verts.append(bm.verts.new([self.vertices[newvert*3],self.vertices[(newvert*3)+2],self.vertices[(newvert*3)+1]]))
            for newface in range(0,self.tri_count):
                try:
                    bm.faces.new([bmesh_verts[self.triangles[newface*3]],bmesh_verts[self.triangles[(newface*3)+2]],bmesh_verts[self.triangles[(newface*3)+1]]])
                except:
                    pass


            # Finish up, write the bmesh back to the mesh
            bm.to_mesh(me)
            bm.free()  # free and prevent further access
            
        if(binary[-3] == '1'):
            #print("bones")
            print("bones")
            #self.file = "C:/Users/Onan/source/repos/Krysalis/Thundagun/bone_CSV/"+str(refid)+".csv"
            
            self.bones = ReadBinary_Array(resp,"q",8)
            if(len(mesh_obj.vertex_groups) == 0):
                for idx,bone in enumerate(self.bones):
                    mesh_obj.vertex_groups.new(name=obj_name_cross_refs[str(bone)])
        
        if(binary[-4] == '1'):
            print("bone_groups")
            self.bone_groups = ReadBinary_Array(resp,"i",4) 
        if(binary[-5] == '1'):
            print("bone_weights")
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
        if(binary[-6] == '1'):
            #print("bone_position")
            self.bone_position = np.reshape(np.array(ReadBinary_Array(resp,"f",4)),(-1,3)) 
        if(binary[-7] == '1'):
            print("bone_creation")
            self.bone_vector = np.reshape(np.array(ReadBinary_Array(resp,"f",4)),(-1,3)) 
            
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
                print("bone")
                newbone = None
                if obj_name_cross_refs[str(bone)] in skelly.data.edit_bones: 
                    newbone = skelly.data.edit_bones[obj_name_cross_refs[str(bone)]]
                else:
                    newbone = skelly.data.edit_bones.new(obj_name_cross_refs[str(bone)])
                
                newbone.head = self.bone_position[idx]
                newbone.tail = self.bone_vector[idx]+self.bone_position[idx]
                
                #spamwriter = csv.writer(csvfile, delimiter=' ',
                #            quotechar='|', quoting=csv.QUOTE_MINIMAL)
                #spamwriter.writerow([obj_name_cross_refs[str(bone)]]+["head"]+[newbone.head[0],newbone.head[1],newbone.head[2]])
                #spamwriter.writerow([obj_name_cross_refs[str(bone)]]+["tail"]+[newbone.tail[0],newbone.tail[1],newbone.tail[2]])
                newbone.head = [newbone.head[0],newbone.head[2],newbone.head[1]]
                newbone.tail = [newbone.tail[0],newbone.tail[2],newbone.tail[1]]
                #newbone.tail = [newbone.head[0],newbone.head[2],newbone.head[1]+1]
            bpy.ops.object.mode_set(mode='OBJECT')
            for idx,bone in enumerate(self.bones):
                rotation = None
                location = None
                scale = None
                if 'Copy Location' in skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints:
                    location = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints['Copy Location']
                else:
                    location = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints.new('COPY_LOCATION')
                if 'Copy Rotation' in skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints:
                    rotation = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints['Copy Rotation']
                else:
                    rotation = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints.new('COPY_ROTATION')
                if 'Copy Scale' in skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints:
                    scale = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints['Copy Scale']
                else:
                    scale = skelly.pose.bones[obj_name_cross_refs[str(bone)]].constraints.new('COPY_SCALE')
                rotation.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                location.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                scale.target = bpy.data.objects[obj_name_cross_refs[str(bone)]]
                
                #rotation.invert_x = True
            skelly.select_set(False)
            
            mod = mesh_obj.modifiers.get("Armature")
            if mod is None:
                # otherwise add a modifier to selected object
                mod = mesh_obj.modifiers.new("Armature", 'ARMATURE')
                mod.object = skelly
            bpy.ops.object.mode_set(mode='OBJECT')
        
    

lasttime = 0
class ModalOperator(bpy.types.Operator):
    """Move an object with the mouse, example"""
    bl_idname = "object.modal_operator"
    bl_label = "Simple Modal Operator"
    
    
    messagebin = b""
    connected = False
    
    obj_name_cross_refs = {}
    
    meshes = {}
    
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
                PIPE = win32file.CreateFile(
                    r'\\.\pipe\FrooxEnginePipe',
                    win32file.GENERIC_READ,
                    0,
                    None,
                    win32file.OPEN_EXISTING,
                    0,
                    None
                )
                self.connected = True
            except pywintypes.error as e:
                if e.args[0] == 2:
                    #print("no pipe, trying again in a sec")
                    return {'PASS_THROUGH'}
            print("engine pipe connected.")
            

        try:
            if win32pipe.PeekNamedPipe(PIPE,1)[1] > 0:
                resp = win32file.ReadFile(PIPE, 20000000, None)
                self.messagebin += resp[1]
                indexofend = self.messagebin.find(b'FINIS_FR')
                
                while indexofend != -1:
                    try:
                        self.Binary_Update(self.messagebin[:indexofend])
                    except Exception:
                        print("exception reading binary")
                        print(traceback.format_exc())
                    
                    indexofend += len('FINIS_FR')
                    self.messagebin = self.messagebin[indexofend:]
                    indexofend = self.messagebin.find(b'FINIS_FR')
            else:
                return {'PASS_THROUGH'}
                
            
        except pywintypes.error as e:
            if e.args[0] == 2:
                print("no pipe, trying again in a sec")
                time.sleep(1)
            elif e.args[0] == 109:
                print("broken pipe, bye bye")
                quit = True
                
                try:
                    with bpy.context.temp_override(selected_objects = [obj for obj in bpy.data.objects if obj.name in self.obj_name_cross_refs.values()]): 
                        bpy.ops.object.delete()
                    #pass
                except Exception:
                    print("Deleting objects failed:")
                    print(traceback.format_exc())
                self.obj_name_cross_refs = {}
                bpy.ops.outliner.orphans_purge()
                win32file.CloseHandle(PIPE)
                return {'FINISHED'}
            else:
                print(f"error: {e}")
                print(PIPE)
                
        except Exception:
            print("got error!")
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
        subprocess.Popen(["resonite.exe", "-Screen", "-LoadAssembly", "Libraries/ResoniteModLoader.dll", "-DoNotAutoLoadHome"], shell=True, cwd=path)
        self.obj_name_cross_refs = {}
        InitFrooc()
        self._timer = context.window_manager.event_timer_add(0.01, window=context.window)
        context.window_manager.modal_handler_add(self)
        
        return {'RUNNING_MODAL'}
    def Binary_Update(self, bin):
        #print(f"binary: {bin}")
        #print(BitArray(bytes=bin).b)
        resp = io.BytesIO(bin)
        

        TYPE = struct.unpack("b", resp.read(1))[0]
        #print(TYPE)
        if TYPE == 1:
            #print("reading slot")
            self.BINARY_SLOT(resp)
        if TYPE == 2:
            try:
                print("reading mesh")
                slotrefid = struct.unpack("q", resp.read(8))[0]
                refid = struct.unpack("q", resp.read(8))[0]
                if str(refid) in self.meshes:
                    self.meshes[str(refid)].BINARY_MESH(resp,self.obj_name_cross_refs,slotrefid,refid)
                else:
                    self.meshes[str(refid)] = MeshUpdater()
                    self.meshes[str(refid)].BINARY_MESH(resp,self.obj_name_cross_refs,slotrefid,refid)
                print("finished mesh")
            except Exception:
                print("Error decoding mesh:")
                print(traceback.format_exc())
    

            


            
        
        #indices = np.reshape(np.array(submesh.RawIndicies)[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))
        #for i in range(v[1].SubmeshCount):
        #    submesh = v[1].GetSubmesh(i)
        #    indices = np.concatenate((indices, np.reshape(np.array(submesh.RawIndicies)[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))), axis = 0)
        #
        #mesh_finishes.append(v[2])

    def BINARY_SLOT(self, resp):
        refid,create,destroy,active,position,rotation,scale,parentID,name = (None,None,None,None,None,None,None,0,None)
        
        try:
            binary = format(int(struct.unpack('i', resp.read(4))[0]),'#034b')
            
            
            
            #print("start!")
            #print(binary)
            ob = None
            if(binary[-1] == '1'): #just in case
                #print("refid!")
                refid = struct.unpack("q", resp.read(8))[0]
                #print(str(refid))
            
            
        
                #[obj for obj in bpy.data.objects if obj.refid == str(refid)]
                if str(refid) in self.obj_name_cross_refs:
                    ob = bpy.data.objects[self.obj_name_cross_refs[str(refid)]]
                else:
                    ob = bpy.data.objects.new("UNNAMED",None)
                    bpy.data.collections["RESONITE"].objects.link(ob)
                    self.obj_name_cross_refs[str(refid)] = ob.name
                    ob.refid = str(refid)
            if ob == None:
                return
            
            if ob.parent != None:
                if not ob.hide_viewport:
                    ob.hide_viewport = ob.parent.hide_viewport
                    ob.hide_render = ob.parent.hide_render
            
            if(binary[-2] == '1'):
                #print("destroy")
                destroy = struct.unpack("?", resp.read(1))[0]
                list = [t for t in ob.children_recursive]
                list.append(ob)
                for deleted in list:
                    del self.obj_name_cross_refs[str(deleted.refid)]
                with bpy.context.temp_override(selected_objects = list): 
                    bpy.ops.object.delete()
            if(binary[-3] == '1'):
                #print("create!")
                create = struct.unpack("?", resp.read(1))[0]
            if(binary[-4] == '1'):
                name = resp.read(struct.unpack('i', resp.read(4))[0]*2).decode('utf-16-le')
                if name != None:
                    ob.name = name
                    self.obj_name_cross_refs[str(refid)] = ob.name
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
                position = [struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0]]
                ob.location = [position[0],position[2],position[1]]
            if(binary[-7] == '1'):
                #print("rotation!")
                rotation = [struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0]]
                ob.rotation_mode = 'QUATERNION'
                ob.rotation_quaternion = [-rotation[3],rotation[0],rotation[2],rotation[1]]
            if(binary[-8] == '1'):
                #print("scale!")
                scale = [struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0],struct.unpack("f", resp.read(4))[0]]
                ob.scale = [scale[0],scale[2],scale[1]]
            if(binary[-9] == '1'):
                #print("ParentID!")
                parentID = struct.unpack("q", resp.read(8))[0]
                #print(parentID)
                if str(parentID) in self.obj_name_cross_refs:
                    obparent = bpy.data.objects[self.obj_name_cross_refs[str(parentID)]]
                    ob.parent = obparent
                    
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