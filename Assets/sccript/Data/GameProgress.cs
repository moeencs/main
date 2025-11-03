using Newtonsoft.Json;

namespace sccript.Data
{
    public class GameProgress
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }
        
        [JsonProperty("levelId")]
        public int LevelId { get; set; }
    }
}