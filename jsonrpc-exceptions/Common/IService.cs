using System.Threading.Tasks;

namespace Common
{
    public interface IService
    {
        Task ExecuteAsync();
        Task StopAsync();
    }
}
