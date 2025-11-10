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
    private KursForm activeKurs;

    private void Awake()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    // -----------------------------------------------
    // CRUD-AKTIONEN
    // -----------------------------------------------

    public IEnumerator FetchKursForm()
    {
        using (UnityWebRequest uwr = UnityWebRequest.Get(api))
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

    public IEnumerator AddNew()
    {
        string bz = GameObject.Find("bzEingabe").GetComponent<TMP_InputField>().text;

        // Node-RED erwartet: [{"bezeichnung": "..."}]
        string jsonData = "[{\"bezeichnung\":\"" + bz + "\"}]";

        using (UnityWebRequest uwr = new UnityWebRequest(api, "POST"))
        {
            uwr.timeout = 10;
            uwr.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"POST fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    public IEnumerator Edit(KursForm kursForm)
    {
        string bz = GameObject.Find("bzEingabe").GetComponent<TMP_InputField>().text;
        string jsonData = "[{\"bezeichnung\":\"" + bz + "\"}]";

        string url = api + kursForm.kfnr;

        using (UnityWebRequest uwr = new UnityWebRequest(url, "PATCH"))
        {
            uwr.timeout = 10;
            uwr.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"PATCH fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    public IEnumerator Delete(KursForm kursForm)
    {
        string url = api + kursForm.kfnr;

        using (UnityWebRequest uwr = UnityWebRequest.Delete(url))
        {
            uwr.timeout = 10;
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"DELETE fehlgeschlagen: {uwr.error}");
        }

        yield return Refresh();
    }

    public IEnumerator Refresh()
    {
        yield return StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    // -----------------------------------------------
    // UI-Logik
    // -----------------------------------------------

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

    public void EditMenu()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        GameObject parent = clickedButton.transform.parent.gameObject;
        if (!int.TryParse(parent.name, out int id))
        {
            Debug.LogWarning($"Button-Name '{parent.name}' ist keine Zahl!");
            return;
        }

        Destroy(activeMenu);
        activeMenu = Instantiate(editMenu);

        activeKurs = latestKursForm.Find(x => x.kfnr == id);

        Button[] b = activeMenu.GetComponentsInChildren<Button>();
        foreach (var button in b)
        {
            if (button.name == "Edit")
                button.onClick.AddListener(() => Change());
            else if (button.name == "Back")
                button.onClick.AddListener(() => ShowMenu());
        }

        TMP_InputField textComponents = activeMenu.GetComponentInChildren<TMP_InputField>();
        TMP_Text[] textF = activeMenu.GetComponentsInChildren<TMP_Text>();
        foreach (var t in textF)
        {
            if (t.gameObject.name == "kfnrEingabe")
                t.text = activeKurs.kfnr.ToString();
        }
        if (textComponents.gameObject.name == "bzEingabe")
            textComponents.text = activeKurs.bezeichnung.ToString();
    }

    public void Add() => StartCoroutine(AddNew());
    public void Change() => StartCoroutine(Edit(activeKurs));

    public void DeleteAction()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        GameObject parent = clickedButton.transform.parent.gameObject;
        if (!int.TryParse(parent.name, out int id))
        {
            Debug.LogWarning($"Button-Name '{parent.name}' ist keine Zahl!");
            return;
        }
        activeKurs = latestKursForm.Find(x => x.kfnr == id);
        StartCoroutine(Delete(activeKurs));
    }

    // -----------------------------------------------
    // Hilfsklassen
    // -----------------------------------------------

    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    [Serializable]
    public class KursForm
    {
        public int kfnr;
        public string bezeichnung;
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
