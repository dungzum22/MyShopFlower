using MyShop.DataContext;
using MyShop.Entities;


public class CategoryService : ICategoryService
{
    private readonly FlowershopContext _context;

    public CategoryService(FlowershopContext context)
    {
        _context = context;
    }

    // Method to get flowers by category ID
    public IEnumerable<FlowerInfo> GetFlowersByCategoryId(int categoryId)
    {
        // Query to get flowers by category ID
        return _context.FlowerInfos.Where(f => f.CategoryId == categoryId).ToList();
    }

    // Method to create a new category
    public async Task<Category> CreateCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public IEnumerable<Category> GetAllCategories()
    {
        return _context.Categories.ToList();
    }

    public async Task<Category> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories.FindAsync(categoryId);
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }


}