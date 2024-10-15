using System.Net.Http;
using System.Threading.Tasks;


namespace MyShop.Services
{
    public interface IVNPayService
    {
        Task<string> ProcessPaymentAsync(decimal amount, string phoneNumber);
    }

}
