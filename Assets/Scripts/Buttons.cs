using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    [Header("API")]
    public string api = "http://192.168.198.129:1880/kursform/";
    [Header("Table Prefab")]
    public GameObject tablePrefab;
    public GameObject editMenu;
    public GameObject addMenu;
    public GameObject showMenu;
    public GameObject activeMenu;

    private string lastResponseJson;
    private List<KursForm> latestKursForm = new();
    KursForm activeKurs;

    private void Awake()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    public void DeleteAction()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Transform>().gameObject;
        int name = int.Parse(clickedButton.name);

        activeKurs = latestKursForm.Find(x => x.kfnr == name);
        StartCoroutine(Delete(activeKurs));
        StartCoroutine(Refresh());
    }

    public IEnumerator Refresh()
    {
        yield return StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (activeMenu != null)
            Destroy(activeMenu);
        activeMenu = Instantiate(showMenu);

        Button addB = activeMenu.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "Hinzufügen");
        if (addB != null)
            addB.onClick.AddListener(() => AddMenu());

        Vector3 offset = new Vector3(0, 120, 0);
        foreach (var kur in latestKursForm)
        {
            GameObject gO = Instantiate(tablePrefab, activeMenu.transform.position + offset, Quaternion.identity, activeMenu.transform);
            gO.name = kur.kfnr.ToString();

            TMP_Text[] textComponents = gO.GetComponentsInChildren<TMP_Text>();
            foreach (var text in textComponents)
            {
                if (text.gameObject.name == "Eingabe knfr")
                    text.text = kur.kfnr.ToString();
                else if (text.gameObject.name == "Eingabe bezeichnung")
                    text.text = kur.bezeichnung.ToString();
            }

            Button[] button = gO.GetComponentsInChildren<Button>();
            foreach (var b in button)
            {
                if (b.name == "Edit")
                    b.onClick.AddListener(() => EditMenu());
                else if (b.name == "Delete")
                    b.onClick.AddListener(() => DeleteAction());
            }
            offset.y -= 80;
        }
    }

    public void AddMenu()
    {
        Destroy(activeMenu);
        activeMenu = Instantiate(addMenu);

        Button[] b = activeMenu.GetComponentsInChildren<Button>();
        foreach (var button in b)
        {
            if (button.name == "Add")
                button.onClick.AddListener(() => Add());
            else if (button.name == "Back")
                button.onClick.AddListener(() => ShowMenu());
        }
    }

    public void Add()
    {
        StartCoroutine(AddNew());
    }

    public void EditMenu()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        GameObject parent = clickedButton.transform.parent.gameObject;
        if (!int.TryParse(parent.name, out int name))
        {
            Debug.LogWarning($"Button-Name '{parent.name}' ist keine Zahl!");
            return;
        }

        Destroy(activeMenu);
        activeMenu = Instantiate(editMenu);

        activeKurs = latestKursForm.Find(x => x.kfnr == name);

        Button[] b = activeMenu.GetComponentsInChildren<Button>();
        foreach (var button in b)
        {
            if (button.name == "Edit")
                button.onClick.AddListener(() => Change());
            else if (button.name == "Back")
                button.onClick.AddListener(() => ShowMenu());
        }

        TMP_InputField[] textComponents = activeMenu.GetComponentsInChildren<TMP_InputField>();
        foreach (var text in textComponents)
        {
            if (text.gameObject.name == "kfnrEingabe")
                text.text = activeKurs.kfnr.ToString();
            else if (text.gameObject.name == "bzEingabe")
                text.text = activeKurs.bezeichnung.ToString();
        }
    }

    public void Change()
    {
        StartCoroutine(Edit(activeKurs));
    }

    public IEnumerator Edit(KursForm kursForm)
    {
        int newkf = Convert.ToInt32(GameObject.Find("kfnrEingabe").GetComponent<TMP_InputField>().text);
        string bz = GameObject.Find("bzEingabe").GetComponent<TMP_InputField>().text;

        KursForm updated = new KursForm(newkf.ToString(), bz);
        string jsonData = JsonUtility.ToJson(updated);

        var url = api + "edit"; // Node-RED Endpoint
        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            uwr.timeout = 10;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    public IEnumerator Delete(KursForm kursForm)
    {
        string jsonData = JsonUtility.ToJson(kursForm);
        var url = api + "delete"; // Node-RED Endpoint

        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            uwr.timeout = 10;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    public IEnumerator AddNew()
    {
        string kf = GameObject.Find("kfnrEingabe").GetComponent<TMP_InputField>().text;
        string bz = GameObject.Find("bzEingabe").GetComponent<TMP_InputField>().text;

        KursForm kursForm = new KursForm(kf, bz);
        string jsonData = JsonUtility.ToJson(kursForm);

        var url = api + "add"; // Node-RED Endpoint
        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            uwr.timeout = 10;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    private IEnumerator FetchKursForm()
    {
        var url = api; // Node-RED GET Endpoint
        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.timeout = 10;
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }

            lastResponseJson = uwr.downloadHandler.text;
            Debug.Log("API-Antwort empfangen: " + lastResponseJson);

            try
            {
                latestKursForm = JsonHelper.FromJson<KursForm>(lastResponseJson);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Fehler beim Deserialisieren: " + e);
            }
        }
    }

    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    [Serializable]
    public class KursForm
    {
        public int kfnr;
        public string bezeichnung;

        public KursForm(string kf, string bz)
        {
            kfnr = Convert.ToInt32(kf);
            bezeichnung = bz;
        }
    }

    [Serializable]
    public class KursFormList
    {
        public List<KursForm> kursformen;
    }

    public static class JsonHelper
    {
        public static List<T> FromJson<T>(string json)
        {
            string wrapped = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.Items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public List<T> Items;
        }
    }
}
