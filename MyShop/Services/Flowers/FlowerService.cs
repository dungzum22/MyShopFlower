using MyShop.DataContext;
using MyShop.Entities;

namespace MyShop.Services.Flowers
{
    public class FlowerService : IFlowerService
    {
        private readonly FlowershopContext _context;

        public FlowerService(FlowershopContext context)
        {
            _context = context;
        }



        public FlowerInfo CreateFlower(FlowerInfo flower)
        {

            _context.FlowerInfos.Add(flower);
            _context.SaveChanges();

            return flower;
        }

        public FlowerInfo GetFlowerById(int id)
        {
            return _context.FlowerInfos.Find(id);
        }



        public FlowerInfo UpdateFlower(FlowerInfo flower)
        {
            _context.FlowerInfos.Update(flower);
            _context.SaveChanges();

            return flower;
        }








    }
}
