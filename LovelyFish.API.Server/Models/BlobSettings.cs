namespace LovelyFish.API.Server.Models
{
    public class BlobSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "uploads";
    }
}
