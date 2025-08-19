namespace LovelyFish.API.Server.Dtos
{
    public class CheckoutDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty; //  ConfirmOrderPage 填的电话
        public Dictionary<int, int>? Quantities { get; set; }
        public List<int> CartItemIds { get; set; } = new List<int>();

        // 前端选择使用哪些优惠
        public bool UseNewUserCoupon { get; set; } = false; // 5刀新用户只能用一次
        public bool Use50Coupon { get; set; } = false;      // 累计满 50
        public bool Use100Coupon { get; set; } = false;     // 累计满 100
    }
}
