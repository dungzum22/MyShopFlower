using MyShop.Entities;
using System.Collections.Generic;

public interface ICategoryService
{
    IEnumerable<FlowerInfo> GetFlowersByCategoryId(int categoryId);
    Task<Category> CreateCategoryAsync(Category category);

    IEnumerable<FlowerInfo> GetFlowersByCategoryName(string categoryName);
}


