using MyShop.Entities;
using System.Collections.Generic;

public interface ICategoryService
{
    IEnumerable<FlowerInfo> GetFlowersByCategoryId(int categoryId);
    Task<Category> CreateCategoryAsync(Category category);

    IEnumerable<Category> GetAllCategories();
    Task<Category> GetCategoryByIdAsync(int categoryId);
    Task UpdateCategoryAsync(Category category);

    Task DeleteCategoryAsync(int categoryId);

}


