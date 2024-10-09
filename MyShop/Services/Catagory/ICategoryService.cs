using MyShop.Entities;
using System.Collections.Generic;

public interface ICategoryService
{
    IEnumerable<FlowerInfo> GetFlowersByCategoryId(int categoryId);
}
