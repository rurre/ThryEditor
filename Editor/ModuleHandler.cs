﻿// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using System.Threading;
using UnityEditor.PackageManager.Requests;

namespace Thry
{
    public abstract class ModuleSettings
    {
        public const string MODULES_CONFIG = "Thry/modules_config";

        public abstract void Draw();
    }

    public class ModuleHandler
    {
        private static List<PackageInfo> first_party_packages;
        private static List<PackageInfo> third_party_packages;
        private static bool modules_are_being_loaded = false;

        private class PackageCollection
        {
            public List<PackageInfo> first_party = null;
            public List<PackageInfo> third_party = null;
        }

        public static List<PackageInfo> GetFirstPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadPackages();
            return first_party_packages;
        }

        public static void ForceReloadModules()
        {
            LoadPackages();
        }

        public static List<PackageInfo> GetThirdPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadPackages();
            return third_party_packages;
        }

        private static void LoadPackages()
        {
            modules_are_being_loaded = true;
            var installedPackages = Client.List(true);
            WebHelper.DownloadStringASync(URL.MODULE_COLLECTION, (string s) => {
                first_party_packages = new List<PackageInfo>();
                third_party_packages = new List<PackageInfo>();
                PackageCollection module_collection = Parser.Deserialize<PackageCollection>(s);
                while(installedPackages.IsCompleted == false)
                    Thread.Sleep(100);
                module_collection.first_party.ForEach(m => LoadInfoForPackage(m, installedPackages));
                module_collection.third_party.ForEach(m => LoadInfoForPackage(m, installedPackages));
                first_party_packages = module_collection.first_party;
                third_party_packages = module_collection.third_party;
                UnityHelper.RepaintEditorWindow<Settings>();
            });
        }

        static void LoadInfoForPackage(PackageInfo p, ListRequest installedPackages)
        {
            if(p.isUPM)
            {
                var package = installedPackages.Result.FirstOrDefault(pac => pac.name == p.packageId);
                p.UnityPackageInfo = package;
                p.IsInstalled = package != null;
                if (p.IsInstalled) p.HasUpdate = package.versions.all.Length > 0 && package.versions.latest != package.version;
            }else
            {
                string path = AssetDatabase.GUIDToAssetPath(p.guid);
                p.IsInstalled = string.IsNullOrWhiteSpace(path) == false && (File.Exists(path) || Directory.Exists(path));
            }
        }

        static List<(Request request, PackageInfo package, bool isInstall)> s_requests = new List<(Request request, PackageInfo package, bool isInstall)>();
        public static void InstallPackage(PackageInfo package)
        {
            if(package.isUPM && !package.upmInstallFromUnitypackage) InstallPackageInternal(package, package.git + ".git");
            else GetPackageUrlFromReleases(package, (string url) => InstallPackageInternal(package, url));
        }

        static void InstallPackageInternal(PackageInfo package, string url)
        {
            if(package.isUPM && !package.upmInstallFromUnitypackage)
            {
                Debug.Log("[UPM] Downloading & Installing " + url);
                var request = Client.Add(url);
                package.IsInstalled = true;
                package.IsBeingModified = true;
                s_requests.Add((request, package, true));
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
            }else
            {
                Debug.Log("[Unitypackage] Downloading & Installing " + url);
                package.IsBeingModified = true;
                UnityHelper.RepaintEditorWindow<Settings>();
                var filenname = url.Substring(url.LastIndexOf("/") + 1);
                var path = Path.Combine(Application.temporaryCachePath, filenname);
                WebHelper.DownloadFileASync(url, path, (string path2) =>
                {
                    AssetDatabase.ImportPackage(path2, false);
                    package.IsInstalled = true;
                    package.IsBeingModified = false;
                    UnityHelper.RepaintEditorWindow<Settings>();
                });
            }
        }

        static void GetPackageUrlFromReleases(PackageInfo package, Action<string> callback)
        {
            var parts = package.git.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
            var repo = parts[parts.Length - 2] + "/" + parts[parts.Length - 1];
            WebHelper.DownloadStringASync($"https://api.github.com/repos/{repo}/releases/latest", (string s) =>
            {
                try
                {
                    Dictionary<object, object> dict = (Dictionary<object, object>)Parser.ParseJson(s);
                    List<object> assets = (List<object>)dict["assets"];
                    bool useRegex = string.IsNullOrWhiteSpace(package.unitypackageRegex) == false;
                    foreach(var asset in assets)
                    {
                        Dictionary<object, object> assetDict = (Dictionary<object, object>)asset;
                        if(assetDict.ContainsKey("browser_download_url") && assetDict["browser_download_url"].ToString().EndsWith(".unitypackage") &&
                            (useRegex == false || Regex.Match(assetDict["browser_download_url"].ToString(), package.unitypackageRegex).Success))
                        {
                            var url = assetDict["browser_download_url"].ToString();
                            callback(url);
                        }
                    }
                    
                }catch(Exception e)
                {
                    Debug.LogError("Error while downloading latest release of " + package.git + ".");
                    Debug.LogError(e);
                }
            });
        }

        public static void RemovePackage(PackageInfo package)
        {
            if(package.isUPM)
            {
                var request = Client.Remove(package.packageId);
                package.IsInstalled = false;
                package.IsBeingModified = true;
                s_requests.Add((request, package, false));
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
            }else
            {
                string path = AssetDatabase.GUIDToAssetPath(package.guid);
                if(EditorUtility.DisplayDialog("Remove module", "Do you want to delete the folder " + path + "?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(path);
                    package.IsInstalled = false;
                    package.IsBeingModified = false;
                    AssetDatabase.Refresh();
                    UnityHelper.RepaintEditorWindow<Settings>();
                }
            }
        }

        static void CheckRequests()
        {
            for (int i = 0; i < s_requests.Count; i++)
            {
                var request = s_requests[i];
                if (request.request.IsCompleted)
                {
                    if (request.request.Status == StatusCode.Success)
                    {
                        request.package.IsBeingModified = false;
                        s_requests.RemoveAt(i);
                        i--;
                    }
                    else if (request.request.Status >= StatusCode.Failure)
                    {
                        s_requests.RemoveAt(i);
                        i--;
                        // Try manually deleting the package
                        if(!request.isInstall && request.package.UnityPackageInfo != null)
                        {
                            string path = request.package.UnityPackageInfo.assetPath;
                            Debug.LogWarning($"[Package] UPM Removing failed, trying to delete the package manually from {path}.");
                            AssetDatabase.DeleteAsset(path);
                        }else
                        {
                            Debug.LogError(request.request.Error);
                            request.package.IsBeingModified = false;
                            request.package.IsInstalled = !request.isInstall;
                        }
                    }
                    UnityHelper.RepaintEditorWindow<Settings>();
                }
            }
            if (s_requests.Count == 0)
                EditorApplication.update -= CheckRequests;
        }
    }

}