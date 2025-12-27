using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using System.Linq;

public class TimelineDuplicator
{
    [MenuItem("CONTEXT/PlayableDirector/Duplicate Current Playable Asset while Keeping Bindings")]
    static void DuplicateAndBind(MenuCommand command)
    {
        PlayableDirector director = (PlayableDirector)command.context;
        TimelineAsset originAsset = director.playableAsset as TimelineAsset;

        if (originAsset == null)
        {
            Debug.LogError("No Timeline Asset assigned to this Director!");
            return;
        }

        var bindingMap = new Dictionary<int, Object>();
        var originTracks = originAsset.GetOutputTracks().ToList();

        for (int i = 0; i < originTracks.Count; i++)
        {
            Object sceneObj = director.GetGenericBinding(originTracks[i]);
            if (sceneObj != null)
            {
                bindingMap.Add(i, sceneObj);
            }
        }

        string originPath = AssetDatabase.GetAssetPath(originAsset);
        string newPath = AssetDatabase.GenerateUniqueAssetPath(originPath);

        if (AssetDatabase.CopyAsset(originPath, newPath))
        {
            TimelineAsset newAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(newPath);

            Undo.RecordObject(director, "Duplicate Timeline and Rebind");

            director.playableAsset = newAsset;

            var newTracks = newAsset.GetOutputTracks().ToList();

            foreach (var kvp in bindingMap)
            {
                int trackIndex = kvp.Key;
                Object sceneObj = kvp.Value;

                if (trackIndex < newTracks.Count)
                {
                    director.SetGenericBinding(newTracks[trackIndex], sceneObj);
                }
            }

            Debug.Log($"Success! Duplicated to {newPath} and restored {bindingMap.Count} bindings.");
            EditorGUIUtility.PingObject(newAsset);
        }
        else
        {
            Debug.LogError("Failed to copy asset.");
        }
    }
}
