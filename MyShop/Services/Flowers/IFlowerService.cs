using MyShop.Entities;

namespace MyShop.Services.Flowers
{
    public interface IFlowerService
    {
        //bool CheckFlowerExists(string flowername);
        FlowerInfo CreateFlower(FlowerInfo flower);
        FlowerInfo GetFlowerById(int id);
    }
}