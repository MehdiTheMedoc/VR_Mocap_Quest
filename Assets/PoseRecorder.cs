using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseRecorder : MonoBehaviour
{
    public GameObject root;
    public float frameDuration = 0.03333f;
    List<BoneData> bones = new List<BoneData>();

    float nextFrameTime=0;
    bool rec = false;
    public string outputScript;

    // Start is called before the first frame update
    void Start()
    {
        FillData_recur(bones, root.transform);

        Save();
    }

    void FillData_recur(List<BoneData> b, Transform parent)
    {
        foreach (Transform t in parent)
        {
            b.Add(new BoneData(t));
            FillData_recur(b, t);
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

    public void Record()
    {
        nextFrameTime = Time.time + frameDuration;
        rec = true;
    }

    public void Save()
    {
        outputScript = @"import bpy, bmesh

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
";

foreach (BoneData bd in bones)
{
    outputScript += @"b = edit_bones.new('" + bd.transform.name + @"')
b.head = "+ bd.startPosition.ToString("F5") + @"
b.tail = "+ (bd.startPosition + Vector3.up*0.01f).ToString("F5")+@"
";
}

    }


    public class BoneData{
        public Transform transform;
        public List<Vector3> positions;
        public List<Quaternion> rotations;

        public Vector3 startPosition;

        public BoneData(Transform t)
        {
            transform = t;
            positions = new List<Vector3>();
            rotations = new List<Quaternion>();
            startPosition = t.position;

        }

        public void KeyFrame()
        {
            positions.Add(transform.position);
            rotations.Add(transform.rotation);
        }
    }
}
