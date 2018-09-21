using System.Threading.Tasks;
using Refit;

namespace Hacker1ProductsFuncApp
{
    public interface IService
    {
        [Get("/api/users/{userId}")]
        Task<User> GetUser(string userId);
        [Get("/api/products/{productId}")]
        Task<Product> GetProduct(string productId);
    }
}