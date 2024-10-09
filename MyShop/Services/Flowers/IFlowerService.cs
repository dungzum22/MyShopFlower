using MyShop.Entities;

namespace MyShop.Services.Flowers
{
    public interface IFlowerService
    {
        FlowerInfo CreateFlower(FlowerInfo flower);
        FlowerInfo GetFlowerById(int id);
        FlowerInfo UpdateFlower(FlowerInfo flower);
    }
}
