using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

public class EventService : MonoBehaviour
{
    private Queue<Event> _eventQueue = new Queue<Event>();
    
    private const float CooldownBeforeSend = 2.0f;
    private bool _isSending = false;

    public string ServerUrl;

    public void TrackEvent(string type, string data)
    {
        Event newEvent = new Event(type, data);
        _eventQueue.Enqueue(newEvent);

        if (!_isSending)
        {
            _isSending = true;
            StartCoroutine(ProcessEventQueue());
        }
    }

    private IEnumerator ProcessEventQueue()
    {
        while (_eventQueue.Count > 0)
        {
            List<Event> eventsToSend = new List<Event>(_eventQueue);

            yield return StartCoroutine(SendEvents(eventsToSend));

            yield return new WaitForSeconds(CooldownBeforeSend);
        }

        _isSending = false;
    }

    private IEnumerator SendEvents(List<Event> events)
    {
        string jsonData = JsonUtility.ToJson(new { events = events });
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(ServerUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.responseCode != 200)
            {
#if UNITY_EDITOR
                Debug.LogError($"Ошибка при выполнении запроса: {request.error}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"Запрос выполнен успешно: {request.result}");
#endif
                foreach (Event @event in events)
                {
                    _eventQueue.Dequeue();
                }
            }
        }
    }

#region SaveLoad
    private void Start()
    {
        LoadEvents();
    }

    private void OnApplicationQuit()
    {
        SaveEvents();
    }

    private void SaveEvents()
    {
        List<Event> eventsToSave = new List<Event>(_eventQueue);
        string json = JsonUtility.ToJson(new EventsWrapper { events = eventsToSave });
        PlayerPrefs.SetString("savedEvents", json);
    }

    private void LoadEvents()
    {
        if (PlayerPrefs.HasKey("savedEvents"))
        {
            string json = PlayerPrefs.GetString("savedEvents");
            EventsWrapper loadedEvents = JsonUtility.FromJson<EventsWrapper>(json);
            _eventQueue = new Queue<Event>(loadedEvents.events);
        }
    }

    [System.Serializable]
    private class EventsWrapper
    {
        public List<Event> events;
    }
#endregion
}
