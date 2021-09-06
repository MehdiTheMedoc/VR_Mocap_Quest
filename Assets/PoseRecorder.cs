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
        outputScriptList.Add(@"import bpy, bmesh

# create the armature
armdata = bpy.data.armatures.new(""skeleton"")
skelly = bpy.data.objects.new(""skeleton"", armdata)

# add it to the scene
skelly.location = (0,0,0)
bpy.context.collection.objects.link(skelly)

# select it

bpy.context.view_layer.objects.active = skelly

# edit mode
bpy.ops.object.mode_set(mode='EDIT', toggle=False)


# let's go and add some bones
edit_bones = skelly.data.edit_bones
");

foreach (BoneData bd in bones)
{
    outputScriptList.Add( @"b = edit_bones.new('" + (bd.id) + @"')
b.head = "+ bd.startPosition.ToString("F5") + @"
b.tail = "+ (bd.startPosition + Vector3.up*0.01f).ToString("F5")+@"
");
}

/*foreach (BoneData bd in bones)
{
    if(bd.GetParent() != null){
    //outputScriptList.Add("edit_bones['"+(bd.id)+"'].parent = edit_bones['"+(bd.GetParent().id)+@"']
//");
if(Vector3.Distance(bd.startPosition, bd.GetParent().startPosition) > 0.00001f){
 outputScriptList.Add(@"edit_bones['"+(bd.GetParent().id)+"'].tail = "+ bd.startPosition.ToString("F5")+@"
");
}
    }
}*/


     outputScriptList.Add(@"action = bpy.data.actions.new(""Quest Mocap"")
# KEYFRAME ON BONE
bpy.ops.object.mode_set(mode='POSE')
");

    foreach (BoneData bd in bones)
{
     outputScriptList.Add(@"thebone=skelly.pose.bones["+(bd.id) + @"]
");
    for(int i=0; i<bd.rotations.Count; i++)
    {
         outputScriptList.Add(@"thebone.rotation_quaternion="+bd.rotations[i].ToString("F5")+@"
thebone.keyframe_insert(data_path='rotation_quaternion',frame="+ i +@")
");
         outputScriptList.Add(@"thebone.location="+bd.positions[i].ToString("F5")+@"
thebone.keyframe_insert(data_path='location',frame="+ i +@")
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
}
