using MyShop.Entities;

public interface IGHNService
{
    
    Task<HttpResponseMessage> CalculateShippingFeeAsync(string buyerAddress, string sellerAddress);


    Task<Dictionary<string, object>> GetShippingFeeAsync(string userAddress, string sellerAddress);
}
