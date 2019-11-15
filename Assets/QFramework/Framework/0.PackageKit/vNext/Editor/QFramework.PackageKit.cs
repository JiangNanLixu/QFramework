using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using QFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace QFramework
{
    using Dependencies.PackageKit;

    public static class User
    {
        public static Property<string> Username = new Property<string>(LoadString("username"));
        public static Property<string> Password = new Property<string>(LoadString("password"));
        public static Property<string> Token    = new Property<string>(LoadString("token"));

        public static bool Logined
        {
            get
            {
                return Token.Value.IsNotNullAndEmpty() &&
                       Username.Value.IsNotNullAndEmpty() &&
                       Password.Value.IsNotNullAndEmpty();
            }
        }


        public static void Save()
        {
            Username.SaveString("username");
            Password.SaveString("password");
            Token.SaveString("token");
        }

        public static void Clear()
        {
            Username.Value = string.Empty;
            Password.Value = string.Empty;
            Token.Value = string.Empty;
            Save();
        }

        public static void SaveString(this Property<string> selfProperty, string key)
        {
            EditorPrefs.SetString(key, selfProperty.Value);
        }


        public static string LoadString(string key)
        {
            return EditorPrefs.GetString(key, string.Empty);
        }
    }

    public interface IPackageManagerServer
    {
        void DeletePackage(string packageId, Action onResponse);

        void GetAllRemotePackageInfo(Action<List<PackageData>> onResponse);
    }

    public class PackageManagerServer : IPackageManagerServer
    {
        [Serializable]
        public class QFrameworkServerResultFormat<T>
        {
            public int code;

            public string msg;

            public T data;
        }

        public void DeletePackage(string packageId, Action onResponse)
        {
            var form = new WWWForm();

            form.AddField("username", User.Username.Value);
            form.AddField("password", User.Password.Value);
            form.AddField("id", packageId);

            EditorHttp.Post("https://api.liangxiegame.com/qf/v4/package/delete", form, (type, response) =>
            {
                if (type == ResponseType.SUCCEED)
                {
                    var result = JsonUtility.FromJson<QFrameworkServerResultFormat<object>>(response);

                    if (result.code == 1)
                    {
                        Debug.Log("删除成功");

                        onResponse();
                    }
                }
            });
        }

        public void GetAllRemotePackageInfo(Action<List<PackageData>> onResponse)
        {
            if (User.Logined)
            {
                var form = new WWWForm();

                form.AddField("username", User.Username.Value);
                form.AddField("password", User.Password.Value);

                EditorHttp.Post("https://api.liangxiegame.com/qf/v4/package/list", form,
                    (type, s) => OnResponse(type, s, onResponse));
            }
            else
            {
                EditorHttp.Post("https://api.liangxiegame.com/qf/v4/package/list", new WWWForm(),
                    (type, s) => OnResponse(type, s, onResponse));
            }
        }

        void OnResponse(ResponseType responseType, string response,Action<List<PackageData>> onResponse)
        {
            if (responseType == ResponseType.SUCCEED)
            {
                var responseJson = JsonUtility.FromJson<QFrameworkServerResultFormat<List<PackageData>>>(response);

                if (responseJson.code == 1)
                {
                    var packageInfosJson = responseJson.data;

                    var packageDatas = new List<PackageData>();
                    foreach (var packageInfo in packageInfosJson)
                    {
                        var name = packageInfo.Name;
                        
                        

                        var package = packageDatas.Find(packageData => packageData.Name == name);

                        if (package == null)
                        {
                            package = new PackageData()
                            {
                                Name = name,
                            };

                            packageDatas.Add(package);
                        }

                        var id = packageInfo.Id;
                        var version = packageInfo.Version;
                        var url = packageInfo.DownloadUrl;
                        var installPath = packageInfo.InstallPath;
//                        var releaseNote = packageInfo["releaseNote"].Value<string>();
//                        var createAt = packageInfo["createAt"].Value<string>();
//                        var creator = packageInfo["username"].Value<string>();
//                        var releaseItem = new ReleaseItem(version, releaseNote, creator, DateTime.Parse(createAt), id);
//                        var accessRightName = packageInfo["accessRight"].Value<string>();
//                        var typeName = packageInfo["type"].Value<string>();
//
//                        var packageType = PackageType.FrameworkModule;
//
//                        switch (typeName)
//                        {
//                            case "fm":
//                                packageType = PackageType.FrameworkModule;
//                                break;
//                            case "s":
//                                packageType = PackageType.Shader;
//                                break;
//                            case "agt":
//                                packageType = PackageType.AppOrGameDemoOrTemplate;
//                                break;
//                            case "p":
//                                packageType = PackageType.Plugin;
//                                break;
//                            case "master":
//                                packageType = PackageType.Master;
//                                break;
//                        }
//
//                        var accessRight = PackageAccessRight.Public;
//
//                        switch (accessRightName)
//                        {
//                            case "public":
//                                accessRight = PackageAccessRight.Public;
//                                break;
//                            case "private":
//                                accessRight = PackageAccessRight.Private;
//                                break;
//                        }
//
//                        package.PackageVersions.Add(new PackageVersion()
//                        {
//                            Id = id,
//                            Version = version,
//                            DownloadUrl = url,
//                            InstallPath = installPath,
//                            Type = packageType,
//                            AccessRight = accessRight,
//                            Readme = releaseItem,
//                        });
//
//                        package.readme.AddReleaseNote(releaseItem);
                    }
//
//                    packageDatas.ForEach(packageData =>
//                    {
//                        packageData.PackageVersions.Sort((a, b) =>
//                            b.VersionNumber - a.VersionNumber);
//                        packageData.readme.items.Sort((a, b) =>
//                            b.VersionNumber - a.VersionNumber);
//                    });
//
//                    mOnGet.InvokeGracefully(packageDatas);
//
//                    new PackageInfosRequestCache()
//                    {
//                        PackageDatas = packageDatas
//                    }.Save();

                }
                
                onResponse(null);
            }
        }
    }

    public static class UploadPackage
    {
        private static string UPLOAD_URL
        {
            get { return "https://api.liangxiegame.com/qf/v4/package/add"; }
        }

        public static void DoUpload(PackageVersion packageVersion, System.Action succeed)
        {
            EditorUtility.DisplayProgressBar("插件上传", "打包中...", 0.1f);

            var fileName = packageVersion.Name + "_" + packageVersion.Version + ".unitypackage";
            var fullpath = PackageManagerView.ExportPaths(fileName, packageVersion.InstallPath);
            var file = File.ReadAllBytes(fullpath);

            var form = new WWWForm();
            form.AddField("username", User.Username.Value);
            form.AddField("password", User.Password.Value);
            form.AddField("name", packageVersion.Name);
            form.AddField("version", packageVersion.Version);
            form.AddBinaryData("file", file);
            form.AddField("version", packageVersion.Version);
            form.AddField("releaseNote", packageVersion.Readme.content);
            form.AddField("installPath", packageVersion.InstallPath);
            form.AddField("accessRight", packageVersion.AccessRight.ToString().ToLower());
            form.AddField("docUrl", packageVersion.DocUrl);

            if (packageVersion.Type == PackageType.FrameworkModule)
            {
                form.AddField("type", "fm");
            }
            else if (packageVersion.Type == PackageType.Shader)
            {
                form.AddField("type", "s");
            }
            else if (packageVersion.Type == PackageType.AppOrGameDemoOrTemplate)
            {
                form.AddField("type", "agt");
            }
            else if (packageVersion.Type == PackageType.Plugin)
            {
                form.AddField("type", "p");
            }
            else if (packageVersion.Type == PackageType.Master)
            {
                form.AddField("type", "master");
            }

            Debug.Log(fullpath);

            EditorUtility.DisplayProgressBar("插件上传", "上传中...", 0.2f);

            EditorHttp.Post(UPLOAD_URL, form, (type, responseContent) =>
            {
                if (type == ResponseType.SUCCEED)
                {
                    EditorUtility.ClearProgressBar();
                    Debug.Log(responseContent);
                    succeed.InvokeGracefully();
                    File.Delete(fullpath);
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("插件上传", "上传失败!{0}".FillFormat(responseContent), "确定");
                    File.Delete(fullpath);
                }
            });
        }
    }

    public class ReadmeWindow : EditorWindow
    {
        private Readme mReadme;

        private Vector2 mScrollPos = Vector2.zero;

        private PackageVersion mPackageVersion;


        public static void Init(Readme readme, PackageVersion packageVersion)
        {
            var readmeWin = (ReadmeWindow) GetWindow(typeof(ReadmeWindow), true, packageVersion.Name, true);
            readmeWin.mReadme = readme;
            readmeWin.mPackageVersion = packageVersion;
            readmeWin.position = new Rect(Screen.width / 2, Screen.height / 2, 600, 300);
            readmeWin.Show();
        }

        public void OnGUI()
        {
            mScrollPos = GUILayout.BeginScrollView(mScrollPos, true, true, GUILayout.Width(580), GUILayout.Height(300));

            GUILayout.Label("类型:" + mPackageVersion.Type);

            mReadme.items.ForEach(item =>
            {
                new CustomView(() =>
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();

                    GUILayout.Label("version: " + item.version, GUILayout.Width(130));
                    GUILayout.Label("author: " + item.author);
                    GUILayout.Label("date: " + item.date);

                    if (item.author == User.Username.Value)
                    {
                        if (GUILayout.Button("删除"))
                        {
//                            EditorActionKit.ExecuteNode(new DeletePackage(item.PackageId)
//                            {
//                                OnEndedCallback = () => { mReadme.items.Remove(item); }
//                            });
                        }
                    }


                    GUILayout.EndHorizontal();
                    GUILayout.Label(item.content);
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }).DrawGUI();
            });

            GUILayout.EndScrollView();
        }
    }

    public class InstallPackage : NodeAction
    {
        private PackageData mRequestPackageData;

        public InstallPackage(PackageData requestPackageData)
        {
            mRequestPackageData = requestPackageData;
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            var tempFile = "Assets/" + mRequestPackageData.Name + ".unitypackage";

            Debug.Log(mRequestPackageData.DownloadUrl + ">>>>>>:");

            EditorUtility.DisplayProgressBar("插件更新", "插件下载中 ...", 0.1f);

//            var progressListener = new ScheduledNotifier<float>();
//
//            ObservableWWW.GetAndGetBytes(mRequestPackageData.DownloadUrl, null, progressListener)
//                .Subscribe(bytes =>
//                {
//                    File.WriteAllBytes(tempFile, bytes);
//
//                    EditorUtility.ClearProgressBar();
//
//                    AssetDatabase.ImportPackage(tempFile, false);
//
//                    File.Delete(tempFile);
//
//                    mRequestPackageData.SaveVersionFile();
//
//                    AssetDatabase.Refresh();
//
//                    Log.I("PackageManager:插件下载成功");
//
//                    InstalledPackageVersions.Reload();
//                }, e =>
//                {
//                    EditorUtility.ClearProgressBar();
//
//                    EditorUtility.DisplayDialog(mRequestPackageData.Name,
//                        "插件安装失败,请联系 liangxiegame@163.com 或者加入 QQ 群:623597263" + e.ToString() + ";", "OK");
//                });

//            progressListener.Subscribe(OnProgressChanged);
        }

        private void OnProgressChanged(float progress)
        {
            EditorUtility.DisplayProgressBar("插件更新",
                "插件下载中 {0:P2}".FillFormat(progress), progress);
        }
    }

    public class InstalledPackageVersions
    {
        private static List<PackageVersion> mPackageVersions = new List<PackageVersion>();

        public static List<PackageVersion> Get()
        {
            if (mPackageVersions.Count == 0)
            {
                Reload();
            }

            return mPackageVersions;
        }

        public static void Reload()
        {
            mPackageVersions.Clear();

            var versionFiles = Array.FindAll(AssetDatabase.GetAllAssetPaths(),
                name => name.EndsWith("PackageVersion.json"));

            versionFiles.ForEach(fileName =>
            {
                mPackageVersions.Add(SerializeHelper.LoadJson<PackageVersion>(fileName));
            });
        }

        public static PackageVersion FindVersionByName(string name)
        {
            return Get().Find(installedPackageVersion => installedPackageVersion.Name == name);
        }
    }

    [Serializable]
    public class ReleaseItem
    {
        public ReleaseItem()
        {
        }

        public ReleaseItem(string version, string content, string author, DateTime date, string packageId = "")
        {
            this.version = version;
            this.content = content;
            this.author = author;
            this.date = date.ToString("yyyy 年 MM 月 dd 日 HH:mm");
            PackageId = packageId;
        }

        public string version = "";
        public string content = "";
        public string author  = "";
        public string date    = "";
        public string PackageId { get; set; }


        public int VersionNumber
        {
            get
            {
                if (version.IsNullOrEmpty())
                {
                    return 0;
                }

                var numbersStr = version.RemoveString("v").Split('.');

                var retNumber = numbersStr[2].ToInt();
                retNumber += numbersStr[1].ToInt() * 100;
                retNumber += numbersStr[0].ToInt() * 10000;

                return retNumber;
            }
        }
    }

    [Serializable]
    public class Readme
    {
        public List<ReleaseItem> items;

        public ReleaseItem GetItem(string version)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            return items.First(s => s.version == version);
        }

        public void AddReleaseNote(ReleaseItem pluginReadme)
        {
            if (items == null)
            {
                items = new List<ReleaseItem> {pluginReadme};
            }
            else
            {
                bool exist = false;
                foreach (var item in items)
                {
                    if (item.version == pluginReadme.version)
                    {
                        item.content = pluginReadme.content;
                        item.author = pluginReadme.author;
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    items.Add(pluginReadme);
                }
            }
        }
    }

    [Serializable]
    public class PackageData
    {
        public string Id;
        
        public string Name = "";

        
        public string Version
        {
            get { return PackageVersions.FirstOrDefault() == null ? string.Empty : PackageVersions.First().Version; }
        }

        public string DownloadUrl
        {
            get { return PackageVersions.FirstOrDefault() == null ? string.Empty : PackageVersions.First().DownloadUrl; }
        }

        public string InstallPath
        {
            get { return PackageVersions.FirstOrDefault() == null ? string.Empty : PackageVersions.First().InstallPath; }
        }

        public string DocUrl
        {
            get { return PackageVersions.FirstOrDefault() == null ? string.Empty : PackageVersions.First().DocUrl; }
        }

        public PackageType Type
        {
            get { return PackageVersions.FirstOrDefault() == null ? PackageType.Master : PackageVersions.First().Type; }
        }

        public PackageAccessRight AccessRight
        {
            get { return PackageVersions.FirstOrDefault() == null ? PackageAccessRight.Public : PackageVersions.First().AccessRight; }
        }

        public Readme readme;

        public List<PackageVersion> PackageVersions = new List<PackageVersion>();

        public PackageData()
        {
            readme = new Readme();
        }

        public int VersionNumber
        {
            get
            {
                var numbersStr = Version.RemoveString("v").Split('.');

                var retNumber = numbersStr[2].ToInt();
                retNumber += numbersStr[1].ToInt() * 100;
                retNumber += numbersStr[0].ToInt() * 10000;
                return retNumber;
            }
        }

        public bool Installed
        {
            get { return Directory.Exists(InstallPath); }
        }

        public void SaveVersionFile()
        {
            PackageVersions.First().Save();
        }
    }

    public enum PackageType
    {
        FrameworkModule, //fm
        Shader, //s
        UIKitComponent, //uc
        Plugin, // p
        AppOrGameDemoOrTemplate, //agt
        DocumentsOrTutorial, //doc
        Master, // master
    }

    public enum PackageAccessRight
    {
        Public,
        Private
    }

    [Serializable]
    public class PackageVersion
    {
        public string Id { get; set; }

        public string Name
        {
            get { return InstallPath.IsNotNullAndEmpty() ? InstallPath.GetLastDirName() : ""; }
        }

        public string Version = "v0.0.0";

        public PackageType Type;

        public PackageAccessRight AccessRight;

        public int VersionNumber
        {
            get
            {
                var numbersStr = Version.RemoveString("v").Split('.');

                var retNumber = numbersStr[2].ToInt();
                retNumber += numbersStr[1].ToInt() * 100;
                retNumber += numbersStr[0].ToInt() * 10000;

                return retNumber;
            }
        }

        public string DownloadUrl;

        public string InstallPath = "Assets/QFramework/Framework/";

        public string FileName
        {
            get { return Name + "_" + Version + ".unitypackage"; }
        }

        public string DocUrl;

        public ReleaseItem Readme = new ReleaseItem();

        public void Save()
        {
            this.SaveJson(InstallPath.CreateDirIfNotExists() + "/PackageVersion.json");
        }

        public static PackageVersion Load(string filePath)
        {
            if (filePath.EndsWith("/"))
            {
                filePath += "PackageVersion.json";
            }
            else if (!filePath.EndsWith("PackageVersion.json"))
            {
                filePath += "/PackageVersion.json";
            }

            return SerializeHelper.LoadJson<PackageVersion>(filePath);
        }
    }

    public class PackageView : HorizontalLayout
    {
        class LocaleText
        {
            public static string Doc
            {
                get { return Language.IsChinese ? "文档" : "Doc"; }
            }

            public static string Import
            {
                get { return Language.IsChinese ? "导入" : "Import"; }
            }

            public static string Update
            {
                get { return Language.IsChinese ? "更新" : "Update"; }
            }

            public static string Reimport
            {
                get { return Language.IsChinese ? "再次导入" : "Reimport"; }
            }

            public static string ReleaseNotes
            {
                get { return Language.IsChinese ? "版本说明" : "Release Notes"; }
            }
        }

        private PackageData mPackageData;

        public PackageView(PackageData packageData) : base(null)
        {
            this.mPackageData = packageData;

            Refresh();
        }

        protected override void OnRefresh()
        {
            Clear();

            new SpaceView(2).AddTo(this);

            new LabelView(mPackageData.Name)
                .FontBold()
                .Width(150)
                .AddTo(this);

            new LabelView(mPackageData.Version)
                .TextMiddleCenter()
                .Width(80)
                .AddTo(this);

            var installedPackage = InstalledPackageVersions.FindVersionByName(mPackageData.Name);

            new LabelView(installedPackage != null ? installedPackage.Version : " ")
                .TextMiddleCenter()
                .Width(80)
                .AddTo(this);

            new LabelView(mPackageData.AccessRight.ToString())
                .TextMiddleCenter()
                .Width(80)
                .AddTo(this);

            if (mPackageData.DocUrl.IsNotNullAndEmpty())
            {
                new ButtonView(LocaleText.Doc, () => { }).AddTo(this);
            }
            else
            {
                new SpaceView(40).AddTo(this);
            }


            if (installedPackage == null)
            {
                new ButtonView(LocaleText.Import, () =>
                    {
                        EditorActionKit.ExecuteNode(new InstallPackage(mPackageData));

                        PackageApplication.Container.Resolve<PackageKitWindow>().Close();
                    })
                    .Width(90)
                    .AddTo(this);
            }

            else if (installedPackage != null && mPackageData.VersionNumber > installedPackage.VersionNumber)
            {
                new ButtonView(LocaleText.Update, () =>
                    {
                        var path = Application.dataPath.Replace("Assets", mPackageData.InstallPath);

                        path.DeleteDirIfExists();

                        EditorActionKit.ExecuteNode(new InstallPackage(mPackageData));

                        PackageApplication.Container.Resolve<PackageKitWindow>().Close();
                    })
                    .Width(90)
                    .AddTo(this);
            }
            else if (installedPackage.IsNotNull() &&
                     mPackageData.VersionNumber == installedPackage.VersionNumber)
            {
                new ButtonView(LocaleText.Reimport, () =>
                    {
                        var path = Application.dataPath.Replace("Assets", mPackageData.InstallPath);

                        path.DeleteDirIfExists();

                        EditorActionKit.ExecuteNode(new InstallPackage(mPackageData));
                        PackageApplication.Container.Resolve<PackageKitWindow>().Close();
                    })
                    .Width(90)
                    .AddTo(this);
            }
            else if (installedPackage != null)
            {
                new SpaceView(94).AddTo(this);
            }

            new ButtonView(LocaleText.ReleaseNotes,
                    () => { ReadmeWindow.Init(mPackageData.readme, mPackageData.PackageVersions.First()); }).Width(100)
                .AddTo(this);
        }
    }

    public class HeaderView : HorizontalLayout
    {
        public HeaderView()
        {
            HorizontalStyle = "box";

            new LabelView(LocaleText.PackageName)
                .Width(150)
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.ServerVersion)
                .Width(80)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.LocalVersion)
                .Width(80)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.AccessRight)
                .Width(80)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.Doc)
                .Width(40)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.Action)
                .Width(100)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);

            new LabelView(LocaleText.ReleaseNote)
                .Width(100)
                .TextMiddleCenter()
                .FontSize(12)
                .FontBold()
                .AddTo(this);
        }

        class LocaleText
        {
            public static string PackageName
            {
                get { return Language.IsChinese ? " 模块名" : " Package Name"; }
            }

            public static string ServerVersion
            {
                get { return Language.IsChinese ? "服务器版本" : "Server Version"; }
            }

            public static string LocalVersion
            {
                get { return Language.IsChinese ? "本地版本" : "Local Version"; }
            }

            public static string AccessRight
            {
                get { return Language.IsChinese ? "访问权限" : "Access Right"; }
            }

            public static string Doc
            {
                get { return Language.IsChinese ? "文档" : "Doc"; }
            }

            public static string Action
            {
                get { return Language.IsChinese ? "动作" : "Action"; }
            }

            public static string ReleaseNote
            {
                get { return Language.IsChinese ? "版本说明" : "ReleaseNote Note"; }
            }
        }
    }

    public interface IEditorStrangeMVCCommand
    {
        void Execute();
    }

    public class PackageManagerApp
    {
        public IQFrameworkContainer Container = new QFrameworkContainer();

        public PackageManagerApp()
        {
            // 注册好 自己的实例
            Container.RegisterInstance<IQFrameworkContainer>(Container);

            // 配置命令的执行
            Dependencies.PackageKit.TypeEventSystem.Register<IEditorStrangeMVCCommand>(OnCommandExecute);
            
            InstalledPackageVersions.Reload();

            // 注册好 model
            var model = new PackageManagerModel
            {
                PackageDatas = PackageInfosRequestCache.Get().PackageDatas
            };

            Container.RegisterInstance(model);

            Container.Register<IPackageManagerServer, PackageManagerServer>();
        }

        void OnCommandExecute(IEditorStrangeMVCCommand command)
        {
            Container.Inject(command);
            command.Execute();
        }

        public void Dispose()
        {
            Dependencies.PackageKit.TypeEventSystem.UnRegister<IEditorStrangeMVCCommand>(OnCommandExecute);

            Container.Clear();
            Container = null;
        }
    }

    public class PackageInfosRequestCache
    {
        public List<PackageData> PackageDatas = new List<PackageData>();

        private static string mFilePath
        {
            get
            {
                return (Application.dataPath + "/.qframework/PackageManager/").CreateDirIfNotExists() +
                       "PackageInfosRequestCache.json";
            }
        }

        public static PackageInfosRequestCache Get()
        {
            if (File.Exists(mFilePath))
            {
                return SerializeHelper.LoadJson<PackageInfosRequestCache>(mFilePath);
            }

            return new PackageInfosRequestCache();
        }

        public void Save()
        {
            this.SaveJson(mFilePath);
        }
    }

    public class PackageManagerModel
    {
        public PackageManagerModel()
        {
            PackageDatas = PackageInfosRequestCache.Get().PackageDatas;
        }

        public List<PackageData> PackageDatas = new List<PackageData>();

        public bool VersionCheck
        {
            get { return EditorPrefs.GetBool("QFRAMEWORK_VERSION_CHECK", true); }
            set { EditorPrefs.SetBool("QFRAMEWORK_VERSION_CHECK", value); }
        }
    }

    public class PackageManagerViewUpdate
    {
        public List<PackageData> PackageDatas { get; set; }

        public bool VersionCheck { get; set; }
    }

    public class PackageManagerStartUpCommand : IEditorStrangeMVCCommand
    {
        [Inject]
        public PackageManagerModel Model { get; set; }

        [Inject]
        public IPackageManagerServer Server { get; set; }

        public void Execute()
        {
            Debug.Log("Execute Start Up");
            Dependencies.PackageKit.TypeEventSystem.Send(new PackageManagerViewUpdate()
            {
                PackageDatas = Model.PackageDatas,
                VersionCheck = Model.VersionCheck
            });

            Server.GetAllRemotePackageInfo(list =>
            {
                Dependencies.PackageKit.TypeEventSystem.Send(new PackageManagerViewUpdate()
                {
                    PackageDatas = PackageInfosRequestCache.Get().PackageDatas,
                    VersionCheck = Model.VersionCheck
                });
            });
        }
    }

    public class PackageManagerView : IPackageKitView
    {
        private static readonly string EXPORT_ROOT_DIR = Application.dataPath.CombinePath("../");

        public static string ExportPaths(string exportPackageName, params string[] paths)
        {
            if (Directory.Exists(paths[0]))
            {
                if (paths[0].EndsWith("/"))
                {
                    paths[0] = paths[0].Remove(paths[0].Length - 1);
                }

                var filePath = EXPORT_ROOT_DIR.CombinePath(exportPackageName);
                AssetDatabase.ExportPackage(paths,
                    filePath, ExportPackageOptions.Recurse);
                AssetDatabase.Refresh();

                return filePath;
            }

            return string.Empty;
        }


        PackageManagerApp mPackageManagerApp = new PackageManagerApp();

        private Vector2 mScrollPos;


        private Action mOnToolbarIndexChanged;

        public int ToolbarIndex
        {
            get { return EditorPrefs.GetInt("PM_TOOLBAR_INDEX", 0); }
            set
            {
                EditorPrefs.SetInt("PM_TOOLBAR_INDEX", value);
                mOnToolbarIndexChanged.InvokeGracefully();
            }
        }

        private string[] mToolbarNamesLogined =
            {"Framework", "Plugin", "UIKitComponent", "Shader", "AppOrTemplate", "Private", "Master"};

        private string[] mToolbarNamesUnLogined = {"Framework", "Plugin", "UIKitComponent", "Shader", "AppOrTemplate"};

        public string[] ToolbarNames
        {
            get { return User.Logined ? mToolbarNamesLogined : mToolbarNamesUnLogined; }
        }

        public IEnumerable<PackageData> SelectedPackageType(List<PackageData> packageDatas)
        {
            switch (ToolbarIndex)
            {
                case 0:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.FrameworkModule)
                        .OrderBy(p => p.Name);
                case 1:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.Plugin)
                        .OrderBy(p => p.Name);
                case 2:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.UIKitComponent)
                        .OrderBy(p => p.Name);
                case 3:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.Shader)
                        .OrderBy(p => p.Name);
                case 4:
                    return packageDatas.Where(packageData =>
                        packageData.Type == PackageType.AppOrGameDemoOrTemplate).OrderBy(p => p.Name);
                case 5:
                    return packageDatas.Where(packageData =>
                        packageData.AccessRight == PackageAccessRight.Private).OrderBy(p => p.Name);
                case 6:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.Master)
                        .OrderBy(p => p.Name);
                default:
                    return packageDatas.Where(packageData => packageData.Type == PackageType.FrameworkModule)
                        .OrderBy(p => p.Name);
            }
        }

        public IQFrameworkContainer Container { get; set; }

        public int RenderOrder
        {
            get { return 1; }
        }

        public bool Ignore { get; private set; }

        public bool Enabled
        {
            get { return true; }
        }

        private VerticalLayout mRootLayout = new VerticalLayout();

        public void Init(IQFrameworkContainer container)
        {
            Dependencies.PackageKit.TypeEventSystem.Register<PackageManagerViewUpdate>(OnRefresh);

            // 执行
            Dependencies.PackageKit.TypeEventSystem.Send<IEditorStrangeMVCCommand>(new PackageManagerStartUpCommand());
        }

        void OnRefresh(PackageManagerViewUpdate viewUpdateEvent)
        {
            Debug.Log("Start Up");
            mRootLayout = new VerticalLayout();

            var treeNode = new TreeNode(true, LocaleText.FrameworkPackages).AddTo(mRootLayout);

            var verticalLayout = new VerticalLayout("box");

            treeNode.Add2Spread(verticalLayout);

            new ToolbarView(ToolbarIndex)
                .Menus(ToolbarNames.ToList())
                .AddTo(verticalLayout)
                .Index.Bind(newIndex => ToolbarIndex = newIndex);


            new HeaderView()
                .AddTo(verticalLayout);

            var packageList = new VerticalLayout("box")
                .AddTo(verticalLayout);

            var scroll = new ScrollLayout()
                .Height(240)
                .AddTo(packageList);

            new SpaceView(2).AddTo(scroll);

            mOnToolbarIndexChanged = () =>
            {
                scroll.Clear();

                foreach (var packageData in SelectedPackageType(viewUpdateEvent.PackageDatas))
                {
                    new SpaceView(2).AddTo(scroll);
                    new PackageView(packageData).AddTo(scroll);
                }
            };

            foreach (var packageData in SelectedPackageType(viewUpdateEvent.PackageDatas))
            {
                new SpaceView(2).AddTo(scroll);
                new PackageView(packageData).AddTo(scroll);
            }
        }

        public void OnUpdate()
        {
        }

        public void OnGUI()
        {
            mRootLayout.DrawGUI();
        }

        public void OnDispose()
        {
            Dependencies.PackageKit.TypeEventSystem.UnRegister<PackageManagerViewUpdate>(OnRefresh);

            mPackageManagerApp.Dispose();
            mPackageManagerApp = null;
        }


        class LocaleText
        {
            public static string FrameworkPackages
            {
                get { return Language.IsChinese ? "框架模块" : "Framework Packages"; }
            }

            public static string VersionCheck
            {
                get { return Language.IsChinese ? "版本检测" : "Version Check"; }
            }
        }
    }

    public enum ResponseType
    {
        SUCCEED,
        EXCEPTION,
        TIMEOUT,
    }

    public static class EditorHttp
    {
        public class EditorWWWExecuter
        {
            private WWW                          mWWW;
            private Action<ResponseType, string> mResponse;

            public EditorWWWExecuter(WWW www, Action<ResponseType, string> response)
            {
                mWWW = www;
                mResponse = response;
                EditorApplication.update += Update;
            }

            void Update()
            {
                if (mWWW != null && mWWW.isDone)
                {
                    if (string.IsNullOrEmpty(mWWW.error))
                    {
                        mResponse(ResponseType.SUCCEED, mWWW.text);
                    }
                    else
                    {
                        mResponse(ResponseType.EXCEPTION, mWWW.error);
                    }

                    Dispose();
                }
            }

            void Dispose()
            {
                mWWW.Dispose();
                mWWW = null;

                EditorApplication.update -= Update;
            }
        }


        public static void Get(string url, Action<ResponseType, string> response)
        {
            new EditorWWWExecuter(new WWW(url), response);
        }

        public static void Post(string url, WWWForm form, Action<ResponseType, string> response)
        {
            new EditorWWWExecuter(new WWW(url, form), response);
        }
    }

    public static class FrameworkMenuItems
    {
        public const string Preferences = "QFramework/Preferences... %e";
        public const string PackageKit  = "QFramework/PackageKit... %#e";

        public const string Feedback = "QFramework/Feedback";
    }

    public static class FrameworkMenuItemsPriorities
    {
        public const int Preferences = 1;

        public const int Feedback = 11;
    }

    public interface IPackageKitView
    {
        IQFrameworkContainer Container { get; set; }

        /// <summary>
        /// 1 after 0
        /// </summary>
        int RenderOrder { get; }

        bool Ignore { get; }

        bool Enabled { get; }

        void Init(IQFrameworkContainer container);

        void OnUpdate();
        void OnGUI();

        void OnDispose();
    }

    public class PackageKitWindow : IMGUIEditorWindow
    {
        class LocaleText
        {
            public static string QFrameworkSettings
            {
                get { return Language.IsChinese ? "QFramework 设置" : "QFramework Settings"; }
            }
        }

        [MenuItem(FrameworkMenuItems.Preferences, false, FrameworkMenuItemsPriorities.Preferences)]
        [MenuItem(FrameworkMenuItems.PackageKit, false, FrameworkMenuItemsPriorities.Preferences)]
        private static void Open()
        {
            var packageKitWindow = Create<PackageKitWindow>(true);
            packageKitWindow.titleContent = new GUIContent(LocaleText.QFrameworkSettings);
            packageKitWindow.position = new Rect(100, 100, 690, 800);
            packageKitWindow.Show();
        }

        private const string URL_FEEDBACK = "http://feathub.com/liangxiegame/QFramework";

        [MenuItem(FrameworkMenuItems.Feedback, false, FrameworkMenuItemsPriorities.Feedback)]
        private static void Feedback()
        {
            Application.OpenURL(URL_FEEDBACK);
        }

        public override void OnUpdate()
        {
            mPackageKitViews.ForEach(view => view.OnUpdate());
        }

        public List<IPackageKitView> mPackageKitViews = null;

        protected override void Init()
        {
            var label = GUI.skin.label;
            PackageApplication.Container = null;

            RemoveAllChidren();

            mPackageKitViews = PackageApplication.Container
                .ResolveAll<IPackageKitView>()
                .OrderBy(view => view.RenderOrder)
                .ToList();

            PackageApplication.Container.RegisterInstance(this);
        }

        public override void OnGUI()
        {
            base.OnGUI();
            mPackageKitViews.ForEach(view => view.OnGUI());
        }

        public override void OnClose()
        {
            mPackageKitViews.ForEach(view => view.OnDispose());

            RemoveAllChidren();
        }
    }

    public static class PackageApplication
    {
        public static  List<Assembly>                  CachedAssemblies { get; set; }
        private static Dictionary<Type, IEventManager> mEventManagers;

        private static Dictionary<Type, IEventManager> EventManagers
        {
            get { return mEventManagers ?? (mEventManagers = new Dictionary<Type, IEventManager>()); }
            set { mEventManagers = value; }
        }

        private static QFrameworkContainer mContainer;

        public static QFrameworkContainer Container
        {
            get
            {
                if (mContainer != null) return mContainer;
                mContainer = new QFrameworkContainer();
                InitializeContainer(mContainer);
                return mContainer;
            }
            set
            {
                mContainer = value;
                if (mContainer == null)
                {
                    IEventManager eventManager;
                    EventManagers.TryGetValue(typeof(ISystemResetEvents), out eventManager);
                    EventManagers.Clear();
                    var events = eventManager as EventManager<ISystemResetEvents>;
                    if (events != null)
                    {
                        events.Signal(_ => _.SystemResetting());
                    }
                }
            }
        }

        public static IEnumerable<Type> GetDerivedTypes<T>(bool includeAbstract = false, bool includeBase = true)
        {
            var type = typeof(T);
            if (includeBase)
                yield return type;
            if (includeAbstract)
            {
                foreach (var assembly in CachedAssemblies)
                {
                    foreach (var t in assembly
                        .GetTypes()
                        .Where(x => type.IsAssignableFrom(x)))
                    {
                        yield return t;
                    }
                }
            }
            else
            {
                var items = new List<Type>();
                foreach (var assembly in CachedAssemblies)
                {
                    try
                    {
                        items.AddRange(assembly.GetTypes()
                            .Where(x => type.IsAssignableFrom(x) && !x.IsAbstract));
                    }
                    catch (Exception ex)
                    {
                        Log.I(ex.Message);
//						InvertApplication.Log(ex.Message);
                    }
                }

                foreach (var item in items)
                    yield return item;
            }
        }

        public static System.Action ListenFor(Type eventInterface, object listenerObject)
        {
            var listener = listenerObject;

            IEventManager manager;
            if (!EventManagers.TryGetValue(eventInterface, out manager))
            {
                EventManagers.Add(eventInterface,
                    manager = (IEventManager) Activator.CreateInstance(
                        typeof(EventManager<>).MakeGenericType(eventInterface)));
            }

            var m = manager;


            return m.AddListener(listener);
        }

        private static IPackageKitView[] mPlugins;

        public static IPackageKitView[] Plugins
        {
            get { return mPlugins ?? (mPlugins = Container.ResolveAll<IPackageKitView>().ToArray()); }
            set { mPlugins = value; }
        }

        private static void InitializeContainer(IQFrameworkContainer container)
        {
            mPlugins = null;
            container.RegisterInstance(container);
            var pluginTypes = GetDerivedTypes<IPackageKitView>(false, false).ToArray();
//			// Load all plugins
            foreach (var diagramPlugin in pluginTypes)
            {
//				if (pluginTypes.Any(p => p.BaseType == diagramPlugin)) continue;
                var pluginInstance = Activator.CreateInstance((Type) diagramPlugin) as IPackageKitView;
                if (pluginInstance == null) continue;
                container.RegisterInstance(pluginInstance, diagramPlugin.Name, false);
                container.RegisterInstance(pluginInstance.GetType(), pluginInstance);
                if (pluginInstance.Enabled)
                {
                    foreach (var item in diagramPlugin.GetInterfaces())
                    {
                        ListenFor(item, pluginInstance);
                    }
                }
            }

            container.InjectAll();

            foreach (var diagramPlugin in Plugins.OrderBy(p => p.RenderOrder).Where(p => !p.Ignore))
            {
                if (diagramPlugin.Enabled)
                {
                    var start = DateTime.Now;
                    diagramPlugin.Container = Container;
                    diagramPlugin.Init(Container);
                }
            }

            foreach (var diagramPlugin in Plugins.OrderBy(p => p.RenderOrder).Where(p => !p.Ignore))
            {
                if (diagramPlugin.Enabled)
                {
                    var start = DateTime.Now;
                    container.Inject(diagramPlugin);
//					diagramPlugin.Loaded(Container);
//					diagramPlugin.LoadTime = DateTime.Now.Subtract(start);
                }
            }

            SignalEvent<ISystemResetEvents>(_ => _.SystemRestarted());
        }

        public static void SignalEvent<TEvents>(Action<TEvents> action) where TEvents : class
        {
            IEventManager manager;
            if (!EventManagers.TryGetValue(typeof(TEvents), out manager))
            {
                EventManagers.Add(typeof(TEvents), manager = new EventManager<TEvents>());
            }

            var m = manager as EventManager<TEvents>;
            m.Signal(action);
        }

        static PackageApplication()
        {
            CachedAssemblies = new List<Assembly>
            {
                typeof(int).Assembly, typeof(List<>).Assembly
            };

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("QF") || assembly.FullName.StartsWith("Assembly-CSharp-Editor"))
                {
                    CachedAssembly(assembly);
                }
            }
        }

        public static void CachedAssembly(Assembly assembly)
        {
            if (CachedAssemblies.Contains(assembly)) return;
            CachedAssemblies.Add(assembly);
        }
    }

    public interface IEventManager
    {
        System.Action AddListener(object listener);
        void Signal(Action<object> obj);
    }

    public class EventManager<T> : IEventManager where T : class
    {
        private List<T> _listeners;

        public List<T> Listeners
        {
            get { return _listeners ?? (_listeners = new List<T>()); }
            set { _listeners = value; }
        }

        public void Signal(Action<object> obj)
        {
            foreach (var item in Listeners)
            {
                var item1 = item;
                obj(item1);
            }
        }

        public void Signal(Action<T> action)
        {
            foreach (var item in Listeners)
            {
                //InvertApplication.Log(typeof(T).Name + " was signaled on " + item.GetType().Name);
                var item1 = item;
                action(item1);
            }
        }

        public System.Action Subscribe(T listener)
        {
            if (!Listeners.Contains(listener))
                Listeners.Add(listener);

            return () => { Unsubscribe(listener); };
        }

        private void Unsubscribe(T listener)
        {
            Listeners.Remove(listener);
        }

        public System.Action AddListener(object listener)
        {
            return Subscribe(listener as T);
        }
    }

    public interface ISystemResetEvents
    {
        void SystemResetting();
        void SystemRestarted();
    }
}


namespace Dependencies.PackageKit
{
    using Object = UnityEngine.Object;

    public class Language
    {
        public static bool IsChinese
        {
            get
            {
                return Application.systemLanguage == SystemLanguage.Chinese ||
                       Application.systemLanguage == SystemLanguage.ChineseSimplified;
            }
        }
    }

    public abstract class IMGUIEditorWindow : EditorWindow
    {
        public static T Create<T>(bool utility, string title = null) where T : IMGUIEditorWindow
        {
            return string.IsNullOrEmpty(title) ? GetWindow<T>(utility) : GetWindow<T>(utility, title);
        }

        private readonly List<IView> mChildren = new List<IView>();

        private bool mVisible = true;

        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }

        public void AddChild(IView childView)
        {
            mChildren.Add(childView);
        }

        public void RemoveChild(IView childView)
        {
            mChildren.Remove(childView);
        }

        public List<IView> Children
        {
            get { return mChildren; }
        }

        public void RemoveAllChidren()
        {
            mChildren.Clear();
        }

        public abstract void OnClose();


        public abstract void OnUpdate();

        private void OnDestroy()
        {
            OnClose();
        }

        protected abstract void Init();

        private bool mInited = false;

        public virtual void OnGUI()
        {
            if (!mInited)
            {
                Init();
                mInited = true;
            }

            OnUpdate();

            if (Visible)
            {
                mChildren.ForEach(childView => childView.DrawGUI());
            }
        }
    }

    public class SubWindow : EditorWindow, ILayout
    {
        void IView.Hide()
        {
        }

        void IView.DrawGUI()
        {
        }

        ILayout IView.Parent { get; set; }

        private GUIStyle mStyle = null;

        public GUIStyle Style
        {
            get { return mStyle; }
            set { mStyle = value; }
        }

        Color IView.BackgroundColor { get; set; }


        private List<IView> mPrivateChildren = new List<IView>();

        private List<IView> mChildren
        {
            get { return mPrivateChildren; }
            set { mPrivateChildren = value; }
        }

        void IView.RefreshNextFrame()
        {
        }

        void IView.AddLayoutOption(GUILayoutOption option)
        {
        }

        void IView.RemoveFromParent()
        {
        }

        void IView.Refresh()
        {
        }

        public void AddChild(IView view)
        {
            mChildren.Add(view);
            view.Parent = this;
        }

        public void RemoveChild(IView view)
        {
            mChildren.Add(view);
            view.Parent = null;
        }

        public void Clear()
        {
            mChildren.Clear();
        }

        private void OnGUI()
        {
            mChildren.ForEach(view => view.DrawGUI());
        }

        public void Dispose()
        {
        }
    }

    public class TypeEventSystem
    {
        /// <summary>
        /// 接口 只负责存储在字典中
        /// </summary>
        interface IRegisterations
        {
        }

        /// <summary>
        /// 多个注册
        /// </summary>
        class Registerations<T> : IRegisterations
        {
            /// <summary>
            /// 不需要 List<Action<T>> 了
            /// 因为委托本身就可以一对多注册
            /// </summary>
            public Action<T> OnReceives = obj => { };
        }

        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<Type, IRegisterations> mTypeEventDict = new Dictionary<Type, IRegisterations>();

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(System.Action<T> onReceive)
        {
            var type = typeof(T);

            IRegisterations registerations = null;

            if (mTypeEventDict.TryGetValue(type, out registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnReceives += onReceive;
            }
            else
            {
                var reg = new Registerations<T>();
                reg.OnReceives += onReceive;
                mTypeEventDict.Add(type, reg);
            }
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public static void UnRegister<T>(System.Action<T> onReceive)
        {
            var type = typeof(T);

            IRegisterations registerations = null;

            if (mTypeEventDict.TryGetValue(type, out registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnReceives -= onReceive;
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        public static void Send<T>(T t)
        {
            var type = typeof(T);

            IRegisterations registerations = null;

            if (mTypeEventDict.TryGetValue(type, out registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnReceives(t);
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        public static void Send<T>() where T : new()
        {
            var type = typeof(T);

            IRegisterations registerations = null;

            if (mTypeEventDict.TryGetValue(type, out registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnReceives(new T());
            }
        }
    }

    public abstract class View : IView
    {
        public class EventRecord
        {
            public int Key;

            public Action<object> OnEvent;
        }

        List<EventRecord> mPrivteEventRecords = new List<EventRecord>();

        protected List<EventRecord> mEventRecords
        {
            get { return mPrivteEventRecords; }
        }

        protected void RegisterEvent<T>(T key, Action<object> onEvent) where T : IConvertible
        {
            EventDispatcher.Register(key, onEvent);

            mEventRecords.Add(new EventRecord
            {
                Key = key.ToInt32(null),
                OnEvent = onEvent
            });
        }

        protected void UnRegisterAll()
        {
            mEventRecords.ForEach(record => { EventDispatcher.UnRegister(record.Key, record.OnEvent); });

            mEventRecords.Clear();
        }

        protected void SendEvent<T>(T key, object arg) where T : IConvertible
        {
            EventDispatcher.Send(key, arg);
        }

        private bool mVisible = true;

        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }

        private List<GUILayoutOption> mprivateLayoutOptions = new List<GUILayoutOption>();

        private List<GUILayoutOption> mLayoutOptions
        {
            get { return mprivateLayoutOptions; }
        }

        protected GUILayoutOption[] LayoutStyles { get; private set; }


        private GUIStyle mStyle = new GUIStyle();

        public GUIStyle Style
        {
            get { return mStyle; }
            protected set { mStyle = value; }
        }

        private Color mBackgroundColor = GUI.backgroundColor;

        public Color BackgroundColor
        {
            get { return mBackgroundColor; }
            set { mBackgroundColor = value; }
        }

        public void RefreshNextFrame()
        {
            this.PushCommand(Refresh);
        }

        public void AddLayoutOption(GUILayoutOption option)
        {
            mLayoutOptions.Add(option);
        }

        public void Show()
        {
            Visible = true;
            OnShow();
        }

        protected virtual void OnShow()
        {
        }

        public void Hide()
        {
            Visible = false;
            OnHide();
        }

        protected virtual void OnHide()
        {
        }


        private Color mPreviousBackgroundColor;

        public void DrawGUI()
        {
            BeforeDraw();

            if (Visible)
            {
                mPreviousBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = BackgroundColor;
                OnGUI();
                GUI.backgroundColor = mPreviousBackgroundColor;
            }
        }

        private bool mBeforeDrawCalled = false;

        void BeforeDraw()
        {
            if (!mBeforeDrawCalled)
            {
                OnBeforeDraw();

                LayoutStyles = mLayoutOptions.ToArray();

                mBeforeDrawCalled = true;
            }
        }

        protected virtual void OnBeforeDraw()
        {
        }

        public ILayout Parent { get; set; }

        public void RemoveFromParent()
        {
            Parent.RemoveChild(this);
        }

        public virtual void Refresh()
        {
            OnRefresh();
        }

        protected virtual void OnRefresh()
        {
        }

        protected abstract void OnGUI();

        public void Dispose()
        {
            UnRegisterAll();
            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
        }
    }

    public abstract class Window : EditorWindow, IDisposable
    {
        public static Window MainWindow { get; protected set; }

        public IMGUIViewController ViewController { get; set; }

        public T CreateViewController<T>() where T : IMGUIViewController, new()
        {
            var t = new T();
            t.SetUpView();
            return t;
        }

        public static void Open<T>(string title) where T : Window
        {
            MainWindow = GetWindow<T>(true);

            if (!MainWindow.mShowing)
            {
                MainWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 600);
                MainWindow.titleContent = new GUIContent(title);
                MainWindow.Init();
                MainWindow.mShowing = true;
                MainWindow.Show();
            }
            else
            {
                MainWindow.mShowing = false;
                MainWindow.Dispose();
                MainWindow.Close();
                MainWindow = null;
            }
        }

        public static SubWindow CreateSubWindow(string name = "SubWindow")
        {
            var window = GetWindow<SubWindow>(true, name);
            window.Clear();
            return window;
        }

        void Init()
        {
            OnInit();
        }

        private Queue<Action> mPrivateCommands = new Queue<Action>();

        private Queue<Action> mCommands
        {
            get { return mPrivateCommands; }
        }

        public void PushCommand(Action command)
        {
            Debug.Log("push command");

            mCommands.Enqueue(command);
        }

        private void OnGUI()
        {
            ViewController.View.DrawGUI();

            while (mCommands.Count > 0)
            {
                Debug.Log(mCommands.Count);
                mCommands.Dequeue().Invoke();
            }
        }

        public void Dispose()
        {
            OnDispose();
        }

        protected bool mShowing = false;


        protected abstract void OnInit();
        protected abstract void OnDispose();
    }


    public class ExpandLayout : Layout
    {
        public ExpandLayout(string label)
        {
            Label = label;
        }

        public string Label { get; set; }


        protected override void OnGUIBegin()
        {
        }


        protected override void OnGUI()
        {
//            if (GUIHelpers.DoToolbarEx(Label))
//            {
//                foreach (var child in Children)
//                {
//                    child.DrawGUI();
//                }
//            }
        }

        protected override void OnGUIEnd()
        {
        }
    }

    public class HorizontalLayout : Layout
    {
        public string HorizontalStyle { get; set; }

        public HorizontalLayout(string horizontalStyle = null)
        {
            HorizontalStyle = horizontalStyle;
        }

        protected override void OnGUIBegin()
        {
            if (string.IsNullOrEmpty(HorizontalStyle))
            {
                GUILayout.BeginHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal(HorizontalStyle);
            }
        }

        protected override void OnGUIEnd()
        {
            GUILayout.EndHorizontal();
        }
    }

    public class ScrollLayout : Layout
    {
        Vector2 mScrollPos = Vector2.zero;

        protected override void OnGUIBegin()
        {
            mScrollPos = GUILayout.BeginScrollView(mScrollPos, LayoutStyles);
        }

        protected override void OnGUIEnd()
        {
            GUILayout.EndScrollView();
        }
    }

    public class TreeNode : VerticalLayout
    {
        public Property<bool> Spread = null;

        public string Content;


        HorizontalLayout mFirstLine = new HorizontalLayout();

        private VerticalLayout mSpreadView = new VerticalLayout();

        public TreeNode(bool spread, string content, int indent = 0)
        {
            Content = content;
            Spread = new Property<bool>(spread);

            Style = new GUIStyle(EditorStyles.foldout);

            mFirstLine.AddTo(this);
            mFirstLine.AddChild(new SpaceView(indent));

            new CustomView(() =>
            {
                Spread.Value = EditorGUILayout.Foldout(Spread.Value, Content, true, Style);
            }).AddTo(mFirstLine);

            new CustomView(() =>
            {
                if (Spread.Value)
                {
                    mSpreadView.DrawGUI();
                }
            }).AddTo(this);
        }

        public TreeNode Add2FirstLine(IView view)
        {
            view.AddTo(mFirstLine);
            return this;
        }

        public TreeNode FirstLineBox()
        {
            mFirstLine.HorizontalStyle = "box";

            return this;
        }

        public TreeNode SpreadBox()
        {
            mSpreadView.VerticalStyle = "box";

            return this;
        }

        public TreeNode Add2Spread(IView view)
        {
            view.AddTo(mSpreadView);
            return this;
        }
    }

    public class VerticalLayout : Layout
    {
        public string VerticalStyle { get; set; }

        public VerticalLayout(string verticalStyle = null)
        {
            VerticalStyle = verticalStyle;
        }

        protected override void OnGUIBegin()
        {
            if (string.IsNullOrEmpty(VerticalStyle))
            {
                GUILayout.BeginVertical(LayoutStyles);
            }
            else
            {
                GUILayout.BeginVertical(VerticalStyle, LayoutStyles);
            }
        }

        protected override void OnGUIEnd()
        {
            GUILayout.EndVertical();
        }
    }

    public abstract class Layout : View, ILayout
    {
        protected List<IView> Children = new List<IView>();

        public void AddChild(IView view)
        {
            Children.Add(view);
            view.Parent = this;
        }

        public void RemoveChild(IView view)
        {
            this.PushCommand(() =>
            {
                Children.Remove(view);
                view.Parent = null;
            });

            view.Dispose();
        }

        public void Clear()
        {
            Children.Clear();
        }

        public override void Refresh()
        {
            Children.ForEach(view => view.Refresh());
            base.Refresh();
        }

        protected override void OnGUI()
        {
            OnGUIBegin();
            foreach (var child in Children)
            {
                child.DrawGUI();
            }

            OnGUIEnd();
        }

        protected abstract void OnGUIBegin();
        protected abstract void OnGUIEnd();
    }

    public interface IView : IDisposable
    {
        void Show();

        void Hide();

        void DrawGUI();

        ILayout Parent { get; set; }

        GUIStyle Style { get; }

        Color BackgroundColor { get; set; }

        void RefreshNextFrame();

        void AddLayoutOption(GUILayoutOption option);

        void RemoveFromParent();

        void Refresh();
    }

    public interface ILayout : IView
    {
        void AddChild(IView view);

        void RemoveChild(IView view);

        void Clear();
    }

    public static class WindowExtension
    {
        public static T PushCommand<T>(this T view, Action command) where T : IView
        {
            Window.MainWindow.PushCommand(command);
            return view;
        }
    }

    public static class SubWindowExtension
    {
        public static T Postion<T>(this T subWindow, int x, int y) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.x = x;
            rect.y = y;
            subWindow.position = rect;

            return subWindow;
        }

        public static T Size<T>(this T subWindow, int width, int height) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.width = width;
            rect.height = height;
            subWindow.position = rect;

            return subWindow;
        }

        public static T PostionScreenCenter<T>(this T subWindow) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.x = Screen.width / 2;
            rect.y = Screen.height / 2;
            subWindow.position = rect;

            return subWindow;
        }
    }

    public static class ViewExtension
    {
        public static T Width<T>(this T view, float width) where T : IView
        {
            view.AddLayoutOption(GUILayout.Width(width));
            return view;
        }

        public static T Height<T>(this T view, float height) where T : IView
        {
            view.AddLayoutOption(GUILayout.Height(height));
            return view;
        }

        public static T MaxHeight<T>(this T view, float height) where T : IView
        {
            view.AddLayoutOption(GUILayout.MaxHeight(height));
            return view;
        }

        public static T MinHeight<T>(this T view, float height) where T : IView
        {
            view.AddLayoutOption(GUILayout.MinHeight(height));
            return view;
        }

        public static T ExpandHeight<T>(this T view) where T : IView
        {
            view.AddLayoutOption(GUILayout.ExpandHeight(true));
            return view;
        }


        public static T TextMiddleLeft<T>(this T view) where T : IView
        {
            view.Style.alignment = TextAnchor.MiddleLeft;
            return view;
        }

        public static T TextMiddleRight<T>(this T view) where T : IView
        {
            view.Style.alignment = TextAnchor.MiddleRight;
            return view;
        }

        public static T TextLowerRight<T>(this T view) where T : IView
        {
            view.Style.alignment = TextAnchor.LowerRight;
            return view;
        }

        public static T TextMiddleCenter<T>(this T view) where T : IView
        {
            view.Style.alignment = TextAnchor.MiddleCenter;
            return view;
        }

        public static T TextLowerCenter<T>(this T view) where T : IView
        {
            view.Style.alignment = TextAnchor.LowerCenter;
            return view;
        }

        public static T Color<T>(this T view, Color color) where T : IView
        {
            view.BackgroundColor = color;
            return view;
        }

        public static T FontColor<T>(this T view, Color color) where T : IView
        {
            view.Style.normal.textColor = color;
            return view;
        }

        public static T FontBold<T>(this T view) where T : IView
        {
            view.Style.fontStyle = FontStyle.Bold;
            return view;
        }

        public static T FontNormal<T>(this T view) where T : IView
        {
            view.Style.fontStyle = FontStyle.Normal;
            return view;
        }

        public static T FontSize<T>(this T view, int fontSize) where T : IView
        {
            view.Style.fontSize = fontSize;
            return view;
        }
    }

    public static class LayoutExtension
    {
        public static T AddTo<T>(this T view, ILayout parent) where T : IView
        {
            parent.AddChild(view);
            return view;
        }
    }

    [Serializable]
    public class IntProperty : Property<int>
    {
        public int Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }
    }


    [Serializable]
    public class Property<T>
    {
        public Property()
        {
        }

        private bool setted = false;

        public Property(T initValue)
        {
            mValue = initValue;
        }

        public T Value
        {
            get { return mValue; }
            set
            {
                if (value == null || !value.Equals(mValue) || !setted)
                {
                    mValue = value;

                    if (mSetter != null)
                    {
                        mSetter.Invoke(mValue);
                    }

                    setted = true;
                }
            }
        }

        private T mValue;

        /// <summary>
        /// TODO:注销也要做下
        /// </summary>
        /// <param name="setter"></param>
        public void Bind(Action<T> setter)
        {
            mSetter += setter;
            mBindings.Add(setter);
        }

        private List<Action<T>> mBindings = new List<Action<T>>();

        public void UnBindAll()
        {
            foreach (var binding in mBindings)
            {
                mSetter -= binding;
            }
        }

        private event Action<T> mSetter;
    }

    public static class EditorUtils
    {
        public static void MarkCurrentSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static string CurrentSelectPath
        {
            get { return Selection.activeObject == null ? null : AssetDatabase.GetAssetPath(Selection.activeObject); }
        }

        public static string AssetsPath2ABSPath(string assetsPath)
        {
            string assetRootPath = Path.GetFullPath(Application.dataPath);
            return assetRootPath.Substring(0, assetRootPath.Length - 6) + assetsPath;
        }

        public static string ABSPath2AssetsPath(string absPath)
        {
            string assetRootPath = Path.GetFullPath(Application.dataPath);
            Debug.Log(assetRootPath);
            Debug.Log(Path.GetFullPath(absPath));
            return "Assets" + Path.GetFullPath(absPath).Substring(assetRootPath.Length).Replace("\\", "/");
        }


        public static string AssetPath2ReltivePath(string path)
        {
            if (path == null)
            {
                return null;
            }

            return path.Replace("Assets/", "");
        }

        public static bool ExcuteCmd(string toolName, string args, bool isThrowExcpetion = true)
        {
            Process process = new Process();
            process.StartInfo.FileName = toolName;
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            OuputProcessLog(process, isThrowExcpetion);
            return true;
        }

        public static void OuputProcessLog(Process p, bool isThrowExcpetion)
        {
            string standardError = string.Empty;
            p.BeginErrorReadLine();

            p.ErrorDataReceived += (sender, outLine) => { standardError += outLine.Data; };

            string standardOutput = string.Empty;
            p.BeginOutputReadLine();
            p.OutputDataReceived += (sender, outLine) => { standardOutput += outLine.Data; };

            p.WaitForExit();
            p.Close();

            Log.I(standardOutput);
            if (standardError.Length > 0)
            {
                if (isThrowExcpetion)
                {
                    Log.E(standardError);
                    throw new Exception(standardError);
                }

                Log.I(standardError);
            }
        }

        public static Dictionary<string, string> ParseArgs(string argString)
        {
            int curPos = argString.IndexOf('-');
            Dictionary<string, string> result = new Dictionary<string, string>();

            while (curPos != -1 && curPos < argString.Length)
            {
                int nextPos = argString.IndexOf('-', curPos + 1);
                string item = string.Empty;

                if (nextPos != -1)
                {
                    item = argString.Substring(curPos + 1, nextPos - curPos - 1);
                }
                else
                {
                    item = argString.Substring(curPos + 1, argString.Length - curPos - 1);
                }

                item = StringTrim(item);
                int splitPos = item.IndexOf(' ');

                if (splitPos == -1)
                {
                    string key = StringTrim(item);
                    result[key] = "";
                }
                else
                {
                    string key = StringTrim(item.Substring(0, splitPos));
                    string value = StringTrim(item.Substring(splitPos + 1, item.Length - splitPos - 1));
                    result[key] = value;
                }

                curPos = nextPos;
            }

            return result;
        }

        public static string GetFileMD5Value(string absPath)
        {
            if (!File.Exists(absPath))
                return "";

            MD5CryptoServiceProvider md5CSP = new MD5CryptoServiceProvider();
            FileStream file = new FileStream(absPath, FileMode.Open);
            byte[] retVal = md5CSP.ComputeHash(file);
            file.Close();
            string result = "";

            for (int i = 0; i < retVal.Length; i++)
            {
                result += retVal[i].ToString("x2");
            }

            return result;
        }

        public static string GetStrMD5Value(string str)
        {
            MD5CryptoServiceProvider md5CSP = new MD5CryptoServiceProvider();
            byte[] retVal = md5CSP.ComputeHash(Encoding.Default.GetBytes(str));
            string retStr = "";

            for (int i = 0; i < retVal.Length; i++)
            {
                retStr += retVal[i].ToString("x2");
            }

            return retStr;
        }

        public static List<Object> GetDirSubAssetsList(string dirAssetsPath, bool isRecursive = true,
            string suffix = "", bool isLoadAll = false)
        {
            string dirABSPath = ABSPath2AssetsPath(dirAssetsPath);
            Debug.Log(dirABSPath);
            List<string> assetsABSPathList = dirABSPath.GetDirSubFilePathList(isRecursive, suffix);
            List<Object> resultObjectList = new List<Object>();

            for (int i = 0; i < assetsABSPathList.Count; ++i)
            {
                Debug.Log(assetsABSPathList[i]);
                if (isLoadAll)
                {
                    Object[] objs = AssetDatabase.LoadAllAssetsAtPath(ABSPath2AssetsPath(assetsABSPathList[i]));
                    resultObjectList.AddRange(objs);
                }
                else
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(ABSPath2AssetsPath(assetsABSPathList[i]));
                    resultObjectList.Add(obj);
                }
            }

            return resultObjectList;
        }

        public static List<T> GetDirSubAssetsList<T>(string dirAssetsPath, bool isRecursive = true, string suffix = "",
            bool isLoadAll = false) where T : Object
        {
            List<T> result = new List<T>();
            List<Object> objectList = GetDirSubAssetsList(dirAssetsPath, isRecursive, suffix, isLoadAll);

            for (int i = 0; i < objectList.Count; ++i)
            {
                if (objectList[i] is T)
                {
                    result.Add(objectList[i] as T);
                }
            }

            return result;
        }

        public static string GetSelectedDirAssetsPath()
        {
            string path = string.Empty;

            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }

            return path;
        }

        public static string StringTrim(string str, params char[] trimer)
        {
            int startIndex = 0;
            int endIndex = str.Length;

            for (int i = 0; i < str.Length; ++i)
            {
                if (!IsInCharArray(trimer, str[i]))
                {
                    startIndex = i;
                    break;
                }
            }

            for (int i = str.Length - 1; i >= 0; --i)
            {
                if (!IsInCharArray(trimer, str[i]))
                {
                    endIndex = i;
                    break;
                }
            }

            if (startIndex == 0 && endIndex == str.Length)
            {
                return string.Empty;
            }

            return str.Substring(startIndex, endIndex - startIndex + 1);
        }

        public static string StringTrim(string str)
        {
            return StringTrim(str, ' ', '\t');
        }

        static bool IsInCharArray(char[] array, char c)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] == c)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ClearAssetBundlesName()
        {
            int length = AssetDatabase.GetAllAssetBundleNames().Length;
            string[] oldAssetBundleNames = new string[length];

            for (int i = 0; i < length; i++)
            {
                oldAssetBundleNames[i] = AssetDatabase.GetAllAssetBundleNames()[i];
            }

            for (int j = 0; j < oldAssetBundleNames.Length; j++)
            {
                AssetDatabase.RemoveAssetBundleName(oldAssetBundleNames[j], true);
            }

            length = AssetDatabase.GetAllAssetBundleNames().Length;
            AssetDatabase.SaveAssets();
        }

        public static bool SetAssetBundleName(string assetsPath, string bundleName)
        {
            AssetImporter ai = AssetImporter.GetAtPath(assetsPath);

            if (ai != null)
            {
                ai.assetBundleName = bundleName + ".assetbundle";
                return true;
            }

            return false;
        }

        public static void SafeRemoveAsset(string assetsPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetsPath);

            if (obj != null)
            {
                AssetDatabase.DeleteAsset(assetsPath);
            }
        }

        public static void Abort(string errMsg)
        {
            Log.E("BatchMode Abort Exit " + errMsg);
            Thread.CurrentThread.Abort();
            Process.GetCurrentProcess().Kill();

            Environment.ExitCode = 1;
            Environment.Exit(1);

            EditorApplication.Exit(1);
        }
    }

    public static class MouseSelector
    {
        public static string GetSelectedPathOrFallback()
        {
            var path = string.Empty;

            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (path.IsNotNullAndEmpty() && File.Exists(path))
                {
                }
            }

            return path;
        }
    }

    public class BoxView : View
    {
        public string Text;

        public BoxView(string text)
        {
            Text = text;
            //Style = new GUIStyle(GUI.skin.box);
        }

        protected override void OnGUI()
        {
            GUILayout.Box(Text, GUI.skin.box, LayoutStyles);
        }
    }

    public class ColorView : View
    {
        public ColorView(Color color)
        {
            Color = new Property<Color>(color);
        }

        public Property<Color> Color { get; private set; }

        protected override void OnGUI()
        {
            Color.Value = EditorGUILayout.ColorField(Color.Value, LayoutStyles);
        }
    }

    public class ButtonView : View
    {
        public ButtonView(string text, Action onClickEvent)
        {
            Text = text;
            OnClickEvent = onClickEvent;
            //Style = new GUIStyle(GUI.skin.button);
        }

        public string Text { get; set; }

        public Action OnClickEvent { get; set; }

        protected override void OnGUI()
        {
            if (GUILayout.Button(Text, GUI.skin.button, LayoutStyles))
            {
                OnClickEvent.Invoke();
            }
        }
    }

    public class CustomView : View
    {
        public CustomView(Action onGuiAction)
        {
            OnGUIAction = onGuiAction;
        }

        public Action OnGUIAction { get; set; }

        protected override void OnGUI()
        {
            OnGUIAction.Invoke();
        }
    }

    public class EnumPopupView : View //where T : struct
    {
        public Property<Enum> ValueProperty { get; private set; }

        public EnumPopupView(Enum initValue)
        {
            ValueProperty = new Property<Enum>(initValue);
            ValueProperty.Value = initValue;

            try
            {
                Style = new GUIStyle(EditorStyles.popup);
            }
            catch (Exception e)
            {
            }
        }

        protected override void OnGUI()
        {
            Enum enumType = ValueProperty.Value;
            ValueProperty.Value = EditorGUILayout.EnumPopup(enumType, Style, LayoutStyles);
        }
    }

    public class FlexibaleSpaceView : View
    {
        protected override void OnGUI()
        {
            GUILayout.FlexibleSpace();
        }
    }

    public class ImageButtonView : View
    {
        private Texture2D mTexture2D { get; set; }

        private Action mOnClick { get; set; }

        public ImageButtonView(string texturePath, Action onClick)
        {
            mTexture2D = Resources.Load<Texture2D>(texturePath);
            mOnClick = onClick;

            //Style = new GUIStyle(GUI.skin.button);
        }

        protected override void OnGUI()
        {
            if (GUILayout.Button(mTexture2D, LayoutStyles))
            {
                mOnClick.Invoke();
            }
        }
    }

    public class LabelView : View
    {
        public string Content { get; set; }

        public LabelView(string content)
        {
            Content = content;
            try
            {
                Style = new GUIStyle(GUI.skin.label);
            }
            catch (Exception e)
            {
            }
        }


        protected override void OnGUI()
        {
            GUILayout.Label(Content, Style, LayoutStyles);
        }
    }

    public class PopupView : View
    {
        public Property<int> IndexProperty { get; private set; }

        public string[] MenuArray { get; private set; }

        public PopupView(int initValue, string[] menuArray)
        {
            MenuArray = menuArray;
            IndexProperty = new Property<int>(initValue);
            IndexProperty.Value = initValue;

            Style = new GUIStyle(EditorStyles.popup);
        }

        protected override void OnGUI()
        {
            IndexProperty.Value = EditorGUILayout.Popup(IndexProperty.Value, MenuArray, LayoutStyles);
        }
    }

    public class SpaceView : View
    {
        public int Pixel { get; set; }

        public SpaceView(int pixel = 10)
        {
            Pixel = pixel;
        }

        protected override void OnGUI()
        {
            GUILayout.Space(Pixel);
        }
    }

    public class TextAreaView : View
    {
        public TextAreaView(string content)
        {
            Content = new Property<string>(content);
            //Style = new GUIStyle(GUI.skin.textArea);
        }

        public Property<string> Content { get; set; }

        protected override void OnGUI()
        {
            Content.Value = EditorGUILayout.TextArea(Content.Value, GUI.skin.textArea, LayoutStyles);
        }
    }

    public class TextView : View
    {
        public TextView(string content)
        {
            Content = new Property<string>(content);
            //Style = GUI.skin.textField;
        }

        public Property<string> Content { get; set; }

        protected override void OnGUI()
        {
            if (mPasswordMode)
            {
                Content.Value = EditorGUILayout.PasswordField(Content.Value, GUI.skin.textField, LayoutStyles);
            }
            else
            {
                Content.Value = EditorGUILayout.TextField(Content.Value, GUI.skin.textField, LayoutStyles);
            }
        }


        private bool mPasswordMode = false;

        public TextView PasswordMode()
        {
            mPasswordMode = true;
            return this;
        }
    }

    public class ToggleView : View
    {
        public string Text { get; private set; }

        public ToggleView(string text, bool initValue = false)
        {
            Text = text;
            Toggle = new Property<bool>(initValue);

            try
            {
                Style = new GUIStyle(GUI.skin.toggle);
            }
            catch (System.Exception e)
            {
            }
        }

        public Property<bool> Toggle { get; private set; }

        protected override void OnGUI()
        {
            // Toggle.Value = GUILayout.Toggle(Toggle.Value, Text, Style, LayoutStyles);
            Toggle.Value = GUILayout.Toggle(Toggle.Value, Text, LayoutStyles);
        }
    }

    public class ToolbarView : View
    {
        public ToolbarView(int defaultIndex = 0)
        {
            Index.Value = defaultIndex;
            Index.Bind(index => MenuSelected[index].Invoke(MenuNames[index]));
        }


        public ToolbarView Menus(List<string> menuNames)
        {
            this.MenuNames = menuNames;
            // empty
            this.MenuSelected = MenuNames.Select(menuName => new Action<string>((str => { }))).ToList();
            return this;
        }

        public ToolbarView AddMenu(string name, Action<string> onMenuSelected)
        {
            MenuNames.Add(name);
            MenuSelected.Add(onMenuSelected);
            return this;
        }

        List<string> MenuNames = new List<string>();

        List<Action<string>> MenuSelected = new List<Action<string>>();

        public Property<int> Index = new Property<int>(0);

        protected override void OnGUI()
        {
            Index.Value = GUILayout.Toolbar(Index.Value, MenuNames.ToArray(), GUI.skin.button, LayoutStyles);
        }
    }

    public static class EventDispatcher
    {
        private static Dictionary<int, Action<object>> mRegisteredEvents = new Dictionary<int, Action<object>>();

        public static void Register<T>(T key, Action<object> onEvent) where T : IConvertible
        {
            int intKey = key.ToInt32(null);

            Action<object> registerdEvent;
            if (!mRegisteredEvents.TryGetValue(intKey, out registerdEvent))
            {
                registerdEvent = (_) => { };
                registerdEvent += onEvent;
                mRegisteredEvents.Add(intKey, registerdEvent);
            }
            else
            {
                mRegisteredEvents[intKey] += onEvent;
            }
        }

        public static void UnRegister<T>(T key, Action<object> onEvent) where T : IConvertible
        {
            int intKey = key.ToInt32(null);

            Action<object> registerdEvent;
            if (!mRegisteredEvents.TryGetValue(intKey, out registerdEvent))
            {
            }
            else
            {
                registerdEvent -= onEvent;
            }
        }

        public static void UnRegisterAll<T>(T key) where T : IConvertible
        {
            int intKey = key.ToInt32(null);
            mRegisteredEvents.Remove(intKey);
        }

        public static void Send<T>(T key, object arg = null) where T : IConvertible
        {
            int intKey = key.ToInt32(null);

            Action<object> registeredEvent;
            if (mRegisteredEvents.TryGetValue(intKey, out registeredEvent))
            {
                registeredEvent.Invoke(arg);
            }
        }
    }

    public static class ColorUtil
    {
        public static string ToText(this Color color)
        {
            return string.Format("{0}@{1}@{2}@{3}", color.r, color.g, color.b, color.a);
        }

        public static Color ToColor(this string colorText)
        {
            var channels = colorText.Split('@');
            return new Color(
                float.Parse(channels[0]),
                float.Parse(channels[1]),
                float.Parse(channels[2]),
                float.Parse(channels[3]));
        }
    }

    public abstract class IMGUIViewController
    {
        public VerticalLayout View = new VerticalLayout();

        public abstract void SetUpView();
    }

    /// <summary>
    /// Used by the injection container to determine if a property or field should be injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class InjectAttribute : Attribute
    {
        public InjectAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public InjectAttribute()
        {
        }
    }

    internal interface IQFrameworkContainer
    {
        /// <summary>
        /// Clears all type mappings and instances.
        /// </summary>
        void Clear();

        /// <summary>
        /// Injects registered types/mappings into an object
        /// </summary>
        /// <param name="obj"></param>
        void Inject(object obj);

        /// <summary>
        /// Injects everything that is registered at once
        /// </summary>
        void InjectAll();

        /// <summary>
        /// Register a type mapping
        /// </summary>
        /// <typeparam name="TSource">The base type.</typeparam>
        /// <typeparam name="TTarget">The concrete type</typeparam>
        void Register<TSource, TTarget>(string name = null);

        void RegisterRelation<TFor, TBase, TConcrete>();

        /// <summary>
        /// Register an instance of a type.
        /// </summary>
        /// <typeparam name="TBase"></typeparam>
        /// <param name="default"></param>
        /// <param name="injectNow"></param>
        /// <returns></returns>
        void RegisterInstance<TBase>(TBase @default, bool injectNow) where TBase : class;

        /// <summary>
        /// Register an instance of a type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="default"></param>
        /// <param name="injectNow"></param>
        /// <returns></returns>
        void RegisterInstance(Type type, object @default, bool injectNow);

        /// <summary>
        /// Register a named instance
        /// </summary>
        /// <param name="baseType">The type to register the instance for.</param>
        /// <param name="name">The name for the instance to be resolved.</param>
        /// <param name="instance">The instance that will be resolved be the name</param>
        /// <param name="injectNow">Perform the injection immediately</param>
        void RegisterInstance(Type baseType, object instance = null, string name = null, bool injectNow = true);

        void RegisterInstance<TBase>(TBase instance, string name, bool injectNow = true) where TBase : class;

        void RegisterInstance<TBase>(TBase instance) where TBase : class;

        /// <summary>
        ///  If an instance of T exist then it will return that instance otherwise it will create a new one based off mappings.
        /// </summary>
        /// <typeparam name="T">The type of instance to resolve</typeparam>
        /// <returns>The/An instance of 'instanceType'</returns>
        T Resolve<T>(string name = null, bool requireInstance = false, params object[] args) where T : class;

        TBase ResolveRelation<TBase>(Type tfor, params object[] arg);

        TBase ResolveRelation<TFor, TBase>(params object[] arg);

        /// <summary>
        /// Resolves all instances of TType or subclasses of TType.  Either named or not.
        /// </summary>
        /// <typeparam name="TType">The Type to resolve</typeparam>
        /// <returns>List of objects.</returns>
        IEnumerable<TType> ResolveAll<TType>();

        //IEnumerable<object> ResolveAll(Type type);
        void Register(Type source, Type target, string name = null);

        /// <summary>
        /// Resolves all instances of TType or subclasses of TType.  Either named or not.
        /// </summary>
        /// <typeparam name="TType">The Type to resolve</typeparam>
        /// <returns>List of objects.</returns>
        IEnumerable<object> ResolveAll(Type type);

        TypeMappingCollection  Mappings             { get; set; }
        TypeInstanceCollection Instances            { get; set; }
        TypeRelationCollection RelationshipMappings { get; set; }

        /// <summary>
        /// If an instance of instanceType exist then it will return that instance otherwise it will create a new one based off mappings.
        /// </summary>
        /// <param name="baseType">The type of instance to resolve</param>
        /// <param name="name">The type of instance to resolve</param>
        /// <param name="requireInstance">If true will return null if an instance isn't registered.</param>
        /// <returns>The/An instance of 'instanceType'</returns>
        object Resolve(Type baseType, string name = null, bool requireInstance = false,
            params object[] constructorArgs);

        object ResolveRelation(Type tfor, Type tbase, params object[] arg);
        void RegisterRelation(Type tfor, Type tbase, Type tconcrete);
        object CreateInstance(Type type, params object[] args);
    }

    /// <summary>
    /// A ViewModel Container and a factory for Controllers and commands.
    /// </summary>
    internal class QFrameworkContainer : IQFrameworkContainer
    {
        private TypeInstanceCollection _instances;
        private TypeMappingCollection  _mappings;


        public TypeMappingCollection Mappings
        {
            get { return _mappings ?? (_mappings = new TypeMappingCollection()); }
            set { _mappings = value; }
        }

        public TypeInstanceCollection Instances
        {
            get { return _instances ?? (_instances = new TypeInstanceCollection()); }
            set { _instances = value; }
        }

        public TypeRelationCollection RelationshipMappings
        {
            get { return _relationshipMappings; }
            set { _relationshipMappings = value; }
        }

        public IEnumerable<TType> ResolveAll<TType>()
        {
            foreach (var obj in ResolveAll(typeof(TType)))
            {
                yield return (TType) obj;
            }
        }

        /// <summary>
        /// Resolves all instances of TType or subclasses of TType.  Either named or not.
        /// </summary>
        /// <typeparam name="TType">The Type to resolve</typeparam>
        /// <returns>List of objects.</returns>
        public IEnumerable<object> ResolveAll(Type type)
        {
            foreach (KeyValuePair<Tuple<Type, string>, object> kv in Instances)
            {
                if (kv.Key.Item1 == type && !string.IsNullOrEmpty(kv.Key.Item2))
                    yield return kv.Value;
            }

            foreach (KeyValuePair<Tuple<Type, string>, Type> kv in Mappings)
            {
                if (!string.IsNullOrEmpty(kv.Key.Item2))
                {
#if NETFX_CORE
                    var condition = type.GetTypeInfo().IsSubclassOf(mapping.From);
#else
                    var condition = type.IsAssignableFrom(kv.Key.Item1);
#endif
                    if (condition)
                    {
                        var item = Activator.CreateInstance(kv.Value);
                        Inject(item);
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Clears all type-mappings and instances.
        /// </summary>
        public void Clear()
        {
            Instances.Clear();
            Mappings.Clear();
            RelationshipMappings.Clear();
        }

        /// <summary>
        /// Injects registered types/mappings into an object
        /// </summary>
        /// <param name="obj"></param>
        public void Inject(object obj)
        {
            if (obj == null) return;
#if !NETFX_CORE
            var members = obj.GetType().GetMembers();
#else
            var members = obj.GetType().GetTypeInfo().DeclaredMembers;
#endif
            foreach (var memberInfo in members)
            {
                var injectAttribute =
                    memberInfo.GetCustomAttributes(typeof(InjectAttribute), true).FirstOrDefault() as InjectAttribute;
                if (injectAttribute != null)
                {
                    if (memberInfo is PropertyInfo)
                    {
                        var propertyInfo = memberInfo as PropertyInfo;
                        propertyInfo.SetValue(obj, Resolve(propertyInfo.PropertyType, injectAttribute.Name), null);
                    }
                    else if (memberInfo is FieldInfo)
                    {
                        var fieldInfo = memberInfo as FieldInfo;
                        fieldInfo.SetValue(obj, Resolve(fieldInfo.FieldType, injectAttribute.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Register a type mapping
        /// </summary>
        /// <typeparam name="TSource">The base type.</typeparam>
        /// <typeparam name="TTarget">The concrete type</typeparam>
        public void Register<TSource>(string name = null)
        {
            Mappings[typeof(TSource), name] = typeof(TSource);
        }


        /// <summary>
        /// Register a type mapping
        /// </summary>
        /// <typeparam name="TSource">The base type.</typeparam>
        /// <typeparam name="TTarget">The concrete type</typeparam>
        public void Register<TSource, TTarget>(string name = null)
        {
            Mappings[typeof(TSource), name] = typeof(TTarget);
        }

        public void Register(Type source, Type target, string name = null)
        {
            Mappings[source, name] = target;
        }

        /// <summary>
        /// Register a named instance
        /// </summary>
        /// <param name="baseType">The type to register the instance for.</param>        
        /// <param name="instance">The instance that will be resolved be the name</param>
        /// <param name="injectNow">Perform the injection immediately</param>
        public void RegisterInstance(Type baseType, object instance = null, bool injectNow = true)
        {
            RegisterInstance(baseType, instance, null, injectNow);
        }

        /// <summary>
        /// Register a named instance
        /// </summary>
        /// <param name="baseType">The type to register the instance for.</param>
        /// <param name="name">The name for the instance to be resolved.</param>
        /// <param name="instance">The instance that will be resolved be the name</param>
        /// <param name="injectNow">Perform the injection immediately</param>
        public virtual void RegisterInstance(Type baseType, object instance = null, string name = null,
            bool injectNow = true)
        {
            Instances[baseType, name] = instance;
            if (injectNow)
            {
                Inject(instance);
            }
        }

        public void RegisterInstance<TBase>(TBase instance) where TBase : class
        {
            RegisterInstance<TBase>(instance, true);
        }

        public void RegisterInstance<TBase>(TBase instance, bool injectNow) where TBase : class
        {
            RegisterInstance<TBase>(instance, null, injectNow);
        }

        public void RegisterInstance<TBase>(TBase instance, string name, bool injectNow = true) where TBase : class
        {
            RegisterInstance(typeof(TBase), instance, name, injectNow);
        }

        /// <summary>
        ///  If an instance of T exist then it will return that instance otherwise it will create a new one based off mappings.
        /// </summary>
        /// <typeparam name="T">The type of instance to resolve</typeparam>
        /// <returns>The/An instance of 'instanceType'</returns>
        public T Resolve<T>(string name = null, bool requireInstance = false, params object[] args) where T : class
        {
            return (T) Resolve(typeof(T), name, requireInstance, args);
        }

        /// <summary>
        /// If an instance of instanceType exist then it will return that instance otherwise it will create a new one based off mappings.
        /// </summary>
        /// <param name="baseType">The type of instance to resolve</param>
        /// <param name="name">The type of instance to resolve</param>
        /// <param name="requireInstance">If true will return null if an instance isn't registered.</param>
        /// <param name="constructorArgs">The arguments to pass to the constructor if any.</param>
        /// <returns>The/An instance of 'instanceType'</returns>
        public object Resolve(Type baseType, string name = null, bool requireInstance = false,
            params object[] constructorArgs)
        {
            // Look for an instance first
            var item = Instances[baseType, name];
            if (item != null)
            {
                return item;
            }

            if (requireInstance)
                return null;
            // Check if there is a mapping of the type
            var namedMapping = Mappings[baseType, name];
            if (namedMapping != null)
            {
                var obj = CreateInstance(namedMapping, constructorArgs);
                //Inject(obj);
                return obj;
            }

            return null;
        }

        public object CreateInstance(Type type, params object[] constructorArgs)
        {
            if (constructorArgs != null && constructorArgs.Length > 0)
            {
                //return Activator.CreateInstance(type,BindingFlags.Public | BindingFlags.Instance,Type.DefaultBinder, constructorArgs,CultureInfo.CurrentCulture);
                var obj2 = Activator.CreateInstance(type, constructorArgs);
                Inject(obj2);
                return obj2;
            }
#if !NETFX_CORE
            ConstructorInfo[] constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
#else
        ConstructorInfo[] constructor = type.GetTypeInfo().DeclaredConstructors.ToArray();
#endif

            if (constructor.Length < 1)
            {
                var obj2 = Activator.CreateInstance(type);
                Inject(obj2);
                return obj2;
            }

            var maxParameters = constructor.First().GetParameters();

            foreach (var c in constructor)
            {
                var parameters = c.GetParameters();
                if (parameters.Length > maxParameters.Length)
                {
                    maxParameters = parameters;
                }
            }

            var args = maxParameters.Select(p =>
            {
                if (p.ParameterType.IsArray)
                {
                    return ResolveAll(p.ParameterType);
                }

                return Resolve(p.ParameterType) ?? Resolve(p.ParameterType, p.Name);
            }).ToArray();

            var obj = Activator.CreateInstance(type, args);
            Inject(obj);
            return obj;
        }

        public TBase ResolveRelation<TBase>(Type tfor, params object[] args)
        {
            try
            {
                return (TBase) ResolveRelation(tfor, typeof(TBase), args);
            }
            catch (InvalidCastException castIssue)
            {
                throw new Exception(
                    string.Format("Resolve Relation couldn't cast  to {0} from {1}", typeof(TBase).Name, tfor.Name),
                    castIssue);
            }
        }

        public void InjectAll()
        {
            foreach (object instance in Instances.Values)
            {
                Inject(instance);
            }
        }

        private TypeRelationCollection _relationshipMappings = new TypeRelationCollection();

        public void RegisterRelation<TFor, TBase, TConcrete>()
        {
            RelationshipMappings[typeof(TFor), typeof(TBase)] = typeof(TConcrete);
        }

        public void RegisterRelation(Type tfor, Type tbase, Type tconcrete)
        {
            RelationshipMappings[tfor, tbase] = tconcrete;
        }

        public object ResolveRelation(Type tfor, Type tbase, params object[] args)
        {
            var concreteType = RelationshipMappings[tfor, tbase];

            if (concreteType == null)
            {
                return null;
            }

            var result = CreateInstance(concreteType, args);
            //Inject(result);
            return result;
        }

        public TBase ResolveRelation<TFor, TBase>(params object[] arg)
        {
            return (TBase) ResolveRelation(typeof(TFor), typeof(TBase), arg);
        }
    }

    // http://stackoverflow.com/questions/1171812/multi-key-dictionary-in-c
    internal class Tuple<T1, T2> //FUCKING Unity: struct is not supported in Mono
    {
        public readonly T1 Item1;
        public readonly T2 Item2;

        public Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public override bool Equals(object obj)
        {
            Tuple<T1, T2> p = obj as Tuple<T1, T2>;
            if (obj == null) return false;

            if (Item1 == null)
            {
                if (p.Item1 != null) return false;
            }
            else
            {
                if (p.Item1 == null || !Item1.Equals(p.Item1)) return false;
            }

            if (Item2 == null)
            {
                if (p.Item2 != null) return false;
            }
            else
            {
                if (p.Item2 == null || !Item2.Equals(p.Item2)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (Item1 != null)
                hash ^= Item1.GetHashCode();
            if (Item2 != null)
                hash ^= Item2.GetHashCode();
            return hash;
        }
    }

    // Kanglai: Using Dictionary rather than List!
    internal class TypeMappingCollection : Dictionary<Tuple<Type, string>, Type>
    {
        public Type this[Type from, string name = null]
        {
            get
            {
                Tuple<Type, string> key = new Tuple<Type, string>(from, name);
                Type mapping = null;
                if (this.TryGetValue(key, out mapping))
                {
                    return mapping;
                }

                return null;
            }
            set
            {
                Tuple<Type, string> key = new Tuple<Type, string>(from, name);
                this[key] = value;
            }
        }
    }

    internal class TypeInstanceCollection : Dictionary<Tuple<Type, string>, object>
    {
        public object this[Type from, string name = null]
        {
            get
            {
                Tuple<Type, string> key = new Tuple<Type, string>(from, name);
                object mapping = null;
                if (this.TryGetValue(key, out mapping))
                {
                    return mapping;
                }

                return null;
            }
            set
            {
                Tuple<Type, string> key = new Tuple<Type, string>(from, name);
                this[key] = value;
            }
        }
    }

    internal class TypeRelationCollection : Dictionary<Tuple<Type, Type>, Type>
    {
        public Type this[Type from, Type to]
        {
            get
            {
                Tuple<Type, Type> key = new Tuple<Type, Type>(from, to);
                Type mapping = null;
                if (this.TryGetValue(key, out mapping))
                {
                    return mapping;
                }

                return null;
            }
            set
            {
                Tuple<Type, Type> key = new Tuple<Type, Type>(from, to);
                this[key] = value;
            }
        }
    }
}