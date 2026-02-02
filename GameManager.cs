using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIEMENS{
    public enum ManagerType{
        SIEMENS_CoroutineManager, SIEMENS_SceneManager, SIEMENS_DataManager, SIEMENS_ResourceManager,
        SIEMENS_PropManager
    }
    public class SIEMENS_MainManager : MonoSingleton<SIEMENS_MainManager>
    {
        private Dictionary<Type, SIEMENS_Manager> managerDic;
        public event Action CloseSIEMENSActionHandler;

        public string[] siemensPropUIDs;
        public string[] mitPropUIDs;

        public bool IsInited{
            get;
            private set;
        }
    
        public void SIEMENS_Start(){
            if(IsInited == true) return;

            managerDic = new Dictionary<Type, SIEMENS_Manager>();
            GeneManagers();
            StartCoroutine(StartManagers());
            PropOnOf(true);
            IsInited = true;
        }
    
        private void GeneManagers(){
            string[] managers = Enum.GetNames(typeof(ManagerType));
            foreach(string name in managers){
                Type type = Type.GetType("SIEMENS." + name);
                if(type == null) continue;
                if(managerDic.ContainsKey(type) == true){
                    continue;
                }
                else if(type != null){
                    GameObject obj = new GameObject();
                    obj.name = name;
                    obj.transform.SetParent(this.transform);
                    SIEMENS_Manager ob = obj.AddComponent(type) as SIEMENS_Manager;
                    if(ob != null)
                        managerDic.Add(type, ob);
                    ob.AwakeManager();
                }
            }
        }

        private void PropOnOf(bool isSIEMENS){
            foreach(string id in siemensPropUIDs){
                PropManager.SetActiveObject(id, isSIEMENS);
            }
            foreach(string id in mitPropUIDs){
                PropManager.SetActiveObject(id, !isSIEMENS);
            }
        }

        private void DestoryManagers(){
            foreach(SIEMENS_Manager manager in managerDic.Values){
                Destroy(manager.gameObject);
            }
        }
    
        private IEnumerator StartManagers(){
            foreach(SIEMENS_Manager manager in managerDic.Values){
                manager.StartManager();
                yield return manager.StartManager_C();
            }
        }
    
        public T GetManager<T>() where T : SIEMENS_Manager{
            Type target = typeof(T);
            if(managerDic.ContainsKey(target) == true){
                return managerDic[target] as T;
            }
            return null;
        }
        public SIEMENS_Manager GetManager(ManagerType type){
            string typeStr = Enum.GetName(typeof(ManagerType), type);
            Type target = Type.GetType(typeStr);
            if(managerDic.ContainsKey(target) == true){
                return managerDic[target];
            }
            return null;
        }
        public SIEMENS_Manager GetManager(string type){
            Type target = Type.GetType(type);
            if(managerDic.ContainsKey(target) == true){
                return managerDic[target];
            }
            return null;
        }
    
        public void OnCloseSIEMENS() {
            StopAllCoroutines();
            StartCoroutine(EndManagerCoroutine());
            CloseSIEMENSActionHandler?.Invoke();
            CloseSIEMENSActionHandler = null;
            PropOnOf(false);
            IsInited = false;
        }

        private IEnumerator EndManagerCoroutine(){
            foreach(SIEMENS_Manager manager in managerDic.Values){
                IEnumerator managerEnd = manager.EndManager();
                yield return managerEnd;
                Debug_Utils.Log("End Scene " + manager.GetType(), "blue");
            }
            DestoryManagers();
        }
    }
}

namespace SIEMENS{
    public class SIEMENS_Manager : MonoBehaviour
    {
        public virtual void AwakeManager(){
        }
        public virtual void StartManager(){
        }
        public virtual IEnumerator StartManager_C(){
            yield break;
        }
        public virtual IEnumerator EndManager(){
            yield break;
        } 
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
namespace SIEMENS{
    public enum SIEMENS_SceneType{
        None = 0, UI_HMI_PKG_SIEMENS = 1 << 1, LoadScene = 1 << 2,
    }
    public class SIEMENS_SceneManager : SIEMENS_Manager
    {
        private SIEMENS_Scene CurrentScene;
        private SIEMENS_Scene PreviousScene;
        private SIEMENS_SceneType LoadedScene;
        private SIEMENS_SceneController sceneController;
        private CoroutineClass coroutine;
        public bool IsLoadDone{
            private set;
            get;
        }
        public bool IsUnLoadDone{
            private set;
            get;
        }
        public float LoadingProgrees{
            private set;
            get;
        }
        private float progressRatio;
        private float waitTime = 2.5f;
        public override void AwakeManager(){
            base.AwakeManager();
            coroutine = new CoroutineClass();
        }
    	public override void StartManager()
    	{
    		base.StartManager();
            LoadScene(SIEMENS_SceneType.UI_HMI_PKG_SIEMENS);
    	}

        public void LoadScene(SIEMENS_SceneType scene){

            if(LoadedScene.HasFlag(scene) == true) return;
            coroutine.StartCoroutine(LoadingSceneCoroutine(scene), scene.ToString(), true);
        }
        public void UnLoadScene(SIEMENS_SceneType scene){
            if(LoadedScene.HasFlag(scene) == false) return;
            coroutine.StopCoroutine(scene.ToString());
            coroutine.StartCoroutine(UnLoadSceneCoroutine(scene), scene.ToString());
        }

        private IEnumerator LoadingSceneCoroutine(SIEMENS_SceneType sceneType){
            IsLoadDone = false;
            LoadingProgrees = 0f;
            
            LoadedScene |= sceneType;

            var sceneInstance = USceneManager.LoadSceneAsync(sceneType.GetName(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
            while(sceneInstance.isDone == false){
                LoadingProgrees += (sceneInstance.progress * progressRatio);
                yield return 0;
            }
            float timer = waitTime;
            LoadingProgrees = 0.9f;
            if(sceneType == SIEMENS_SceneType.UI_HMI_PKG_SIEMENS){
                while(sceneController == null){
                    if(timer <= 0){
                        Debug_Utils.Log("sceneController is null", "red");
                        break;
                    }
                    timer -= Time.deltaTime;
                    yield return 0;
                }
                sceneController?.Init();
            }
            timer = waitTime;
            while(CurrentScene == null){
                if(timer <= 0){
                    Debug_Utils.Log("CurrentScene is null", "red");
                    break;
                }
                timer -= Time.deltaTime;
                yield return 0;
            }
            if(CurrentScene != null){
                yield return CurrentScene.Inint();
            }
            LoadingProgrees = 1f;

            IsLoadDone = true;
        }
        private IEnumerator UnLoadSceneCoroutine(SIEMENS_SceneType sceneType){
            IsUnLoadDone = false;
            if(LoadedScene != sceneType){
                IsUnLoadDone = true;
                yield break;
            }
            CurrentScene?.EndScene();
            var sceneInstance = USceneManager.UnloadSceneAsync(sceneType.GetName());
            while(sceneInstance.isDone == false){
                LoadingProgrees += (sceneInstance.progress * progressRatio);
                yield return 0;
            }
            CurrentScene = null;
            if(sceneType == SIEMENS_SceneType.UI_HMI_PKG_SIEMENS){
                sceneController = null;
            }
            LoadedScene &= ~sceneType;
            IsUnLoadDone = true;
        }

        private IEnumerator LoadSceneOn(bool isLoad){
            if(isLoad == true){
                var sceneInstance = USceneManager.LoadSceneAsync(SIEMENS_SceneType.LoadScene.GetName(), UnityEngine.SceneManagement.LoadSceneMode.Additive);
                while(sceneInstance.isDone == false){
                    yield return 0;
                }
            }
            else{
                var sceneInstance = USceneManager.UnloadSceneAsync(SIEMENS_SceneType.LoadScene.GetName());
                while(sceneInstance.isDone == false){
                    yield return 0;
                }
            }
        }

        public void SetCurrentScene(SIEMENS_Scene scene, bool isForceSet = true){
            if(isForceSet == false
            && (CurrentScene == scene)) return;
            else if(PreviousScene != CurrentScene) PreviousScene = CurrentScene;
            CurrentScene = scene;
        }
        public void SetSceneController(SIEMENS_SceneController sceneController){
            this.sceneController = sceneController;
        }

		public override IEnumerator EndManager()
		{
            UnLoadScene(SIEMENS_SceneType.UI_HMI_PKG_SIEMENS);

            while(IsUnLoadDone == false){
                yield return 0;
            }
		}
	}
}

namespace SIEMENS{
    public class SIEMENS_Scene : MonoBehaviour
    {
        public string MissionID;
        public virtual IEnumerator Inint(){
            yield break;
        }
        public virtual void EndScene(){
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIEMENS{
    public class SIEMENS_ResourceManager : SIEMENS_Manager
    {
        private Dictionary<string, UnityEngine.Object> resourceDic;

        private Dictionary<string, GameObject> startObjectDic;

        private List<GameObject> instantiatedList;

        private readonly string ResourcePath = "SIEMENS";
        private readonly string[] StartLoadObjKeys = {
            
        };

		public override void AwakeManager()
		{
			base.AwakeManager();
            resourceDic = new Dictionary<string, UnityEngine.Object>();
            instantiatedList = new List<GameObject>();
            LoadAll();
		}

		public override void StartManager()
		{
			base.StartManager();
            StartIstantiate();
		}

        private void LoadAll(){
            UnityEngine.Object[] prefabs = Resources.LoadAll(ResourcePath);
            
            foreach(UnityEngine.Object obj in prefabs){
                if(resourceDic.ContainsKey(obj.name) == false){
                    resourceDic.Add(obj.name, obj);
                }
                else{
                    Debug_Utils.Log("same resource name", "red");
                }
            }
        }

        private void StartIstantiate(Vector3 pos = default, Quaternion rotation = default, Transform parent = null){
            startObjectDic = new Dictionary<string, GameObject>();
            foreach(string key in StartLoadObjKeys){
                if(resourceDic.ContainsKey(key) == true
                && resourceDic[key] is GameObject){
                    GameObject obj = Instantiate(key, pos, rotation, parent);
                    startObjectDic.Add(obj.name, obj);
                }
            }
        }

        public GameObject Instantiate(string key, Vector3 pos = default, Quaternion rotation = default, Transform parent = null){
            if(resourceDic.ContainsKey(key) == false
            && resourceDic[key] is GameObject){
                Debug_Utils.Log("no resourceDic key", "red");
                return null;
            }
            GameObject obj = GameObject.Instantiate(resourceDic[key] as GameObject, pos, rotation, parent);
            instantiatedList.Add(obj);
            return obj;
        }

        public GameObject Instantiate(GameObject obj, Vector3 pos = default, Quaternion rotation = default, Transform parent = null){
            GameObject newObj = GameObject.Instantiate(obj, pos, rotation, parent);
            instantiatedList.Add(newObj);
            return obj;
        }

        public T GetResource<T>(string key) where T : UnityEngine.Object{
            if(resourceDic.ContainsKey(key) == false){
                Debug_Utils.Log("no resourceDic key", "red");
                return null;
            }

            return resourceDic[key] as T;
        }

        private void DestoryInstantiated(){
            foreach(GameObject obj in instantiatedList){
                if(obj != null){
                    Destroy(obj);
                }
            }
            instantiatedList.Clear();
        }

		public override IEnumerator EndManager()
		{
            DestoryInstantiated();
            resourceDic.Clear();
            startObjectDic.Clear();
            yield break;
		}
	}
}
