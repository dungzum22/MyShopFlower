using MyShop.Entities;
using System.Collections.Generic;

namespace MyShop.Services.Flowers
{
    public interface ISearchService
    {
        IEnumerable<FlowerInfo> SearchFlowers(string searchQuery);
    }

}






