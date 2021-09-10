using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PoseRecorder : MonoBehaviour
{
    public GameObject root;
    public float frameDuration = 0.03333f;
    List<BoneData> bones = new List<BoneData>();
    float nextFrameTime=0;
    bool rec = false;
    public UnityEvent onRecord, onStop;
    public List<string> whiteList;

    // Start is called before the first frame update
    void Awake()
    {
        FillData_recur(bones, root.transform);
    }

    void FillData_recur(List<BoneData> b, Transform parent)
    {
        foreach(string s in whiteList)
        {
            if(parent.name.Contains(s)){
                b.Add(new BoneData(parent, b.Count));
                foreach (Transform t in parent)
                {
                    FillData_recur(b, t);
                }
                break;
            }
        }

    }

    

    // Update is called once per frame
    void Update()
    {
        if(rec && Time.time > nextFrameTime){
            nextFrameTime = Time.time + frameDuration;

            foreach (BoneData b in bones)
            {
                b.KeyFrame();
            }
        }
    }

    public void ToggleRecord()
    {
        if(rec)
        {
            Stop();
        }else{
            Record();
        }
    }

    public void Record()
    {
        nextFrameTime = Time.time + frameDuration;
        rec = true;
        onRecord.Invoke();
    }

    public void Stop()
    {
        rec = false;
        List<string> outputScriptList = new List<string>();
        outputScriptList.Add(blenderInterfaceFunction);
        outputScriptList.Add(@"

    skelly = None
    if(makeNewArmature):
        # create the armature
        armdata = bpy.data.armatures.new(armatureName)
        skelly = bpy.data.objects.new(armatureName, armdata)
        bpy.context.collection.objects.link(skelly)
    else:
        # find the armature
        skelly = bpy.data.objects[armatureName]

# add it to the scene
    skelly.location = (0,0,0)

# select it
    bpy.context.view_layer.objects.active = skelly

# edit mode
    bpy.ops.object.mode_set(mode='EDIT', toggle=False)


# let's go and add some bones
    edit_bones = skelly.data.edit_bones
");

foreach (BoneData bd in bones)
{
    outputScriptList.Add( @"    b = edit_bones.new('" + B(bd.id) + @"')
    b.head = "+ bd.startPosition.ToString("F5") + @"
    b.tail = "+ (bd.startPosition + Vector3.up*0.01f).ToString("F5")+@"
");
    outputScriptList.Add( @"    b = edit_bones.new('" + B(bd.id)+"_visual" + @"')
    b.head = "+ bd.startPosition.ToString("F5") + @"
    b.tail = "+ (bd.startPosition + Vector3.up*0.01f).ToString("F5")+@"
");
if(bd.GetParent() != null){
    outputScriptList.Add( @"    edit_bones['"+B(bd.id)+"_visual"+"'].parent = edit_bones['"+B(bd.id)+@"']
");
}
}

foreach (BoneData bdd in bones)
{
    if(bdd.GetParent() != null){
if(Vector3.Distance(bdd.startPosition, bdd.GetParent().startPosition) > 0.00001f){
 outputScriptList.Add(@"
    edit_bones['"+B(bdd.GetParent().id)+"_visual"+"'].tail = "+ bdd.startPosition.ToString("F5")+@"
");
}
    }
}


     outputScriptList.Add(@"    action = bpy.data.actions.new(""Quest Mocap"")
# KEYFRAME ON BONE
    bpy.ops.object.mode_set(mode='POSE')
");
outputScriptList.Add(@"    frameIndex = 0
");

    foreach (BoneData bd in bones)
{
     outputScriptList.Add(@"    thebone=skelly.pose.bones['"+B(bd.id) + @"']
");
    for(int i=0; i<bd.rotations.Count; i++)
    {

        outputScriptList.Add(@"    frameIndex = int("+i+@".0*framerate/30.0)
");
    if(i>0) outputScriptList.Add(@"
    if(frameSkip<=1 or "+i+"%frameSkip == 0):");
    else outputScriptList.Add(@"
    if(True):");

    outputScriptList.Add(@"
        thebone.rotation_quaternion="+bd.rotations[i].ToString("F5")+@"
        thebone.keyframe_insert(data_path='rotation_quaternion',frame=frameIndex)
");
         outputScriptList.Add(@"
        thebone.location="+bd.positions[i].ToString("F5")+@"
        thebone.keyframe_insert(data_path='location',frame=frameIndex)
");
    }
}

    int count = 0;
    string defaultOutputFilePath = Application.persistentDataPath + "/outputScript";
    string outputFilePath = defaultOutputFilePath + count + ".py";
    while(File.Exists(outputFilePath))
    {
        count ++;
        outputFilePath = defaultOutputFilePath + count + ".py";
    }

    using (StreamWriter outputFile = new StreamWriter(outputFilePath, true))
        {
            foreach(string s in outputScriptList)
            {
                outputFile.Write(s);
            }
        }

    foreach(BoneData bd in bones)
    {
        bd.positions.Clear();
        bd.rotations.Clear();
    }

        onStop.Invoke();
    }
    


    public class BoneData{
        public Transform transform;
        public List<Vector3> positions;
        public List<Quaternion> rotations;
        public Vector3 startPosition;
        public Quaternion startRotationInverted;
        public int id;
        static Dictionary<Transform, BoneData> dict = new Dictionary<Transform, BoneData>();

        public BoneData(Transform t, int index)
        {
            transform = t;
            positions = new List<Vector3>();
            rotations = new List<Quaternion>();
            startPosition = FlipLeftRightHanded(t.position);
            startRotationInverted = Quaternion.Inverse(t.rotation);
            id = index;
            dict.Add(transform, this);
        }

        public void KeyFrame()
        {
            positions.Add(FlipLeftRightHanded(transform.position) - startPosition);
            rotations.Add(FlipLeftRightHanded(transform.rotation * startRotationInverted));
            //rotations.Add(FlipLeftRightHanded(Quaternion.identity));
        }

        private Vector3 FlipLeftRightHanded(Vector3 vector)
        {  
            return new Vector3(vector.x, vector.z, vector.y);
        }

        private Quaternion FlipLeftRightHanded(Quaternion quaternion)
        {
            return new Quaternion (quaternion.w, -quaternion.x, -quaternion.z, -quaternion.y);
    }

        public BoneData GetParent()
        {
            if(transform.parent != null && dict.ContainsKey(transform.parent))
            {
                return dict[transform.parent];
            }
            return null;
        }
    }

    public string B(int id){
        return B(id+"");
    }

    public string B(string id){
        return "bone_"+id;
    }


    string blenderInterfaceFunction = @"import bpy
from bpy.props import *
 
frameSkip = 0
makeNewArmature = False
armatureName = ""Armature""
framerate = 30
 
class DialogOperator(bpy.types.Operator):
    bl_idname = ""object.dialog_operator""
    bl_label = ""VR Quest Mocap""
 
    TempframeSkip = IntProperty(name=""Frame Skip"")
    Tempframerate = IntProperty(name=""Blender Animation Framerate"")
    tempCreateNewArmature = BoolProperty(name=""Create New Armature"")
    tempArmatureName = StringProperty(name=""Armature Name"")
 
    def execute(self, context):
        global frameSkip, makeNewArmature, armatureName, framerate
        message = ""%.3f, %d, '%s"" % (self.TempframeSkip, 
            self.tempCreateNewArmature, self.tempArmatureName)

        frameSkip = self.TempframeSkip
        framerate = self.Tempframerate
        makeNewArmature = self.tempCreateNewArmature
        armatureName = self.tempArmatureName
            
        GenerateAnimation()
        return {'FINISHED'}
 
    def invoke(self, context, event):
        global frameSkip, makeNewArmature, armatureName
        self.TempframeSkip = frameSkip
        self.Tempframerate = framerate
        self.tempCreateNewArmature = makeNewArmature
        self.tempArmatureName = armatureName
        
        return context.window_manager.invoke_props_dialog(self)
 
 
bpy.utils.register_class(DialogOperator)
 
# Invoke the dialog when loading
bpy.ops.object.dialog_operator('INVOKE_DEFAULT')


def GenerateAnimation():
    print(""generating animation"")
    global frameSkip, makeNewArmature, armatureName, framerate
    frameSkip+=1
";
}
