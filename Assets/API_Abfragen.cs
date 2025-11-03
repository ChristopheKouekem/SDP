using System;
using System.Collections;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class API_Abfragen : MonoBehaviour
{
    [Header("API")]
    public string api = "http://192.168.198.129:1880/kursform/";
    [Header("Table Prefab")]
    public GameObject tablePrefab;
    public GameObject editMenu;
    public GameObject addMenu;
    public GameObject showMenu;
    private GameObject activeMenu;

    private string lastResponseJson;
    private KursForm latestKursForm;

    private void Awake()
    {
        StartCoroutine(FetchKursForm());
    }

    public void EditMenu()
    {

    }

    public IEnumerator Edit(KursForm kursForm)
    {
        int kf = kursForm.kfnr;

        int newkf = Convert.ToInt32(GameObject.Find("").GetComponent<TMP_Text>().text);
        string bz = GameObject.Find("").GetComponent<TMP_Text>().text;

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

    public IEnumerator Add()
    {
        GameObject kfnr = GameObject.Find("").gameObject;
        GameObject bezeichnung = GameObject.Find("").gameObject;

        int kf = Convert.ToInt32(kfnr.GetComponent<TMP_Text>().text);
        string bz = bezeichnung.GetComponent<TMP_Text>().text;

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
            uwr.timeout = 10; // optional: Timeout in Sekunden
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"API-Request fehlgeschlagen: {uwr.error}");
                yield break;
            }

            lastResponseJson = uwr.downloadHandler.text;
            Debug.Log("API-Antwort empfangen: " + lastResponseJson);

            // Versuchen zu deserialisieren; Struktur anpassen falls nötig
            try
            {
                latestKursForm = JsonUtility.FromJson<KursForm>(lastResponseJson);
                if (latestKursForm != null)
                {
                    Debug.Log("JSON erfolgreich zu `KursForm` deserialisiert.");
                }
                else
                {
                    Debug.LogWarning("Deserialisierung ergab null. Prüfe JSON-Struktur oder `KursForm`-Klasse.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Fehler beim Deserialisieren: " + e);
            }

            // Optional: Wenn ein Prefab zugewiesen ist, eine Instanz erzeugen und Daten übergeben
            if (tablePrefab != null && latestKursForm != null)
            {
                var go = Instantiate(tablePrefab);
                var receiver = go.GetComponent<IKursReceiver>();
                if (receiver != null)
                {
                    receiver.ReceiveKurs(latestKursForm);
                }
            }
        }
    }

    // Beispiel einer serialisierbaren Klasse; Felder an tatsächliche API anpassen
    [Serializable]
    public class KursForm
    {
        public int kfnr;
        public string bezeichnung;

        public KursForm(int kf, string bz)
        {
            kfnr = kf;
            bezeichnung = bz;
        }
    }
    public interface IKursReceiver
    {
        void ReceiveKurs(KursForm kurs);
    }
}
