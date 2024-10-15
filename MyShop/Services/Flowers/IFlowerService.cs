using MyShop.Entities;

namespace MyShop.Services.Flowers
{
    public interface IFlowerService
    {
        Task<FlowerInfo> CreateFlower(FlowerInfo flower);
        Task<FlowerInfo> GetFlowerById(int id);
        Task<IEnumerable<FlowerInfo>> GetAllFlowers();
        Task<FlowerInfo> UpdateFlower(FlowerInfo flower);
        Task<bool> DeleteFlower(int id);
    }
}