namespace LovelyFish.API.Server.Models.Dtos
{
    public class FishOwnerUpdateRequest
    {
        public string UserName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Location { get; set; } = "";
        public string FishName { get; set; } = "";
        public bool IsContactPublic { get; set; } = true;
    }
}
