using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string fileUrlOrId, CancellationToken cancellationToken = default);
}
