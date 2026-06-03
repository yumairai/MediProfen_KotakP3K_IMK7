#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class OculusHandAutoBinder
{
    [MenuItem("Tools/Auto-Bind Oculus Hands")]
    public static void BindOculusHands()
    {
        var drivers = Object.FindObjectsOfType<XRHandSkeletonDriver>();
        int count = 0;
        foreach (var driver in drivers)
        {
            if (BindDriver(driver)) count++;
        }
        Debug.Log($"[Hand Tracking] Berhasil memetakan {count} tangan Oculus ke XR Hand Skeleton Driver.");
    }

    private static bool BindDriver(XRHandSkeletonDriver driver)
    {
        SerializedObject so = new SerializedObject(driver);
        SerializedProperty listProp = so.FindProperty("m_JointTransformReferences");
        if (listProp == null)
        {
            listProp = so.FindProperty("jointTransformReferences");
        }
        
        if (listProp == null) return false;

        Transform root = null;
        foreach(Transform t in driver.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("b_l_hand") || t.name.Contains("b_r_hand"))
            {
                root = t;
                break;
            }
        }

        if (root == null) return false;

        string p = root.name.Contains("_l_") ? "b_l_" : "b_r_";
        
        // XRHandJointID mappings
        Dictionary<int, string> mapping = new Dictionary<int, string>
        {
            { (int)XRHandJointID.Wrist, p + "hand" },
            
            { (int)XRHandJointID.ThumbMetacarpal, p + "thumb1" },
            { (int)XRHandJointID.ThumbProximal, p + "thumb2" },
            { (int)XRHandJointID.ThumbDistal, p + "thumb3" },
            { (int)XRHandJointID.ThumbTip, p + "thumb_ignore" },
            
            { (int)XRHandJointID.IndexProximal, p + "index1" },
            { (int)XRHandJointID.IndexIntermediate, p + "index2" },
            { (int)XRHandJointID.IndexDistal, p + "index3" },
            { (int)XRHandJointID.IndexTip, p + "index_ignore" },
            
            { (int)XRHandJointID.MiddleProximal, p + "middle1" },
            { (int)XRHandJointID.MiddleIntermediate, p + "middle2" },
            { (int)XRHandJointID.MiddleDistal, p + "middle3" },
            { (int)XRHandJointID.MiddleTip, p + "middle_ignore" },
            
            { (int)XRHandJointID.RingProximal, p + "ring1" },
            { (int)XRHandJointID.RingIntermediate, p + "ring2" },
            { (int)XRHandJointID.RingDistal, p + "ring3" },
            { (int)XRHandJointID.RingTip, p + "ring_ignore" },
            
            { (int)XRHandJointID.LittleMetacarpal, p + "pinky0" },
            { (int)XRHandJointID.LittleProximal, p + "pinky1" },
            { (int)XRHandJointID.LittleIntermediate, p + "pinky2" },
            { (int)XRHandJointID.LittleDistal, p + "pinky3" },
            { (int)XRHandJointID.LittleTip, p + "pinky_ignore" }
        };

        listProp.ClearArray();

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        foreach (var kvp in mapping)
        {
            Transform matched = null;
            foreach (var t in allTransforms)
            {
                if (t.name == kvp.Value || t.name == "hands:" + kvp.Value)
                {
                    matched = t;
                    break;
                }
            }

            if (matched != null)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                SerializedProperty element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                
                var idProp = element.FindPropertyRelative("m_XRHandJointID");
                if (idProp != null) idProp.intValue = kvp.Key;
                
                var transProp = element.FindPropertyRelative("m_JointTransform");
                if (transProp != null) transProp.objectReferenceValue = matched;
            }
        }

        so.ApplyModifiedProperties();
        return true;
    }
}
#endif
