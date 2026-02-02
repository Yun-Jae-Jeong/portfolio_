using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using MiniJSON;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using YMX;
using UnityEngine.Networking;
using System.Reflection;
using System.Collections.ObjectModel;
using Yellotail;
using System.Net.Mime;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System.Linq;
using System.Drawing;
using UnityEngine.Purchasing;

namespace NIPA2
{
    public class API_Network
    {
		//"http://10.141.10.123:7777";
		//"http://192.168.1.60:7777/";
		private string API_IP = "http://10.141.10.123";
        private int APi_Port = 7777;
        private bool UseNetwork
        {
            get => YMXManager.Instance.useAPINetwork;
        }
        private CoroutineClass coroutineClass;
        //서버 클래스 추가 될때 마다 추가 해야됨
        public static readonly Dictionary<string, Type> ApiResponseTypeMap = new Dictionary<string, Type>
        {
            { "/nipa2/api/v1/dashboard/list", typeof(Dashboard_List_Response) },
            { "/nipa2/api/v1/dashboard/info", typeof(Dashboard_Info_Response) },
            { "/nipa2/api/v1/dashboard/state", typeof(Dashboard_State_Response) },
            { "/nipa2/api/v1/current/list", typeof(Inout_Dashboard_Response) },
            { "/nipa2/api/v1/current/info", typeof(Current_Info_Response) },
            { "/nipa2/api/v1/statistics/[]", typeof(Statistics_Array_Response) },
            { "/nipa2/api/v1/light/current/getEntry", typeof(Light_Current_GetEntry_Response) },
            { "/nipa2/api/v1/light/current/getDailyWorks", typeof(Light_Current_GetDailyWorks_Response) },
            { "/nipa2/api/v1/light/current/getEntryLog", typeof(Light_Current_GetEntryLog_Response) },
            { "/nipa2/api/v1/light/current/list", typeof(Light_Current_List_Response) },
            { "/nipa2/api/v1/light/current/detail", typeof(Light_Current_Detail_Response) },
            { "/nipa2/api/v1/light/current/breakdown", typeof(Light_Current_Breakdown_Response) },
            { "/nipa2/api/v1/light/process/current", typeof(Light_Process_Current_Response) },
            { "/nipa2/api/v1/light/process/list", typeof(Light_Process_List_Response) },
            { "/nipa2/api/v1/light/process/breakdown", typeof(Light_Process_Breakdown_Response) },
            { "/nipa2/api/v1/light/process/unit", typeof(Light_Process_Unit_Response) },
            { "/nipa2/api/v1/light/process/log", typeof(Light_Process_Log_Response) },
            { "/nipa2/api/track-dashboard", typeof(Track_dashboard_Response) },
            { "/nipa2/api/inout-dashboard", typeof(Inout_Dashboard_Response) },
            { "/nipa2/api/memo-data", typeof(MX_Memo_Response) },
            { "/nipa2/api/heavyDummy", typeof(Heavy_Detail_Response) },
            { "/nipa2/api/DisplayUnit", typeof(DisplayUnit_Response) },
            { "workschedule/Test", typeof(WorkSchedule_Reponse)},
			//{ "/nipa2/api/TTT", typeof(Light_Monitoring_Response) },
            //{ "/nipa2/api/tekim-dashboard", typeof(Tekim_Dashboard_Response) },
            { "/nipa2/api/MSDS-Management", typeof(MSDS_Management_Response) },
            { "/nipa2/api/MSDS-Map", typeof(MSDS_Map_Response) },
            { "MSDS_Product/local", typeof(MSDS_Product_Response) },
            { "MSDS_Notify/local", typeof(MSDS_Notify_Response) },
            { "/nipa2/api/todaylog", typeof(Light_CurrentToday_Response) },
            {"MSDS_PRODUCT_LIST", typeof(MSDS_PRODUCT_LIST_Response)},
            {"/msds/v1/product/list/management", typeof(MSDS_PRODUCT_LIST_Response)},
            {"MSDS_PRODUCT_DETAIL", typeof(MSDS_ProductPID_Response)},
            {"/msds/v1/product/list/management/detail", typeof(MSDS_ProductPID_Response)},
            {"/msds/v1/product/with-file", typeof(MSDS_ProductPID_Response)},
            {"/improvement/v1/list", typeof(MSDS_ImprovementList_Response)},
            {"/safetycall/v1/list", typeof(MSDS_SafetyList_Respons)},
            {"/msds/v1/product/upsert", typeof(MSDS_Collective_Response)},
            {"MSDS_PRODUCT_FILE_UPSERT", typeof(MSDS_Collective_Response)},
            {"/msds/v1/product/file/upsert", typeof(MSDS_Collective_Response)},
            {"/improvement/v1", typeof(MSDS_ReportDetail_Response)},
            {"/safetycall/v1", typeof(MSDS_ReportDetail_Response)},
            {"/msds/v1/product/building", typeof(MSDS_MapBid_Response)},
            {"/msds/v1/product/search/product-name", typeof(MSDS_ProductSearch_Response)},
            {"/msds/v1/product/list/workshop", typeof(MSDS_Map_Information_Response)},
            {"/msds/v1/product", typeof(MSDS_ProductPID_Details_Respon)},
            {"/api/light/maintenance/track-progress-status", typeof(TrackProcess_Response)},
            {"/api/light/maintenance/track-occupancy-changes", typeof(TrackOccupancyChange_Response)},
            {"/api/light/maintenance/daily-performance", typeof(DailyPerformance_Response)},
            {"IMPROVEMENT_CREATE", typeof(MSDS_ReportDetail_Response)},
            {"IMPROVEMENT_AUTHOR_UPDATE", typeof(MSDS_ReportDetail_Response)},
            {"SAFETYCALL_CREATE", typeof(MSDS_ReportDetail_Response)},
            {"SAFETYCALL_AUTHOR_UPDATE", typeof(MSDS_ReportDetail_Response)},
            {"/user/auth/login", typeof(UserData_response)},
            {"EMPLOYEE_SEARCH", typeof(MSDS_SafetyEmployee_Response)},
            {"IMPROVEMENT_LIST", typeof(MSDS_ImprovementList_Response)},
            {"SAFETYCALL_LIST", typeof(MSDS_SafetyList_Respons)},
            {"MSDS_PRODUCT_MANAGEMENT_LIST", typeof(MSDS_PRODUCT_LIST_Response)},
            {"IMPROVEMENT_DETAIL", typeof(MSDS_ReportDetail_Response)},
            {"SAFETYCALL_DETAIL", typeof(MSDS_ReportDetail_Response)},
            {"LOGIN_API", typeof(UserData_response)},
            {"TODAY_ENEXT_PRNMNT_INFO_API", typeof(Today_Train_Response)},
			{ "/api/light/maintenance/release/scheduled", typeof(OutboundDashboard_Response)},

			//{ "/nipa2/api/v1/heavy/", typeof(Heavy) },
            //{ "", typeof(Response) },

        };
        private static readonly Dictionary<Type, Delegate> CallbackDelegates = new Dictionary<Type, Delegate>();

        public API_Network(){
            coroutineClass = new CoroutineClass();
            if (PlayerConfig.HasKey("API_IP") == false)
                PlayerConfig.SetString("API_IP", API_IP);
            else
                API_IP = PlayerConfig.GetString("API_IP", API_IP);

            if (PlayerConfig.HasKey("APi_Port") == false)
                PlayerConfig.SetInt("APi_Port", APi_Port);
            else
                APi_Port = PlayerConfig.GetInt("APi_Port", APi_Port);
            SetLocalData();
        }

        private string CheckAPIName(string apiName)
        {
            if (!apiName.StartsWith("/"))
            {
                apiName = "/" + apiName;
            }
            return apiName;
        }

        public void GetAPICall(string apiName, APIMethod method = APIMethod.GET, Action<Response> callback = null, params string[] parameters)
        {
            string address = string.Empty;
            apiName = CheckAPIName(apiName);
            // if (apiName.StartsWith("http") == false)
            //     address = $"{API_IP}:{APi_Port}{apiName}";
            // else
            //     address = apiName;
            address = $"{API_IP}:{APi_Port}{apiName}";
            if (parameters != null)
            {
                foreach (string p in parameters)
                {
                    address += "/" + p;
                }
            }
            Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if (UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutine(address, apiName, method.ToString(), string.Empty, callback), apiName, true);
            else
                LocalData(apiName, callback);
        }
        public void GetAPIBinaryCall(string apiName, APIMethod method = APIMethod.GET, Action<byte[]> callback = null,  params string[] parameters)
        {
            string address = string.Empty;
            apiName = CheckAPIName(apiName);
            // if (apiName.StartsWith("http") == false)
            //     address = $"{API_IP}:{APi_Port}{apiName}";
            // else
            //     address = apiName;
            address = $"{API_IP}:{APi_Port}{apiName}";

            Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if (UseNetwork == true)
                coroutineClass.StartCoroutine(APIBinaryCoroutine(address, apiName, method.ToString(), string.Empty, callback), apiName, true);
        }
        public void SendAPICall(string apiName, APIMethod method = APIMethod.DELETE, Action<Response> callback = null, params string[] parameters)
		{
            apiName = CheckAPIName(apiName);
			string address = $"{API_IP}:{APi_Port}{apiName}";

            if(parameters != null){
                foreach(string p in parameters){
                    address += "/" + p;
                }
            }
			Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if(UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutineDel(address, apiName, method.ToString(), callback));
            else
                LocalData(apiName, callback);
		}
        public void SendAPICall(string apiName, APIMethod method = APIMethod.POST, string jsonData = null, Action<Response> callback = null, params string[] parameters)
		{
            apiName = CheckAPIName(apiName);
			string address = $"{API_IP}:{APi_Port}{apiName}";

			//string address = API_IP + apiName;
            if(parameters != null){
                foreach(string p in parameters){
                    address += "/" + p;
                }
            }
			Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if(UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutine(address, apiName, method.ToString(), jsonData, callback));
            else
                LocalData(apiName, callback);
		}
        public void SendAPICall(string apiName, APIMethod method = APIMethod.POST, byte[] datas = null, Action<Response> callback = null, params string[] parameters)
        {
            apiName = CheckAPIName(apiName);
            string address = $"{API_IP}:{APi_Port}{apiName}";

            //string address = API_IP + apiName;
            if (parameters != null)
            {
                foreach (string p in parameters)
                {
                    address += "/" + p;
                }
            }
            Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if (UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutine(address, apiName, method.ToString(), datas, callback));
            else
                LocalData(apiName, callback);
        }
        public void SendAPICall<T>(string apiName, T sendData, Func<T, WWWForm> sendFunc,  Action<Response> callback = null, APIMethod method = APIMethod.POST)
		{
            apiName = CheckAPIName(apiName);
			string address = $"{API_IP}:{APi_Port}{apiName}";

			Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if(UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutine<T>(address, apiName, sendData, sendFunc, callback, method.ToString()));
            else
                LocalData(apiName, callback);
		}
        public void SendAPICall<T>(string apiName, T sendData, Func<T, List<IMultipartFormSection>> sendFunc,  Action<Response> callback = null)
		{
            apiName = CheckAPIName(apiName);
			string address = $"{API_IP}:{APi_Port}{apiName}";

			Debug.Log(string.Format("<Color=yellow>[{0}] API Call", apiName));

            //call
            if(UseNetwork == true)
                coroutineClass.StartCoroutine(APICoroutine<T>(address, apiName, sendData, sendFunc, callback));
            else
                LocalData(apiName, callback);
		}
        public void DownLoadAPI(string apiName, Action<byte[]> callback = null)
        {
            string address = string.Empty;
            if (apiName.StartsWith("http") == false)
                address = $"{API_IP}:{APi_Port}{apiName}";
            else
                address = apiName;

            if (UseNetwork == true)
                coroutineClass.StartCoroutine(APIDownloadFile(address, callback));
        }
        IEnumerator APIDownloadFile(string url, Action<byte[]> callback)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(www.downloadHandler.data);
                    Debug.Log($"다운로드 완료: {url}");
                }
                else
                {
                    Debug.LogError("파일 다운로드 실패: " + www.error);
                    PopupManager popupManager = SUtils.GetManager<PopupManager>();
                    WarningPopup p = popupManager.OpenPopup<WarningPopup>();
                    p.SetPopup("파일 다운로드 실패", www.error);
                    callback?.Invoke(null);
                }
            }
        }
        public void DownLoadAPI(string apiName, string fileName, string folderPath, Action<bool> callback = null)
        {
            string address = string.Empty;
            if (apiName.StartsWith("http") == false)
                address = $"{API_IP}:{APi_Port}{apiName}";
            else
                address = apiName;

            if (UseNetwork == true)
                coroutineClass.StartCoroutine(APIDownloadFile(address, fileName, folderPath, callback));
        }
        IEnumerator APIDownloadFile(string url, string fileName, string folderPath, Action<bool> callback = null)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string path = System.IO.Path.Combine(folderPath, fileName);
                    System.IO.File.WriteAllBytes(path, www.downloadHandler.data);

                    Debug.Log($"파일 저장 완료: {path}");
                }
                else
                {
                    Debug.LogError("파일 다운로드 실패: " + www.error);
                    PopupManager popupManager = SUtils.GetManager<PopupManager>();
                    WarningPopup p = popupManager.OpenPopup<WarningPopup>();
                    p.SetPopup("파일 다운로드 실패", www.error);
                }
                callback?.Invoke(www.result == UnityWebRequest.Result.Success);
            }
        }
        private IEnumerator APICoroutine(string address,string apiName, Action<Response> callback = null)
        {
            //추가 서버도메인
            string url = address;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"API Request Error: {request.error}");
                    Response r = new Response();
                    r.api = apiName;
                    r.result = false;
                    r.resultMessage = request.error;
                    callback?.Invoke(r);
                }
                else
                {
                    string resultData = request.downloadHandler.text;

                    Debug.Log($"Response: {resultData}");

                    Response dict = JsonUtility.FromJson<Response>(resultData);
                    if (dict == null)
                    {
                        Debug.Log("Not Found Type In ProcessPacket ");
                    }
                    if (dict.result == false
                    || dict.resultMessage.CompareTo("SUCCESS") != 0)
                    {
                        Debug.LogWarning("Fail ProcessPacket ");
                    }
                    else
                    {
                        callback?.Invoke(dict);
                    }
                }
            }
        }
        private IEnumerator APICoroutineDel(string address, string apiName, string method = "DELETE", Action<Response> callback = null)
        {
            //추가 서버도메인
            string url = address;
            UnityWebRequest request;

            // 요청 생성
            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                request = new UnityWebRequest(url, method);
                request.timeout = 60;
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            else
            {
                request = UnityWebRequest.Get(url);
            }

            yield return request.SendWebRequest();
            string resultData = request.downloadHandler.text;
            Debug.Log($"Response: {resultData}");
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                LocalData(apiName, callback);
                Debug.LogWarning($"{url} API Request Error: {request.error}");
            }
            else
            {

                try
                {
                    SendCallbackData(resultData, apiName, callback);
                }
                catch
                {
                    coroutineClass.StopCoroutine(apiName);
                }
            }
        }
        private IEnumerator APIBinaryCoroutine(string address, string apiName, string method = "GET", string jsonData = null, Action<byte[]> callback = null)
        {
            // 추가 서버 도메인
            string url = address;
            UnityWebRequest request;

            // 요청 생성
            if (method == "POST" || method == "PUT")
            {
                request = new UnityWebRequest(url, method);
                request.timeout = 60;
                if (!string.IsNullOrEmpty(jsonData))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            else
            {
                request = UnityWebRequest.Get(url);
            }

            yield return request.SendWebRequest();
            byte[] fileBytes = request.downloadHandler.data;
            callback?.Invoke(fileBytes);
        }
        private IEnumerator APICoroutine(string address, string apiName, string method = "GET", string jsonData = null, Action<Response> callback = null)
        {
            // 추가 서버 도메인
            string url = address;
            UnityWebRequest request;

            // 요청 생성
            if (method == "POST" || method == "PUT")
            {
                request = new UnityWebRequest(url, method);
                request.timeout = 60;
                if (!string.IsNullOrEmpty(jsonData))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            else
            {
                request = UnityWebRequest.Get(url);
            }

            yield return request.SendWebRequest();
            string resultData = request.downloadHandler.text;
            
            Debug.Log($"Response: {resultData}");
            
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                //LocalData(apiName, callback);
                SendCallbackData(resultData, apiName, callback);
                Debug.LogWarning($"{url} API Request Error: {request.error}");
            }
            else
            {

                try
                {
                    SendCallbackData(resultData, apiName, callback);
                }
                catch
                {
                    coroutineClass.StopCoroutine(apiName);
                }
            }
        }
        private IEnumerator APICoroutine(string address, string apiName, string method = "GET", byte[] datas = null, Action<Response> callback = null)
        {
            // 추가 서버 도메인
            string url = address;
            UnityWebRequest request;

            // 요청 생성
            if (method == "POST" || method == "PUT")
            {
                request = new UnityWebRequest(url, method)
                {
                  timeout = 60,
                   uploadHandler = (datas != null && datas.Length > 0) ? new UploadHandlerRaw(datas) : null,
                   downloadHandler = new DownloadHandlerBuffer()
                };
                if (datas != null && datas.Length > 0)
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                }
            }
                else
                {
                    request = UnityWebRequest.Get(url);
                    request.timeout = 60;
                }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"{url} API Request Error: {request.error}");
                LocalData(apiName, callback);
            }
            else
            {
                string resultData = request.downloadHandler.text;
                Debug.Log($"Response: {resultData}");
                try
                {
                    SendCallbackData(resultData, apiName, callback);
                }
                catch
                {
                    coroutineClass.StopCoroutine(apiName);
                }
            }
        }

        public IEnumerator APICoroutine<T>(string address, string apiName, T sendData, Func<T, WWWForm> sendFunc, Action<Response> callback = null, string method = "POST")
        {
            // WWWForm 사용 (multipart/form-data 자동 처리)
            WWWForm form = sendFunc?.Invoke(sendData);

            UnityWebRequest www;
            try
            {
                if (method.ToUpper() == "POST")
                {
                    www = UnityWebRequest.Post(address, form);
                }
                else if (method.ToUpper() == "PUT")
                {
                    byte[] formData = form.data;
                    string contentType;
                    string boundary = string.Empty;
                    if (form.headers.TryGetValue("Content-Type", out contentType))
                    {
                        string[] parts = contentType.Split(';');
                        foreach (var part in parts)
                        {
                            if (part.Trim().StartsWith("boundary="))
                            {
                                boundary = part.Trim().Substring("boundary=".Length);
                                // boundary 사용
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Content-Type header not found in WWWForm headers");
                    }
                    if (string.IsNullOrEmpty(boundary))
                    {
                        boundary = "------UnityFormBoundary" + System.Guid.NewGuid().ToString("N");
                    }

                    www = new UnityWebRequest(address, "PUT");
                    www.uploadHandler = new UploadHandlerRaw(formData);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundary);
                    //www = UnityWebRequest.Put(address, form.data);
                }
                else
                {
                    www = UnityWebRequest.Post(address, form);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                Response r = new Response();
                r.api = apiName;
                r.result = false;
                r.resultMessage = e.Message.ToString();
                callback?.Invoke(r);
                yield break;
            }
            using (www)
            {
                www.timeout = 30;
                yield return www.SendWebRequest();
                string response = www.downloadHandler.text;
                Debug.Log($"Response: {response}");
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SendCallbackData(response, apiName, callback);
                    }
                    catch
                    {
                        coroutineClass.StopCoroutine(apiName);
                    }
                }
                else
                {
                    try
                    {
                        SendCallbackData(response, apiName, callback);
                    }
                    catch
                    {
                        coroutineClass.StopCoroutine(apiName);
                    }
                }
            }
        }
        public IEnumerator APICoroutine<T>(string address, string apiName, T sendData, Func<T, List<IMultipartFormSection>> sendFunc, Action<Response> callback = null)
        {
            // WWWForm 사용 (multipart/form-data 자동 처리)
            List<IMultipartFormSection> form = sendFunc?.Invoke(sendData);

            UnityWebRequest www;
            try
            {
                www = UnityWebRequest.Post(address, form);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                Response r = new Response();
                r.api = apiName;
                r.result = false;
                r.resultMessage = e.Message.ToString();
                callback?.Invoke(r);
                yield break;
            }
            using (www)
            {
                www.timeout = 30;
                yield return www.SendWebRequest();
                string response = www.downloadHandler.text;
                Debug.Log($"Response: {response}");
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SendCallbackData(response, apiName, callback);
                    }
                    catch
                    {
                        coroutineClass.StopCoroutine(apiName);
                    }
                }
                else
                {
                    try
                    {
                        SendCallbackData(response, apiName, callback);
                    }
                    catch
                    {
                        coroutineClass.StopCoroutine(apiName);
                    }
                }
            }
        }
        private void SendCallbackData(string data, string apiName, Action<Response> callback)
        {
            if (ApiResponseTypeMap.TryGetValue(apiName, out Type responseType))
            {
                InvokeCallbackAction(data, responseType, callback);
            }
            else 
            {
                Response re = JsonUtility.FromJson<Response>(data);
                if (re != null
                && ApiResponseTypeMap.TryGetValue(re.api, out responseType))
                {
                    InvokeCallbackAction(data, responseType, callback);
                }
                else
                {
                    CallbackAction<Response>(data, callback);
                }
            }
        }

        private void InvokeCallbackAction(string data, Type responseType, Action<Response> callback)
        {
            if (!CallbackDelegates.TryGetValue(responseType, out Delegate del))
            {
                MethodInfo method = GetType().GetMethod(nameof(CallbackAction), BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo genericMethod = method.MakeGenericMethod(responseType);
                del = Delegate.CreateDelegate(typeof(Action<string, Action<Response>>), this, genericMethod);
                CallbackDelegates[responseType] = del;
            }

            ((Action<string, Action<Response>>)del).Invoke(data, callback);
        }

        private void CallbackAction<T>(string data, Action<Response> callback) where T : Response
        {
            T result = null;
            try
            {
                result = JsonUtility.FromJson<T>(data);
                if (result == null)
                {
                    Debug.LogError("JSON deserialization returned null.");
                    return;
                }

                if (!result.result || result.resultMessage != "SUCCESS")
                {
					Debug.LogWarning($"API Response indicates failure: {result.resultMessage}");

					callback?.Invoke(result);
					return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process JSON: {ex.Message}");
            }
            if(result != null){
                callback?.Invoke(result);
            }
        }
        private Response GerReponse(string data)
        {
            Response result = null;
            try
            {
                result = JsonUtility.FromJson<Response>(data);
                if (result == null)
                {
                    Debug.LogError("JSON deserialization returned null.");
                }

                if (!result.result || result.resultMessage != "SUCCESS")
                {
                    Debug.LogWarning($"API Response indicates failure: {result.resultMessage}");

                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process JSON: {ex.Message}");
                return result;
            }
            return result;
        }
#region Local_Data
        private Dictionary<string, string> localDataDic;
        private Dictionary<string, string> fileDic = new Dictionary<string, string>
        {
            {"/nipa2/api/v1/current/list", "current_List"},
            {"/nipa2/api/v1/dashboard/list", "dashboard_list"},
			{"/nipa2/api/v1/light/current/getEntry", "current_getentry"},
            {"/nipa2/api/v1/light/current/getDailyWorks", "current_dailywork"},
            {"/nipa2/api/v1/light/current/getEntryLog", "current_entrylog"},
            {"/nipa2/api/v1/light/process/breakdown", "process_breakdown"},
            {"/nipa2/api/v1/light/process/current", "pcurrent"},
            {"/nipa2/api/v1/light/process/log", "pcurrentlog"},
			{"/nipa2/api/track-dashboard", "track-dashboard"},
            {"/nipa2/api/inout-dashboard", "Inout_Dashboard_Response"},
            {"/nipa2/api/memo-data", "MX_Memo_Response"},
            {"/nipa2/api/heavyDummy", "HeavyDummy"},
            {"/nipa2/api/DisplayUnit", "imsiDisplayUnit"},
            { "workschedule/Test", "WorkSchedule_Reponse"},
            //{"/nipa2/api/TTT", "track-dashboard_Testmonitor"},
            //{"/nipa2/api/tekim-dashboard", "tekim-dashboard"},
            {"/nipa2/api/MSDS-Management", "MSDS-Management"},
            {"/nipa2/api/MSDS-Map", "MSDS-Map"},
            {"MSDS_Product/local", "MSDS_Product_Response"},
            { "MSDS_Notify/local", "MSDS_Notify_Response" },
            { "/nipa2/api/todaylog", "todaylog" },
};
        private string filePath = "/98_LocalAPIData/";
        private TableConvertToJson tableConvertToJson;
        private void SetLocalData(){
            foreach(string key in fileDic.Keys){
                string path = Path.Combine(Application.streamingAssetsPath + filePath, fileDic[key] + ".json");
                localDataDic ??= new Dictionary<string, string>();
                if (File.Exists(path))
                {
                    string xmlContent = File.ReadAllText(path);
                    
                    if(localDataDic.ContainsKey(key) == false){
                        localDataDic.Add(key, xmlContent);
                    }
                    else{
                        localDataDic[key] = xmlContent;
                    }
                }
                else{
                    tableConvertToJson ??= new TableConvertToJson();
                    string value = tableConvertToJson.GetJson(key);
                    if(localDataDic.ContainsKey(key) == false){
                        localDataDic.Add(key, value);
                    }
                    else{
                        localDataDic[key] = value;
                    }
                }
            }
        }
        private void LocalData(string apiName, Action<Response> callback = null)
        {
            if (localDataDic == null)
            {
                Response r = new Response();
                r.api = apiName;
                r.result = false;
                callback?.Invoke(r);
            }

            if (localDataDic.TryGetValue(apiName, out string value) == true)
            {
                string newVaue = string.Empty;
                SendCallbackData(value, apiName, callback);
            }
            else
            {
                Response r = new Response();
                r.api = apiName;
                r.result = false;
                callback?.Invoke(r);
            }
		}
#endregion
        public void APIOnApplicationQuit(){
            coroutineClass?.StopAllCoroutines();
        }
    }
}
