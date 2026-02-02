using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

using TMPro;
using UnityEditor;
using System.IO;
namespace NIPA2{
    public class ResourceManager : ManagerClass
    {
        private List<GameObject> resourceGameObjectList;
        private Dictionary<string, SpriteAtlas> atlasDic;
        private int resourceID;

        public bool IsDone{
            private set;
            get;
        }
        public bool IsCommonLoaded{
            private set;
            get;
        }
        public bool IsAtlasLoaded{
            private set;
            get;
        }
        private CoroutineClass coroutineClass;

        private bool isAddressableResource = false;

        public override void AwakeManager(){
            base.AwakeManager();

            isAddressableResource = YMXManager.Instance.useAddressableResource;
            coroutineClass = new CoroutineClass();
            resourceGameObjectList = new List<GameObject>();
            atlasDic = new Dictionary<string, SpriteAtlas>();
            IsDone = false;

            //Local_Resources
            resourceDic = new Dictionary<string, YMXResource>();

            //addressable_Local
            loadReasPathQ = new Queue<string[]>();
            resourceAddressableDic = new Dictionary<string, List<YMXResource>>();
            resourceHandleDic = new Dictionary<int, AsyncOperationHandle>();

            //addressable_Server
            patchMap = new Dictionary<string, long>();
            patchSize = 0;
            PatchSize = default;
            Progress = 0;
        }

        public void LoadResource(params PlatformType[] projectTypes){
            IEnumerator enumerator;
            if(isAddressableResource == true){
                enumerator = LoadResource_Server(projectTypes.ToList());
            }
            else{
                enumerator = LoadResources(projectTypes);
            }
            coroutineClass.StartCoroutine(enumerator);
        }

        public Sprite GetAtlasSprite(string atlasName, string spriteName){
            if(atlasDic.ContainsKey(atlasName) == false)
                return null;
            return atlasDic[atlasName].GetSprite(spriteName);
        }
#region local_Resources
        private Dictionary<string, YMXResource> resourceDic;
        public IEnumerator LoadResources(PlatformType[] projectTypes){
            IsDone = false;
            if(YMXManager.Instance.useAddressableResource == true){
                yield return LoadResource_Server(projectTypes.ToList());
            }
            else{
                UnityEngine.Object[] allResources = null;
#if UNITY_EDITOR
                string sourceFolder = "Assets/00_NIPA2_Korail/03_Resources";
                allResources = LoadAssetsFromFolderAndSubfolders(sourceFolder + "/" + projectTypes[0].ToString()).ToArray();
#else
                allResources = Resources.LoadAll("");
#endif
                int count = 50;
                int counting = 0;
                if(allResources == null
                || allResources.Length == 0){
                    Debug.LogWarning("LoadResources no resources");
                    yield break;
                }
                foreach(UnityEngine.Object obj in allResources){
                    resourceDic.TryAdd(obj.name, new YMXResource(obj, GetResourceID(), obj.name));
                    if(counting++ > count){
                        yield return null;
                        counting = 0;
                    }
                }
            }
            IsDone = true;
        }
        private List<UnityEngine.Object> LoadAssetsFromFolderAndSubfolders(string folderPath)
        {
            List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
#if UNITY_EDITOR
            // 폴더 내 모든 에셋의 GUID 가져오기
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (string guid in guids)
            {
                // GUID를 경로로 변환
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 에셋 로드
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
#endif
            return assets;
        }
#endregion
#region local_Addressable
        public delegate void LoadProgressLocalCallback(float currentCount, float targetCount, float progressRate);

        public event LoadProgressLocalCallback loadProgressCallbackHandler;

        private float loadCurrentCount;
        private float loadTargetCount;
        private Queue<string[]> loadReasPathQ;
        private Dictionary<string, List<YMXResource>> resourceAddressableDic;
        private Dictionary<int, AsyncOperationHandle> resourceHandleDic;
        public IEnumerator LoadResource_Local(List<PlatformType> projectTypeList){
            IsDone = false;

            foreach(PlatformType type in projectTypeList){
                string currentProject = type.ToString();
                if(resourceAddressableDic.ContainsKey(currentProject) == true){
                    Debug.LogWarning("Aleady Loaded Addresabble");
                    yield break;
                }

                WaitWhile waitWhile;
                AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle;
                List<string> hashCodeList = new List<string>();
                string[] keyPath = new string[2];
                loadReasPathQ.Clear();

                handle = Addressables.LoadResourceLocationsAsync(currentProject);
                yield return handle;
                if(handle.Status == AsyncOperationStatus.Succeeded
                && handle.Result != null
                && handle.Result.Count > 0){
                    for(int j = 0; j < handle.Result.Count; j++){
                        if(hashCodeList.Contains(handle.Result[j].PrimaryKey) == true) continue;
                        if (handle.Result[j].PrimaryKey.Contains("LightingData"))
                        {
							continue;
                        }
						hashCodeList.Add(handle.Result[j].PrimaryKey);
                        keyPath = new string[2];
                        keyPath[0] = currentProject.ToString();
                        keyPath[1] = handle.Result[j].PrimaryKey;
                        loadReasPathQ.Enqueue(keyPath);
                    }
                }
                loadCurrentCount = 0;
                loadTargetCount = loadReasPathQ.Count;

                AsyncOperationHandle<UnityEngine.Object> obj;
                UnityEngine.Object resultObj;
                string loadPath = string.Empty;
                string loadKey = string.Empty;
                int id = 0;

                for(int i = 0; loadReasPathQ.Count > 0; i++){
                    keyPath = loadReasPathQ.Dequeue();
                    loadKey = keyPath[0];
                    loadPath = keyPath[1];
                    obj = Addressables.LoadAssetAsync<UnityEngine.Object>(loadPath);

                    waitWhile = new WaitWhile(() => obj.IsDone == false);
                    yield return waitWhile;

                    if(resourceAddressableDic.ContainsKey(loadKey) == false){
                        resourceAddressableDic.Add(loadKey, new List<YMXResource>());
                    }

                    resultObj = obj.Result;

                    if(resultObj != null){
                        id = GetResourceID();
                        resourceAddressableDic[loadKey].Add(new YMXResource(resultObj, id, resultObj.name));
                        resourceHandleDic.Add(id, obj);
                    }
                    else{
                        Debug.LogError("Addresable Load Not Found Res : " + loadPath);
                    }
                    loadCurrentCount += 1;
                    loadProgressCallbackHandler?.Invoke(loadCurrentCount, loadTargetCount, loadCurrentCount / loadTargetCount);

                    loadProgressCallbackHandler = null;
                    yield return 0;
                }
            }
            //로드 엣셋 진행
            IsDone = true;
        }
#endregion

#region server_Addressable
        public delegate void LoadProgressServerCallback(long targetSize, string sizeText, float progressRate);
        public event LoadProgressServerCallback loadServerProgressCallbackHandler;
        private Dictionary<string, long> patchMap;
        private long patchSize;
        public string PatchSize{
            private set;
            get;
        }
        public float Progress{
            private set;
            get;
        }
        private IEnumerator LoadResource_Server(List<PlatformType> projectTypeList){
            IsDone = false;
            Progress = 0;
            PatchSize = string.Empty;
            var init = Addressables.InitializeAsync();
            patchMap.Clear();
            yield return init;

            for(int i = 0; i < projectTypeList.Count; i++){
                string label = projectTypeList[i].ToString();
                AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(label);

                if(handle.Status != AsyncOperationStatus.Succeeded){
                    projectTypeList.RemoveAt(i);
                    i--;
                    continue;
                }

                yield return handle;
                patchSize = 0;
                patchSize += handle.Result;
            }
            
            if(patchSize > decimal.Zero){
                PatchSize = GetFileSize(patchSize);
                yield return PatchFiles(projectTypeList.ToArray());
            }
            else{
                //Progress = 1;
                coroutineClass.StartCoroutine(CheckDownload());
                foreach(PlatformType projectType in projectTypeList){
                    yield return LoadAssetsAsync(projectType);
                }
            }
            while(coroutineClass.GetCoroutineCount() > 0){
                yield return null;
            }
            IsDone = true;
        }

        private IEnumerator PatchFiles(PlatformType[] projectTypes){
            PatchSize = default;
            IEnumerator[] coroutines = new IEnumerator[projectTypes.Length];
            for(int i = 0; i < projectTypes.Length; i++){
                string label = projectTypes[i].ToString();

                var handle = Addressables.GetDownloadSizeAsync(label);

                yield return label;

                if(handle.Result != decimal.Zero){
                    coroutines[i] = DownloadLable(projectTypes);
                }
            }

            coroutineClass.StartCoroutine(CheckDownload());

            foreach(IEnumerator enumerator in coroutines){
                if(enumerator != null){
                    yield return enumerator;
                }
            }
        }

        private IEnumerator DownloadLable(PlatformType[] platformTypes){
            for(int i = 0; i < platformTypes.Length; i++){
                string label = platformTypes[i].ToString();
                if(patchMap.ContainsKey(label)== false){
                    patchMap.Add(label, 0);
                }
                else{
                    patchMap[label] = 0;
                }

                var  handle = Addressables.DownloadDependenciesAsync(label);

                while(!handle.IsDone){
                    patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
                    yield return new WaitForEndOfFrame();
                }

                patchMap[label] = handle.GetDownloadStatus().TotalBytes;
                Addressables.Release(handle);
            }
            for(int i = 0; i < platformTypes.Length; i++)
                yield return LoadAssetsAsync(platformTypes[i]);
        }

        private IEnumerator LoadAssetsAsync(PlatformType platformType){
            string label = platformType.ToString(); 
            var loadHandle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
            
            yield return loadHandle;
            bool isMobile = (platformType == PlatformType.NIPA2_Mobile || platformType == PlatformType.NIPA2_Tablet);
            if(loadHandle.Status == AsyncOperationStatus.Succeeded){
                foreach(UnityEngine.Object obj in loadHandle.Result){
                    AddResourceDic(label, obj);
                    if(isMobile == true){
                        FixTextMeshPro(obj, platformType);
                    }
                }
                
            }
        }

        private void FixTextMeshPro(UnityEngine.Object obj, PlatformType platformType)
        {
            if(obj is GameObject){
                GameObject prefabInstance = obj as GameObject;
                TextMeshProUGUI[] textMeshPros = prefabInstance.GetComponentsInChildren<TextMeshProUGUI>();
                if (textMeshPros != null)
                {
                    foreach(TextMeshProUGUI textMeshProUGUI in textMeshPros){
                        // Material 가져오기
                        Material material = textMeshProUGUI.fontMaterial;
                        // 쉐이더 변경
                        Shader newShader = Shader.Find(material.shader.name);
                        if (newShader != null)
                        {
                            material.shader = newShader;
                        }
                        else
                        {
                            Debug.LogError("쉐이더를 찾을 수 없습니다: " + material.shader.name);
                        }
                    }
                }
            }
        }

        private IEnumerator CheckDownload(){
            float total = 0f;

            while(true){
                total += patchMap.Sum(x => x.Value);

                Progress = total / patchSize;
                loadServerProgressCallbackHandler?.Invoke(patchSize, PatchSize, Progress);

                if(total == patchSize){
                    Debug.Log("Addressable DownLoad Done!");

                    break;
                }

                total = 0;
                yield return new WaitForEndOfFrame();
            }
            loadServerProgressCallbackHandler = null;
        }

        private string GetFileSize(long byteC){
            string size = "0 Bytes";

            if(byteC >= 1073741824){
                size = string.Format("{0:##,##}", byteC / 1073741824 + " GB");
            }
            else if(byteC >= 1048576){
                size = string.Format("{0:##,##}", byteC / 1048576 + " MB");
            }
            else if(byteC >= 1024){
                size = string.Format("{0:##,##}", byteC / 1024 + " KB");
            }
            else if(byteC > 0 && byteC < 1024){
                size = byteC.ToString() + " Bytes";
            }

            return size;
        }
#endregion
#region  GetResource
        /// <summary>
        /// 로드된 addressable Key인가?
        /// </summary>
        /// <param name="key"></param> addressable key
        /// <returns></returns>
        public bool CheckAlreadyAbleKey(string key){
            return resourceAddressableDic.ContainsKey(key);
        }

        /// <summary>
        /// 리소스 고유 아이디 할당
        /// </summary>
        /// <returns></returns>
        private int GetResourceID(){
            return resourceID++;
        }

        private YMXResource GetYMXResource(string name){
            if(isAddressableResource == true){
                YMXResource result = null;
                PlatformType key = YMXManager.Instance.GetCurrentPlatform();
                if(resourceAddressableDic.TryGetValue(key.ToString(), out List<YMXResource> value)){
                    result = value.Find(x => x.name.CompareTo(name) == 0);
                    
                }
                if(result == null
                && key != PlatformType.Common
                && resourceAddressableDic.TryGetValue(PlatformType.Common.ToString(), out value)){
                    result = value.Find(x => x.name.CompareTo(name) == 0);
                }
                if(result != null){
                    return result;
                }
            }
            else{
                if(resourceDic.TryGetValue(name, out YMXResource yMXResource)){
                    return yMXResource;
                }
            }
            Debug.LogWarning("Wrong ResourceName: " + name);
            return null;
        }

        private T GetYMXResource<T>(string name) where T : UnityEngine.Object{
            YMXResource newResource = GetYMXResource(name);
            return newResource == null ? null : newResource.resourceObj as T;
        }

        /// <summary>
        /// 리소스 가져오기
        /// </summary>
        /// <param name="key"></param> addressable Key
        /// <param name="name"></param> 리소스 파일 이름
        /// <returns></returns>
        public YMXResource GetResource(string name){
            return GetYMXResource(name);
        }

        /// <summary>
        /// 리소스를 변환해서 가져오기
        /// </summary>
        /// <param name="name"></param> 리소스 파일 이름
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResource<T>(string name) where T : UnityEngine.Object{
            return GetYMXResource<T>(name);
        }

        public GameObject Instantiate(string name, Transform parent = null){
            GameObject gameObject = GetYMXResource<GameObject>(name);
            if(gameObject != null){
                resourceGameObjectList.Add(GameObject.Instantiate(gameObject, parent));
                return resourceGameObjectList[resourceGameObjectList.Count - 1];
            }
            Debug.LogError("Instantiate Fail: " + name);
            return null;
        } 

        /// <summary>
        /// 메모리 해지
        /// </summary>
        /// <param name="key"></param> addressable key
        public void ReleaseResourceKey(PlatformType projectType){
            string key = projectType.ToString();
            if(resourceAddressableDic.ContainsKey(key) == false){
                return;
            }
            var resources = resourceAddressableDic[key];
            for(int i = 0; i < resources.Count; i++){
                Addressables.Release(resourceHandleDic[resources[i].id]);
                resourceHandleDic.Remove(resources[i].id);
            }
            resourceAddressableDic.Remove(key);
        }

        public void ReleaseResourceName(string name){
            string key = GetKey();
            var resources = resourceAddressableDic[key];
            for(int i = 0; i < resources.Count; i++){
                if(resources[i].name.CompareTo(name) == 0){
                    Addressables.Release(resourceHandleDic[resources[i].id]);
                    resourceHandleDic.Remove(resources[i].id);
                }
            }
            if(resourceAddressableDic[key].Count <= 0)
                resourceAddressableDic.Remove(key);
        }

        /// <summary>
        /// 프로젝트에 종속되어 생성한 게임 오브젝트 일괄 파괴
        /// </summary> 
        /// <param name="scene"></param> 언로드 된 씬
        public void ReleaseAllGameObject(PlatformType projectType){
            for(int i = 0; i < resourceGameObjectList.Count; i++){
                if(resourceGameObjectList[i] != null){
                    if(isAddressableResource == true)
                        Addressables.ReleaseInstance(resourceGameObjectList[i]);
                    GameObject.Destroy(resourceGameObjectList[i]);
                }
            }
            resourceGameObjectList.Clear();
        }

        /// <summary>
        /// 특정만 오브젝트 파괴
        /// </summary>
        /// <param name="key"></param> addressable key
        /// <param name="obj"></param> target gameObject
        public void ReleaseKeyGameObject(string key, GameObject obj)
        {
            for(int i = 0; i < resourceGameObjectList.Count; i++)
            {
                if(resourceGameObjectList[i] == obj)
                {
                    if(isAddressableResource == true)
                        Addressables.ReleaseInstance(resourceGameObjectList[i]);
                    GameObject.Destroy(resourceGameObjectList[i]);
                    resourceGameObjectList.RemoveAt(i);
                }
            }
        }

        public void AddResourceDic(string key, UnityEngine.Object obj){
            if(resourceAddressableDic.ContainsKey(key) == false){
                resourceAddressableDic.Add(key, new List<YMXResource>());
            }
            resourceAddressableDic[key].Add(new YMXResource(obj, GetResourceID(), obj.name));
        }

        private string GetKey()
        {
            return YMXManager.Instance.GetCurrentPlatform().ToString();
        }
    }
#endregion
}