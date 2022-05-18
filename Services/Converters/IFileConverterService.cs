using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Services.Converters
{
    public interface IFileConverterService
    {
        string Convert(string fileName, string fileBody);
    }
}
