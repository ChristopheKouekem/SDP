using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class API_Abfragen : MonoBehaviour
{
    [Header("API")]
    public string api = "https://192.168.198.129:1880/kursform/";
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
        StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    public void DeleteAction()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Transform>().gameObject;
        int name = int.Parse(clickedButton.name);

        activeKurs = latestKursForm.Find(x => x.kfnr == name);
        StartCoroutine(Delete(activeKurs));
        Show();
    }

    public void ShowMenu()
    {
        if (activeMenu != null)
            Destroy(activeMenu);
        activeMenu = Instantiate(showMenu);

        Button addB = (Button)activeMenu.GetComponentsInChildren<Button>().Where(x => x.name == "Hinzufügen");
        addB.onClick.AddListener(() => AddMenu());

        Vector3 offset = Vector3.zero;
        foreach (var kur in latestKursForm)
        {
            GameObject gO = Instantiate(tablePrefab, offset, Quaternion.identity);

            TMP_Text[] textComponents = gO.GetComponentsInChildren<TMP_Text>();
            foreach (var text in textComponents)
            {
                if (text.gameObject.name == "Eingabe knfr")
                {
                    gO.name = kur.kfnr.ToString();
                    text.text = kur.kfnr.ToString();
                }
                else if (text.gameObject.name == "Eingabe bezeichnung")
                {
                    text.text = kur.bezeichnung.ToString();
                }
            }
            Button[] button = gO.GetComponentsInChildren<Button>();
            foreach (var b in button)
            {
                Debug.LogError(b.name);
                if (b.name == "Edit")
                    b.onClick.AddListener(() => EditMenu());
                else if (b.name == "Delete")
                    b.onClick.AddListener(() => DeleteAction());
                Debug.LogError(b.onClick);
            }
            offset.y += 10;
        }
    }

    public void Show()
    {
        StartCoroutine(FetchKursForm());
        ShowMenu();
    }

    public void AddMenu()
    {
        Destroy(activeMenu);
        activeMenu = Instantiate(addMenu);

        Button button = activeMenu.GetComponentInChildren<Button>();
        button.onClick.AddListener(() => EditMenu());
    }

    public void Add()
    {
        AddNew();
    }

    public void EditMenu()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Transform>().gameObject;
        int name = int.Parse(clickedButton.name);


        Destroy(activeMenu);
        activeMenu = Instantiate(editMenu);

        activeKurs = latestKursForm.Find(x => x.kfnr == name);

        GameObject gO = Instantiate(tablePrefab);

        TMP_InputField[] textComponents = gO.GetComponentsInChildren<TMP_InputField>();
        foreach (var text in textComponents)
        {
            if (text.gameObject.name == "kfnrEingabe")
            {
                text.text = activeKurs.kfnr.ToString();
            }
            else if (text.gameObject.name == "bzEingabe")
            {
                text.text = activeKurs.bezeichnung.ToString();
            }
        }
    }

    public void Change()
    {
        Edit(activeKurs);
    }

    public IEnumerator Edit(KursForm kursForm)
    {
        int kf = kursForm.kfnr;

        int newkf = Convert.ToInt32(GameObject.Find("kfnrEingabe").GetComponent<TMP_InputField>().text);
        string bz = GameObject.Find("bzEingabe").GetComponent<TMP_InputField>().text;

        var url = api + "edit/";
        using (UnityWebRequest uwr = new UnityWebRequest(url))
        {
            uwr.timeout = 10;
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }
        }
    }

    public IEnumerator Delete(KursForm kursForm)
    {
        int kf = kursForm.kfnr;
        var url = api + "delete/";
        using (UnityWebRequest uwr = new UnityWebRequest(url))
        {
            uwr.timeout = 10;
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }
        }
    }

    public IEnumerator AddNew()
    {
        GameObject kfnr = GameObject.Find("kfnrEingabe").gameObject;
        GameObject bezeichnung = GameObject.Find("bzEingabe").gameObject;

        string kf = kfnr.GetComponent<TMP_InputField>().text;
        string bz = bezeichnung.GetComponent<TMP_InputField>().text;

        KursForm kursForm = new KursForm(kf, bz);

        var url = api + "add/";
        using (UnityWebRequest uwr = new UnityWebRequest(url))
        {
            uwr.timeout = 10;
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }
        }
    }

    private IEnumerator FetchKursForm()
    {
        var url = api;
        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.timeout = 10;
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }

            lastResponseJson = uwr.downloadHandler.text;
            Debug.Log("API-Antwort empfangen: " + lastResponseJson);

            try
            {
                // Annahme: Die API liefert ein JSON-Objekt mit einer Eigenschaft "kursformen", die ein Array ist
                KursFormList kursFormList = JsonUtility.FromJson<KursFormList>(lastResponseJson);
                if (kursFormList != null && kursFormList.kursformen != null)
                {
                    latestKursForm = kursFormList.kursformen;
                    Debug.Log("JSON erfolgreich zu `List<KursForm>` deserialisiert.");
                }
                else
                {
                    Debug.LogWarning("Deserialisierung ergab null. Prüfe JSON-Struktur oder `KursFormList`-Klasse.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Fehler beim Deserialisieren: " + e);
            }

            Debug.Log(latestKursForm);
        }
    }

    // Hilfsklasse für HTTP-Verbindungen (nur für Entwicklung!)
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    // Beispiel einer serialisierbaren Klasse; Felder an tatsächliche API anpassen
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
}
