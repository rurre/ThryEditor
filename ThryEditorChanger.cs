﻿using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ThryEditorChanger : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Thry/Use Thry Editor for other shaders")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ThryEditorChanger window = (ThryEditorChanger)EditorWindow.GetWindow(typeof(ThryEditorChanger));
        window.Show();
    }

    Vector2 scrollPos;

    bool[] setEditor;
    bool[] wasEditor;

    List<string> paths = null;

    void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        bool init = false;

        if (paths == null)
        {
            paths = new List<string>();
            string[] shaderGuids = AssetDatabase.FindAssets("t:shader");

            for (int sguid = 0; sguid < shaderGuids.Length; sguid++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderGuids[sguid]);
                if (!path.Contains("_differentQueues/")) paths.Add(path);
            }

            if (setEditor == null || setEditor.Length != shaderGuids.Length)
            {
                setEditor = new bool[paths.Count];
                wasEditor = new bool[paths.Count];
            }
            init = true;
        }

        for (int p = 0; p < paths.Count; p++)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(paths[p]);

            if (init)
            {
                EditorUtility.DisplayProgressBar("Load all shaders...", "", (float)p / paths.Count);
                setEditor[p] = (new Material(shader)).HasProperty("shader_is_using_thry_editor");
                wasEditor[p] = setEditor[p];
            }
            setEditor[p] = GUILayout.Toggle(setEditor[p], shader.name);
        }
        if(init)EditorUtility.ClearProgressBar();

        GUILayout.EndScrollView();

        if (GUILayout.Button("Apply"))
        {
            for (int sguid = 0; sguid < paths.Count; sguid++)
            {
                if (wasEditor[sguid] != setEditor[sguid])
                {
                    string path = paths[sguid];
                    if (setEditor[sguid]) addThryEditor(path);
                    else removeThryEditor(path);
                }

                wasEditor[sguid] = setEditor[sguid];
            }
            AssetDatabase.Refresh();
            ThryHelper.RepaintAllMaterialEditors();
        }
    }

    private void addThryEditor(string path)
    {
        replaceEditorInShader(path, "ThryEditor");
        addProperty(path, "[HideInInspector] shader_is_using_thry_editor(\"\", Float)", "0");
    }

    private void removeThryEditor(string path)
    {
        revertEditor(path);
        removeProperty(path, "[HideInInspector] shader_is_using_thry_editor(\"\", Float)", "0");
    }

    private void addProperty(string path, string property, string value)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"Properties.*\n?\s*{";
        RegexOptions options = RegexOptions.Multiline;
        shaderCode = Regex.Replace(shaderCode, pattern, "Properties \r\n  {"+" \r\n      "+ property + "=" + value, options);

        ThryHelper.writeStringToFile(shaderCode, path);
    }

    private void removeProperty(string path, string property, string value)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"\r?\n.*"+Regex.Escape(property)+" ?= ?" + value;
        RegexOptions options = RegexOptions.Multiline;

        shaderCode = Regex.Replace(shaderCode, pattern, "", options);

        ThryHelper.writeStringToFile(shaderCode, path);
    }

    private void revertEditor(string path)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"//originalEditor.*\n";
        Match m = Regex.Match(shaderCode, pattern);
        if (m.Success)
        {
            string orignialEditor = m.Value.Replace("//originalEditor","");
            pattern = @"//originalEditor.*\n.*\n";
            shaderCode = Regex.Replace(shaderCode, pattern, orignialEditor);
            ThryHelper.writeStringToFile(shaderCode, path);
        }
    }

    private void replaceEditorInShader(string path, string newEditor)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"CustomEditor ?.*\n";
        Match m = Regex.Match(shaderCode, pattern);
        if (m.Success)
        {
            string oldEditor = "//originalEditor" + m.Value;
            shaderCode = Regex.Replace(shaderCode, pattern, oldEditor+"CustomEditor \"" + newEditor + "\"\r\n");
        }
        else
        {
            pattern = @"SubShader.*\r?\n?.*{";
            RegexOptions options = RegexOptions.Multiline;
            shaderCode = Regex.Replace(shaderCode, pattern, "CustomEditor \""+ newEditor + "\" \r\n    SubShader \r\n  {", options);
        }

        ThryHelper.writeStringToFile(shaderCode, path);
    }

}
