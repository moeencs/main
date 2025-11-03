using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ParentDashboardScript : MonoBehaviour
{
    // Спрайт для уровней (назначьте в инспекторе)
    [SerializeField] private Sprite levelSprite;
    
    // Существующие объекты для уровней на сцене (перетащите 5 Image объектов)
    [SerializeField] private Image[] levelImageObjects = new Image[5];
    
    // Коллекция всех изображений уровней
    private List<Image> allLevelImages = new List<Image>();
    
    // Текущий активный уровень
    private int currentActiveLevel = 0;
    
    // Цвета для активного и неактивного состояний
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new(53, 177, 203, 255); 

    void Start()
    {
        // Выполняем метод при открытии страницы (запуске сцены)
        OnPageOpen();
        
        // Создаём 5 уровней
        CreateLevels();

        StartCoroutine(LoadProgress());
    }

    // Настройка существующих уровней при запуске сцены
    private void CreateLevels()
    {
        if (levelImageObjects == null || levelImageObjects.Length == 0)
        {
            Debug.LogError("Объекты уровней не назначены в Inspector!");
            return;
        }

        for (int i = 0; i < levelImageObjects.Length; i++)
        {
            Image levelImage = levelImageObjects[i];
            
            if (levelImage == null)
            {
                Debug.LogWarning($"Уровень {i + 1} не назначен в Inspector!");
                continue;
            }
            
            // Устанавливаем спрайт если он указан
            if (levelSprite != null)
            {
                levelImage.sprite = levelSprite;
            }
            
            // Добавляем обработчик клика
            int levelIndex = i + 1; // Сохраняем индекс для замыкания
            AddClickListenerToLevel(levelImage.gameObject, levelIndex);
            
            // Добавляем в коллекцию
            allLevelImages.Add(levelImage);
        }
        
        // Обновляем визуализацию (по умолчанию первый уровень активен, ничего не пройдено)
        UpdateLevelVisuals(0, 1); // lastCompleted = 0, active = 1
    }
    
    // Обновление визуализации уровней
    private void UpdateLevelVisuals(int lastCompletedLevel, int activeLevel)
    {
        currentActiveLevel = activeLevel;
        
        for (int i = 0; i < allLevelImages.Count; i++)
        {
            int levelId = i; // Преобразуем индекс массива в levelId
            
            if (levelId <= activeLevel)
            {
                // Пройденные уровни и активный следующий - нормальный цвет
                allLevelImages[i].color = activeColor;
            }
            else
            {
                // Заблокированные уровни - серый цвет
                allLevelImages[i].color = inactiveColor;
            }
        }
    }
    
    // Добавление слушателя клика для уровня
    private void AddClickListenerToLevel(GameObject imageObject, int levelIndex)
    {
        EventTrigger trigger = imageObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener(_ => { OnLevelClick(levelIndex); });
        trigger.triggers.Add(entry);
    }
    
    // Обработчик клика на уровень
    private void OnLevelClick(int levelIndex)
    {
        // Проверяем, доступен ли уровень (можно играть только активный и пройденные)
        if (levelIndex <= currentActiveLevel)
        {
            Debug.Log($"Переход на уровень {levelIndex}");
            SceneManager.LoadScene($"Chapter{levelIndex}");
        }
        else
        {
            Debug.Log($"Уровень {levelIndex} ещё недоступен!");
        }
    }

    private IEnumerator LoadProgress()
    {
        var userInfoString = PlayerPrefs.GetString("UserInfo");

        Dictionary<string, string> userInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(userInfoString);
        
        var userId =  userInfo["cognito:username"];

        var mongoClient = new MongoClient("mongodb+srv://stephan-admin:fZKA5sPQDkM95WYl@staphan-staging-databas.zw1yqwv.mongodb.net/");
        
        var database = mongoClient.GetDatabase("stephan-database");
        var childrensCollection = database.GetCollection<BsonDocument>("children");
        var usersCollection = database.GetCollection<BsonDocument>("users");
        var userFilter =  Builders<BsonDocument>.Filter.Eq("cognitoUserId", userId);
        
        var user = usersCollection.Find(userFilter).FirstOrDefault(); ;

        var f = user["_id"].ToString();
        
        var filter = Builders<BsonDocument>.Filter.Eq("parentId", new ObjectId(f));
        
        var results = childrensCollection.Find(filter).First();
        
        using UnityWebRequest request = new UnityWebRequest(
            "https://w7hwcmezek.execute-api.us-west-1.amazonaws.com/default/progress-getProgress" +
            $"?userId={userId}" +
            $"&childId={results["_id"]}",
            "GET");
            
        request.downloadHandler = new DownloadHandlerBuffer();

        string idToken = PlayerPrefs.GetString("auth_id_token");
        request.SetRequestHeader("Authorization", $"Bearer {idToken}");
        request.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
        request.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

        // send
        yield return request.SendWebRequest();

        string resp = request.downloadHandler.text;

        var response = JsonConvert.DeserializeObject<ProgressResponse>(resp);

        // Проверяем успешность запроса и наличие данных
        if (response != null && response.Success && response.Data != null && response.Data.Count > 0)
        {
            var result = response.Data
                .DistinctBy(x => x.GetLevelId())
                .OrderByDescending(x => x.GetLevelId())
                .ToList();

            // Получаем последний пройденный уровень из API
            int lastCompletedLevel = result.Max(x => x.GetLevelId());
            
            // Активным будет следующий уровень (последний пройденный + 1)
            int activeLevel = lastCompletedLevel + 1;
            
            // Обновляем визуализацию уровней
            UpdateLevelVisuals(lastCompletedLevel, activeLevel);
            
            Debug.Log($"Последний пройденный уровень: {lastCompletedLevel}, активный уровень: {activeLevel}");
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить прогресс или данные отсутствуют");
        }
    }
    
    private class ProgressResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public List<ProgressResult> Data { get; set; }
    }
    
    private class ProgressResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("childId")]
        public string ChildId { get; set; }
        
        [JsonProperty("levelId")]
        public object LevelId { get; set; }
        
        [JsonProperty("score")]
        public int Score { get; set; }
        
        [JsonProperty("completedAt")]
        public string CompletedAt { get; set; }
        
        // Метод для получения levelId как int (обрабатывает и строку, и число)
        public int GetLevelId()
        {
            if (LevelId == null) return 0;
            
            if (LevelId is long l) return (int)l;
            if (LevelId is int i) return i;
            
            if (int.TryParse(LevelId.ToString(), out int parsed))
                return parsed;
                
            return 0;
        }
    }
    
    // Метод, который выполняется при открытии страницы
    private void OnPageOpen()
    {
        Debug.Log("Страница ParentDashboard открыта!");
    }
}