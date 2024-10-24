using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.Entities;
using Newtonsoft.Json.Linq;

namespace MyShop.Services.Flowers
{
    public class FlowerService : IFlowerService
    {
        private readonly FlowershopContext _context;

        public FlowerService(FlowershopContext context)
        {
            _context = context;
        }

        public async Task<FlowerInfo> CreateFlower(FlowerInfo flower)
        {
            // Add the new flower to the context
            await _context.FlowerInfos.AddAsync(flower);
            // Save changes to the database
            await _context.SaveChangesAsync();
            // Return the created flower
            return flower;
        }

        public async Task<FlowerInfo> GetFlowerById(int id)
        {
            return await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);
        }

        public async Task<IEnumerable<FlowerInfo>> GetAllFlowers()
        {
            return await _context.FlowerInfos.ToListAsync();
        }

        public async Task<IEnumerable<FlowerInfo>> GetAllFlowersBySellerId(int sellerId)
        {
            return await _context.FlowerInfos.Where(f => f.SellerId == sellerId).ToListAsync();
        }

        public async Task<FlowerInfo> UpdateFlower(FlowerInfo flower)
        {
            _context.Entry(flower).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return flower;
        }

        public async Task<bool> DeleteFlower(int id)
        {
            var flower = await _context.FlowerInfos.FindAsync(id);
            if (flower == null) return false;

            _context.FlowerInfos.Remove(flower);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
